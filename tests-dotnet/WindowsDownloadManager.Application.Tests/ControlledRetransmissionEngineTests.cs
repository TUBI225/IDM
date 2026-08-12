using Microsoft.VisualStudio.TestTools.UnitTesting;
using WindowsDownloadManager.Application.Abstractions;
using WindowsDownloadManager.Application.Downloads;

namespace WindowsDownloadManager.Application.Tests;

[TestClass]
public sealed class ControlledRetransmissionEngineTests
{
    private static readonly Guid DownloadId = Guid.NewGuid();
    private const string TemporaryPath = "C:\\Downloads\\fixture.download";

    private static readonly byte[] Pattern =
        Enumerable.Range(0, 70_000).Select(value => (byte)(value % 251)).ToArray();

    private readonly ControlledRetransmissionEngine _engine = new();

    [TestMethod]
    public async Task Execute_IdenticalStream_KeepsPrefixWithoutRewriting()
    {
        var events = new List<string>();
        var writer = new RecordingWriter(events);
        var reader = new StubLocalReader(Pattern);
        var stream = new MemoryStream(Pattern, writable: false);

        var result = await _engine.ExecuteAsync(
            DownloadId,
            stream,
            Pattern.Length,
            TemporaryPath,
            reader,
            writer,
            CancellationToken.None);

        Assert.AreEqual(ControlledRetransmissionStatus.Completed, result.Status);
        Assert.AreEqual(Pattern.Length, result.BytesAlreadyLocal);
        Assert.AreEqual(Pattern.Length, result.BytesReceived);
        Assert.AreEqual(0, writer.WriteCount);
        Assert.IsNull(result.DivergenceOffset);
    }

    [TestMethod]
    public async Task Execute_IdenticalPrefixThenMissingTail_ResumesWriteAtFirstAbsentByte()
    {
        var events = new List<string>();
        var writer = new RecordingWriter(events);
        var local = Pattern.AsMemory(0, 5_000).ToArray();
        var reader = new StubLocalReader(local);
        var stream = new MemoryStream(Pattern, writable: false);

        var result = await _engine.ExecuteAsync(
            DownloadId,
            stream,
            Pattern.Length,
            TemporaryPath,
            reader,
            writer,
            CancellationToken.None);

        Assert.AreEqual(ControlledRetransmissionStatus.Completed, result.Status);
        Assert.AreEqual(Pattern.Length, result.BytesAlreadyLocal);
        Assert.AreEqual(local.Length, writer.PrefixPreserved);
        Assert.AreEqual(2, writer.WriteCount);
        CollectionAssert.AreEqual(
            Pattern.AsSpan(local.Length).ToArray(),
            writer.Written.ToArray());
        Assert.IsNull(result.DivergenceOffset);
    }

    [TestMethod]
    public async Task Execute_DivergenceAt64KiB_StopsSafelyLeavingPartialIntact()
    {
        var events = new List<string>();
        var writer = new RecordingWriter(events);
        var local = (byte[])Pattern.Clone();
        local[64 * 1024] = (byte)(local[64 * 1024] ^ 0xFF);
        var reader = new StubLocalReader(local);
        var stream = new MemoryStream(Pattern, writable: false);

        var result = await _engine.ExecuteAsync(
            DownloadId,
            stream,
            Pattern.Length,
            TemporaryPath,
            reader,
            writer,
            CancellationToken.None);

        Assert.AreEqual(ControlledRetransmissionStatus.DivergenceDetected, result.Status);
        Assert.AreEqual(64 * 1024, result.DivergenceOffset);
        Assert.AreEqual(0, writer.WriteCount);
    }

    [TestMethod]
    public async Task Execute_DivergenceAtFiftyPercent_StopsSafelyLeavingPartialIntact()
    {
        var events = new List<string>();
        var writer = new RecordingWriter(events);
        var local = (byte[])Pattern.Clone();
        local[Pattern.Length / 2] = (byte)(local[Pattern.Length / 2] ^ 0xFF);
        var reader = new StubLocalReader(local);
        var stream = new MemoryStream(Pattern, writable: false);

        var result = await _engine.ExecuteAsync(
            DownloadId,
            stream,
            Pattern.Length,
            TemporaryPath,
            reader,
            writer,
            CancellationToken.None);

        Assert.AreEqual(ControlledRetransmissionStatus.DivergenceDetected, result.Status);
        Assert.AreEqual(Pattern.Length / 2, result.DivergenceOffset);
        Assert.AreEqual(0, writer.WriteCount);
    }

    [TestMethod]
    public async Task Execute_DivergenceNearEnd_StopsSafelyLeavingPartialIntact()
    {
        var events = new List<string>();
        var writer = new RecordingWriter(events);
        var local = (byte[])Pattern.Clone();
        local[^1] = (byte)(local[^1] ^ 0xFF);
        var reader = new StubLocalReader(local);
        var stream = new MemoryStream(Pattern, writable: false);

        var result = await _engine.ExecuteAsync(
            DownloadId,
            stream,
            Pattern.Length,
            TemporaryPath,
            reader,
            writer,
            CancellationToken.None);

        Assert.AreEqual(ControlledRetransmissionStatus.DivergenceDetected, result.Status);
        Assert.AreEqual(Pattern.Length - 1, result.DivergenceOffset);
        Assert.AreEqual(0, writer.WriteCount);
    }

    [TestMethod]
    public async Task Execute_RemoteEndsBeforeAnnounced_ReportsRemoteEndedEarly()
    {
        var events = new List<string>();
        var writer = new RecordingWriter(events);
        var local = Pattern.AsMemory(0, 3_000).ToArray();
        var reader = new StubLocalReader(local);
        var remote = Pattern.AsMemory(0, 1_500).ToArray();
        var stream = new MemoryStream(remote, writable: false);

        var result = await _engine.ExecuteAsync(
            DownloadId,
            stream,
            remoteLength: 2_000,
            TemporaryPath,
            reader,
            writer,
            CancellationToken.None);

        Assert.AreEqual(ControlledRetransmissionStatus.RemoteEndedEarly, result.Status);
        Assert.AreEqual(0, writer.WriteCount);
    }

    [TestMethod]
    public async Task Execute_RemoteExceedsAnnounced_ReportsExceededAnnouncedLength()
    {
        var events = new List<string>();
        var writer = new RecordingWriter(events);
        var local = Pattern.AsMemory(0, 500).ToArray();
        var reader = new StubLocalReader(local);
        var stream = new MemoryStream(Pattern, writable: false);

        var result = await _engine.ExecuteAsync(
            DownloadId,
            stream,
            remoteLength: 1_000,
            TemporaryPath,
            reader,
            writer,
            CancellationToken.None);

        Assert.AreEqual(ControlledRetransmissionStatus.ExceededAnnouncedLength, result.Status);
        Assert.AreEqual(1_000, result.DivergenceOffset);
        Assert.AreEqual(0, writer.WriteCount);
    }

    [TestMethod]
    public async Task Execute_LocalLongerThanRemote_DetectsDivergenceAtRemoteEnd()
    {
        var events = new List<string>();
        var writer = new RecordingWriter(events);
        var local = Pattern.AsMemory(0, 5_000).ToArray();
        var reader = new StubLocalReader(local);
        var remote = Pattern.AsMemory(0, 2_000).ToArray();
        var stream = new MemoryStream(remote, writable: false);

        var result = await _engine.ExecuteAsync(
            DownloadId,
            stream,
            remote.Length,
            TemporaryPath,
            reader,
            writer,
            CancellationToken.None);

        Assert.AreEqual(ControlledRetransmissionStatus.DivergenceDetected, result.Status);
        Assert.AreEqual(2_000, result.DivergenceOffset);
        Assert.AreEqual(0, writer.WriteCount);
    }

    [TestMethod]
    public async Task Execute_WritesAreFlushedBeforeCompletion()
    {
        var events = new List<string>();
        var writer = new RecordingWriter(events);
        var local = Pattern.AsMemory(0, 100).ToArray();
        var reader = new StubLocalReader(local);
        var stream = new MemoryStream(Pattern.AsMemory(0, 200).ToArray(), writable: false);

        var result = await _engine.ExecuteAsync(
            DownloadId,
            stream,
            remoteLength: 200,
            TemporaryPath,
            reader,
            writer,
            CancellationToken.None);

        Assert.AreEqual(ControlledRetransmissionStatus.Completed, result.Status);
        Assert.IsTrue(events.Contains("flush:200"));
        var flush = events.IndexOf("flush:200");
        Assert.IsGreaterThanOrEqualTo(0, flush);
        Assert.AreEqual(200, result.BytesAlreadyLocal);
    }

    [TestMethod]
    public async Task Execute_EmptyStreamWithEmptyLocal_Completes()
    {
        var events = new List<string>();
        var writer = new RecordingWriter(events);
        var reader = new StubLocalReader([]);
        var stream = new MemoryStream([], writable: false);

        var result = await _engine.ExecuteAsync(
            DownloadId,
            stream,
            remoteLength: 0,
            TemporaryPath,
            reader,
            writer,
            CancellationToken.None);

        Assert.AreEqual(ControlledRetransmissionStatus.Completed, result.Status);
        Assert.AreEqual(0, result.BytesAlreadyLocal);
        Assert.AreEqual(0, result.BytesReceived);
        Assert.AreEqual(0, writer.WriteCount);
    }


    [TestMethod]
    public async Task Execute_NullRemoteContent_ThrowsArgumentNullException()
    {
        var writer = new RecordingWriter([]);
        var reader = new StubLocalReader([]);

        await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => _engine
            .ExecuteAsync(DownloadId, null!, 0, TemporaryPath, reader, writer, CancellationToken.None)
            .AsTask());
    }

    [TestMethod]
    public void EstimateCost_UnderThreshold_DoesNotRequireConsent()
    {
        var estimate = _engine.EstimateCost(remoteLength: 1_000, bytesAlreadyLocal: 500);

        Assert.AreEqual(1_000, estimate.TotalBytesNetwork);
        Assert.AreEqual(500, estimate.BytesAlreadyLocal);
        Assert.IsFalse(estimate.RequiresConsent);
    }

    [TestMethod]
    public void EstimateCost_AboveThreshold_RequiresConsent()
    {
        var engine = new ControlledRetransmissionEngine(consentThresholdBytes: 1_000);
        var estimate = engine.EstimateCost(remoteLength: 1_001, bytesAlreadyLocal: 10);

        Assert.AreEqual(1_001, estimate.TotalBytesNetwork);
        Assert.IsTrue(estimate.RequiresConsent);
    }

    [TestMethod]
    public void EstimateCost_UnknownLength_ReportsUnknownNetworkCost()
    {
        var estimate = _engine.EstimateCost(remoteLength: null, bytesAlreadyLocal: 5);

        Assert.IsNull(estimate.TotalBytesNetwork);
        Assert.AreEqual(5, estimate.BytesAlreadyLocal);
        Assert.IsFalse(estimate.RequiresConsent);
    }

    [TestMethod]
    public void EstimateCost_NegativeLocalBytes_ThrowsArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => _engine.EstimateCost(remoteLength: 10, bytesAlreadyLocal: -1));
    }

    private sealed class StubLocalReader(byte[] content) : ITemporaryFileRangeReader
    {
        public ValueTask<TemporaryFileRangeSnapshot> ReadRangeAsync(
            string temporaryPath,
            long offset,
            int length,
            CancellationToken cancellationToken)
        {
            var start = (int)Math.Min(offset, content.Length);
            var count = (int)Math.Min(length, content.Length - start);
            return ValueTask.FromResult(
                new TemporaryFileRangeSnapshot(content.Length, content.AsMemory(start, count)));
        }
    }

    private sealed class RecordingWriter(List<string> events) : ITemporaryFileWriter
    {
        public MemoryStream Written { get; } = new();
        public int WriteCount { get; private set; }
        public long PrefixPreserved { get; private set; }

        public ValueTask PrepareNewAsync(string temporaryPath, CancellationToken cancellationToken) =>
            ValueTask.CompletedTask;

        public async ValueTask<long> WriteAndFlushAsync(
            string temporaryPath,
            long offset,
            ReadOnlyMemory<byte> content,
            CancellationToken cancellationToken)
        {
            WriteCount++;
            if (Written.Length == 0)
            {
                PrefixPreserved = offset;
            }

            await Written.WriteAsync(content, cancellationToken);
            events.Add($"flush:{offset + content.Length}");
            return offset + content.Length;
        }
    }
}

