using Microsoft.VisualStudio.TestTools.UnitTesting;
using WindowsDownloadManager.Application.Abstractions;
using WindowsDownloadManager.Application.Downloads;
using WindowsDownloadManager.Domain.Downloads;

namespace WindowsDownloadManager.Application.Tests;

[TestClass]
public sealed class RecoveryOverlapVerifierTests
{
    [TestMethod]
    public async Task Verify_BlockedDecision_RejectsBeforeReading()
    {
        var localReader = new StubLocalReader([1], fileLength: 1);
        var remoteReader = new StubRemoteReader([1]);
        var decision = ReadyDecision(safePosition: 1) with
        {
            Status = RecoveryDecisionStatus.Blocked,
            Blockers = RecoveryBlocker.RemoteIdentityContradictory,
        };

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () =>
            await new RecoveryOverlapVerifier(localReader, remoteReader)
                .VerifyAsync(decision, CancellationToken.None));

        Assert.AreEqual(0, localReader.ReadCount);
        Assert.AreEqual(0, remoteReader.ReadCount);
    }

    [TestMethod]
    public async Task Verify_ZeroSafePosition_DoesNotRequireOverlapOrRead()
    {
        var localReader = new StubLocalReader([], fileLength: 0);
        var remoteReader = new StubRemoteReader([]);

        var result = await new RecoveryOverlapVerifier(localReader, remoteReader)
            .VerifyAsync(ReadyDecision(safePosition: 0), CancellationToken.None);

        Assert.AreEqual(OverlapVerificationStatus.NotRequired, result.Status);
        Assert.AreEqual(0, result.Offset);
        Assert.AreEqual(0, result.Length);
        Assert.AreEqual(0, localReader.ReadCount);
        Assert.AreEqual(0, remoteReader.ReadCount);
    }

    [TestMethod]
    public async Task Verify_MatchingOverlap_ReturnsMatch()
    {
        byte[] content = [1, 2, 3, 4, 5];
        var localReader = new StubLocalReader(content, fileLength: 5);
        var remoteReader = new StubRemoteReader(content);

        var result = await new RecoveryOverlapVerifier(localReader, remoteReader)
            .VerifyAsync(ReadyDecision(safePosition: 5), CancellationToken.None);

        Assert.AreEqual(OverlapVerificationStatus.Match, result.Status);
        Assert.AreEqual(0, result.Offset);
        Assert.AreEqual(5, result.Length);
        Assert.AreEqual(5, result.ObservedFileLength);
        Assert.AreEqual(1, localReader.ReadCount);
        Assert.AreEqual(1, remoteReader.ReadCount);
    }

    [TestMethod]
    public async Task Verify_DifferentOverlap_ReturnsMismatchWithoutMutation()
    {
        var localReader = new StubLocalReader([1, 2, 3], fileLength: 3);
        var remoteReader = new StubRemoteReader([1, 9, 3]);

        var result = await new RecoveryOverlapVerifier(localReader, remoteReader)
            .VerifyAsync(ReadyDecision(safePosition: 3), CancellationToken.None);

        Assert.AreEqual(OverlapVerificationStatus.Mismatch, result.Status);
        Assert.AreEqual(3, result.Length);
    }

    [TestMethod]
    public async Task Verify_LargeCheckpoint_ReadsOnlyTrailingMaximumWindow()
    {
        var content = new byte[RecoveryOverlapVerifier.MaximumOverlapLength];
        var safePosition = RecoveryOverlapVerifier.MaximumOverlapLength + 123L;
        var localReader = new StubLocalReader(content, fileLength: safePosition);
        var remoteReader = new StubRemoteReader(content);

        var result = await new RecoveryOverlapVerifier(localReader, remoteReader)
            .VerifyAsync(ReadyDecision(safePosition), CancellationToken.None);

        Assert.AreEqual(OverlapVerificationStatus.Match, result.Status);
        Assert.AreEqual(123, result.Offset);
        Assert.AreEqual(RecoveryOverlapVerifier.MaximumOverlapLength, result.Length);
        Assert.AreEqual(123, localReader.Offset);
        Assert.AreEqual(123, remoteReader.Offset);
    }

    [TestMethod]
    public async Task Verify_LocalLengthChanged_BlocksBeforeRemoteRead()
    {
        var localReader = new StubLocalReader([], fileLength: 4);
        var remoteReader = new StubRemoteReader([1, 2, 3, 4, 5]);

        var result = await new RecoveryOverlapVerifier(localReader, remoteReader)
            .VerifyAsync(ReadyDecision(safePosition: 5), CancellationToken.None);

        Assert.AreEqual(OverlapVerificationStatus.LocalFileChanged, result.Status);
        Assert.AreEqual(4, result.ObservedFileLength);
        Assert.AreEqual(0, remoteReader.ReadCount);
    }

    [TestMethod]
    public async Task Verify_RemoteReaderReturnsShortRange_Throws()
    {
        var localReader = new StubLocalReader([1, 2, 3], fileLength: 3);
        var remoteReader = new StubRemoteReader([1, 2]);

        await Assert.ThrowsExactlyAsync<InvalidDataException>(async () =>
            await new RecoveryOverlapVerifier(localReader, remoteReader)
                .VerifyAsync(ReadyDecision(safePosition: 3), CancellationToken.None));
    }

    private static RecoveryDecisionResult ReadyDecision(long safePosition)
    {
        var downloadId = Guid.NewGuid();
        var path = "C:\\Downloads\\file.download";
        var identity = new RemoteIdentity(
            new Uri("https://cdn.example.test/file.bin"),
            safePosition,
            "\"v1\"",
            null,
            supportsByteRanges: true);
        var local = new TemporaryFileReconciliationResult(
            downloadId,
            TemporaryFileReconciliationStatus.TemporaryFileMatchesCheckpoint,
            path,
            safePosition,
            safePosition,
            safePosition);
        var remote = new RemoteIdentityReconciliationResult(
            downloadId,
            RemoteIdentityReconciliationStatus.Compatible,
            RemoteIdentityDifference.None,
            identity,
            identity);
        return new RecoveryDecisionResult(
            downloadId,
            RecoveryDecisionStatus.ReadyForOverlapVerification,
            RecoveryBlocker.None,
            safePosition,
            local,
            remote);
    }

    private sealed class StubLocalReader(byte[] content, long fileLength) : ITemporaryFileRangeReader
    {
        public int ReadCount { get; private set; }
        public long? Offset { get; private set; }

        public ValueTask<TemporaryFileRangeSnapshot> ReadRangeAsync(
            string temporaryPath,
            long offset,
            int length,
            CancellationToken cancellationToken)
        {
            ReadCount++;
            Offset = offset;
            return ValueTask.FromResult(
                new TemporaryFileRangeSnapshot(fileLength, content));
        }
    }

    private sealed class StubRemoteReader(byte[] content) : IRemoteRangeReader
    {
        public int ReadCount { get; private set; }
        public long? Offset { get; private set; }

        public ValueTask<ReadOnlyMemory<byte>> ReadRangeAsync(
            RemoteIdentity identity,
            long offset,
            int length,
            CancellationToken cancellationToken)
        {
            ReadCount++;
            Offset = offset;
            return ValueTask.FromResult<ReadOnlyMemory<byte>>(content);
        }
    }
}
