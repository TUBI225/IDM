using System.Buffers;
using WindowsDownloadManager.Application.Abstractions;

namespace WindowsDownloadManager.Application.Downloads;

/// <summary>
/// Issue d'une retransmission contrôlée.
/// </summary>
public enum ControlledRetransmissionStatus
{
    Completed,
    DivergenceDetected,
    RemoteEndedEarly,
    ExceededAnnouncedLength,
}

/// <summary>
/// Coût réseau annoncé avant exécution (PR-062) : la retransmission renvoie depuis zéro, donc le
/// volume réseau consommé peut largement dépasser le travail local restant. Un coût significatif
/// exige un consentement explicite.
/// </summary>
public sealed record RetransmissionCostEstimate(
    long? TotalBytesNetwork,
    long BytesAlreadyLocal,
    bool RequiresConsent);

/// <summary>
/// Résultat immuable d'une exécution de retransmission contrôlée.
/// </summary>
public sealed record ControlledRetransmissionResult(
    Guid DownloadId,
    ControlledRetransmissionStatus Status,
    long BytesAlreadyLocal,
    long BytesReceived,
    long? DivergenceOffset);

/// <summary>
/// Moteur de retransmission contrôlée (M-012, ADR-020). Le serveur a refusé l'accès partiel et
/// renvoie le corps depuis zéro : le moteur compare le nouveau flux aux octets locaux, ne réécrit
/// qu'au premier octet absent (travail local préservé) et s'arrête immédiatement à toute divergence,
/// l'ancien partiel restant intact. Le coût réseau total est annoncé ; un coût significatif exige un
/// consentement explicite. La confirmation de progression reste contiguë et uniquement après écriture
/// durable (flush), jamais d'octet réécrit sur un préfixe identique.
/// </summary>
public sealed class ControlledRetransmissionEngine
{
    public const int BufferSize = 64 * 1024;
    public const long DefaultConsentThresholdBytes = 1024L * 1024 * 1024;

    private readonly long _consentThresholdBytes;

    public ControlledRetransmissionEngine(long? consentThresholdBytes = null)
    {
        _consentThresholdBytes = consentThresholdBytes ?? DefaultConsentThresholdBytes;
        if (_consentThresholdBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(consentThresholdBytes));
        }
    }

    public RetransmissionCostEstimate EstimateCost(long? remoteLength, long bytesAlreadyLocal)
    {
        if (bytesAlreadyLocal < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bytesAlreadyLocal));
        }

        return new RetransmissionCostEstimate(
            remoteLength,
            bytesAlreadyLocal,
            remoteLength is { } length && length > _consentThresholdBytes);
    }

    public async ValueTask<ControlledRetransmissionResult> ExecuteAsync(
        Guid downloadId,
        Stream remoteContent,
        long? remoteLength,
        string temporaryPath,
        ITemporaryFileRangeReader localReader,
        ITemporaryFileWriter writer,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(remoteContent);
        ArgumentNullException.ThrowIfNull(localReader);
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentException.ThrowIfNullOrWhiteSpace(temporaryPath);
        if (remoteLength < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(remoteLength));
        }

        var buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
        try
        {
            var bytesAlreadyLocal = 0L;
            var bytesReceived = 0L;
            long? lastLocalFileLength = null;
            var resumingWrite = false;

            while (true)
            {
                var read = await remoteContent
                    .ReadAsync(buffer.AsMemory(0, BufferSize), cancellationToken)
                    .ConfigureAwait(false);
                if (read == 0)
                {
                    break;
                }

                bytesReceived += read;
                if (remoteLength is { } length && bytesReceived > length)
                {
                    return new ControlledRetransmissionResult(
                        downloadId,
                        ControlledRetransmissionStatus.ExceededAnnouncedLength,
                        bytesAlreadyLocal,
                        bytesReceived,
                        DivergenceOffset: length);
                }

                if (resumingWrite)
                {
                    bytesAlreadyLocal = await WriteAndVerifyAsync(
                        writer,
                        temporaryPath,
                        bytesAlreadyLocal,
                        buffer.AsMemory(0, read),
                        cancellationToken).ConfigureAwait(false);
                    continue;
                }

                var local = await localReader
                    .ReadRangeAsync(temporaryPath, bytesAlreadyLocal, read, cancellationToken)
                    .ConfigureAwait(false);
                if (local.Content.Length > read)
                {
                    throw new InvalidDataException("The temporary-file reader returned more than requested.");
                }

                lastLocalFileLength = local.FileLength;
                var match = local.Content.Length;
                if (match > 0 &&
                    !buffer.AsSpan(0, match).SequenceEqual(local.Content.Span))
                {
                    var difference = FirstDifference(
                        buffer.AsSpan(0, match),
                        local.Content.Span,
                        match);
                    return new ControlledRetransmissionResult(
                        downloadId,
                        ControlledRetransmissionStatus.DivergenceDetected,
                        bytesAlreadyLocal,
                        bytesReceived,
                        DivergenceOffset: bytesAlreadyLocal + difference);
                }

                bytesAlreadyLocal += match;
                if (match < read)
                {
                    resumingWrite = true;
                    bytesAlreadyLocal = await WriteAndVerifyAsync(
                        writer,
                        temporaryPath,
                        bytesAlreadyLocal,
                        buffer.AsMemory(match, read - match),
                        cancellationToken).ConfigureAwait(false);
                }
            }

            if (remoteLength is { } announced && bytesReceived != announced)
            {
                return new ControlledRetransmissionResult(
                    downloadId,
                    ControlledRetransmissionStatus.RemoteEndedEarly,
                    bytesAlreadyLocal,
                    bytesReceived,
                    DivergenceOffset: null);
            }

            if (!resumingWrite &&
                lastLocalFileLength is { } localLength &&
                bytesAlreadyLocal < localLength)
            {
                return new ControlledRetransmissionResult(
                    downloadId,
                    ControlledRetransmissionStatus.DivergenceDetected,
                    bytesAlreadyLocal,
                    bytesReceived,
                    DivergenceOffset: bytesAlreadyLocal);
            }

            return new ControlledRetransmissionResult(
                downloadId,
                ControlledRetransmissionStatus.Completed,
                bytesAlreadyLocal,
                bytesReceived,
                DivergenceOffset: null);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static async ValueTask<long> WriteAndVerifyAsync(
        ITemporaryFileWriter writer,
        string temporaryPath,
        long offset,
        ReadOnlyMemory<byte> content,
        CancellationToken cancellationToken)
    {
        var flushed = await writer
            .WriteAndFlushAsync(temporaryPath, offset, content, cancellationToken)
            .ConfigureAwait(false);
        if (flushed != offset + content.Length)
        {
            throw new InvalidDataException("The temporary writer confirmed an unexpected byte boundary.");
        }

        return flushed;
    }

    private static int FirstDifference(ReadOnlySpan<byte> remote, ReadOnlySpan<byte> local, int commonLength)
    {
        for (var index = 0; index < commonLength; index++)
        {
            if (remote[index] != local[index])
            {
                return index;
            }
        }

        return commonLength;
    }
}

