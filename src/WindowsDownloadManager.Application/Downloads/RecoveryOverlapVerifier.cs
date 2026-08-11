using WindowsDownloadManager.Application.Abstractions;

namespace WindowsDownloadManager.Application.Downloads;

public sealed class RecoveryOverlapVerifier
{
    public const int MaximumOverlapLength = 64 * 1024;

    private readonly ITemporaryFileRangeReader _temporaryFileRangeReader;
    private readonly IRemoteRangeReader _remoteRangeReader;

    public RecoveryOverlapVerifier(
        ITemporaryFileRangeReader temporaryFileRangeReader,
        IRemoteRangeReader remoteRangeReader)
    {
        _temporaryFileRangeReader = temporaryFileRangeReader ??
            throw new ArgumentNullException(nameof(temporaryFileRangeReader));
        _remoteRangeReader = remoteRangeReader ??
            throw new ArgumentNullException(nameof(remoteRangeReader));
    }

    public async ValueTask<OverlapVerificationResult> VerifyAsync(
        RecoveryDecisionResult decision,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(decision);
        if (decision.Status != RecoveryDecisionStatus.ReadyForOverlapVerification ||
            decision.Blockers != RecoveryBlocker.None)
        {
            throw new InvalidOperationException(
                "Overlap verification requires an unblocked recovery decision.");
        }

        if (decision.SafePosition < 0)
        {
            throw new InvalidDataException("The recovery safe position cannot be negative.");
        }

        if (decision.SafePosition == 0)
        {
            return CreateResult(decision, OverlapVerificationStatus.NotRequired, 0, 0, observedFileLength: null);
        }

        var temporaryPath = decision.TemporaryFile.TemporaryPath
            ?? throw new InvalidDataException("The recovery decision has no temporary path.");
        var expectedFileLength = decision.TemporaryFile.FileLength
            ?? throw new InvalidDataException("The recovery decision has no temporary-file length.");
        var observedIdentity = decision.RemoteIdentity.ObservedIdentity
            ?? throw new InvalidDataException("The recovery decision has no observed remote identity.");

        var length = (int)Math.Min(decision.SafePosition, MaximumOverlapLength);
        var offset = decision.SafePosition - length;
        var local = await _temporaryFileRangeReader
            .ReadRangeAsync(temporaryPath, offset, length, cancellationToken)
            .ConfigureAwait(false);
        if (local.FileLength != expectedFileLength)
        {
            return CreateResult(
                decision,
                OverlapVerificationStatus.LocalFileChanged,
                offset,
                length,
                local.FileLength);
        }

        if (local.Content.Length != length)
        {
            throw new InvalidDataException("The temporary-file reader returned an incomplete range.");
        }

        var remote = await _remoteRangeReader
            .ReadRangeAsync(observedIdentity, offset, length, cancellationToken)
            .ConfigureAwait(false);
        if (remote.Length != length)
        {
            throw new InvalidDataException("The remote reader returned an incomplete range.");
        }

        var status = local.Content.Span.SequenceEqual(remote.Span)
            ? OverlapVerificationStatus.Match
            : OverlapVerificationStatus.Mismatch;
        return CreateResult(decision, status, offset, length, local.FileLength);
    }

    private static OverlapVerificationResult CreateResult(
        RecoveryDecisionResult decision,
        OverlapVerificationStatus status,
        long offset,
        int length,
        long? observedFileLength) =>
        new(
            decision.DownloadId,
            status,
            offset,
            length,
            decision.SafePosition,
            observedFileLength);
}
