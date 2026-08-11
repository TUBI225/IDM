using Microsoft.VisualStudio.TestTools.UnitTesting;
using WindowsDownloadManager.Domain.Downloads;

namespace WindowsDownloadManager.Domain.Tests;

[TestClass]
public sealed class DownloadTaskTests
{
    [TestMethod]
    public void ValidLifecycle_ReachesCompleted()
    {
        var task = NewTask();
        foreach (var state in new[]
                 {
                     DownloadState.Analyzing,
                     DownloadState.Preparing,
                     DownloadState.Waiting,
                     DownloadState.Downloading,
                     DownloadState.Verifying,
                 })
        {
            task.TransitionTo(state);
        }

        task.RecordVerifiedSha256(new string('A', 64));
        task.TransitionTo(DownloadState.Finalizing);
        task.TransitionTo(DownloadState.Completed);

        Assert.AreEqual(DownloadState.Completed, task.State);
    }

    [TestMethod]
    public void Completed_IsTerminal()
    {
        Assert.IsFalse(
            DownloadStateMachine.CanTransition(DownloadState.Completed, DownloadState.Downloading),
            "Completed must be terminal.");
    }

    [TestMethod]
    public void ConfirmedProgress_CannotMoveBackwards()
    {
        var task = NewTask();
        task.ConfirmPersistedBytes(1024);

        AssertThrowsExactly<InvalidOperationException>(() => task.ConfirmPersistedBytes(512));
    }

    [TestMethod]
    public void RecordPreparation_InPreparingState_PreservesRecoveryMetadata()
    {
        var task = NewTask();
        task.TransitionTo(DownloadState.Analyzing);
        task.TransitionTo(DownloadState.Preparing);
        var identity = new RemoteIdentity(
            new Uri("https://cdn.example.test/file.bin"),
            4096,
            "\"v1\"",
            DateTimeOffset.Parse("2026-08-04T00:00:00Z"),
            supportsByteRanges: true);

        task.RecordPreparation("C:\\Downloads\\file.download", identity);

        Assert.AreEqual("C:\\Downloads\\file.download", task.TemporaryPath);
        Assert.AreEqual(identity, task.RemoteIdentity);
    }

    [TestMethod]
    public void RecordPreparation_OutsidePreparingState_IsRejected()
    {
        var task = NewTask();
        var identity = new RemoteIdentity(
            new Uri("https://cdn.example.test/file.bin"),
            1,
            null,
            null,
            supportsByteRanges: false);

        AssertThrowsExactly<InvalidOperationException>(() =>
            task.RecordPreparation("C:\\Downloads\\file.download", identity));
    }

    [TestMethod]
    public void Finalizing_WithoutVerifiedSha256_IsRejected()
    {
        var task = NewTask();
        task.TransitionTo(DownloadState.Analyzing);
        task.TransitionTo(DownloadState.Preparing);
        task.TransitionTo(DownloadState.Waiting);
        task.TransitionTo(DownloadState.Downloading);
        task.TransitionTo(DownloadState.Verifying);

        AssertThrowsExactly<InvalidOperationException>(() =>
            task.TransitionTo(DownloadState.Finalizing));
    }

    [TestMethod]
    public void RecordVerifiedSha256_NormalizesLowercaseHex()
    {
        var task = DownloadTask.Restore(
            Guid.NewGuid(),
            new Uri("https://example.test/file.bin"),
            "file.bin",
            DownloadState.Verifying,
            confirmedBytes: 0);

        task.RecordVerifiedSha256(new string('a', 64));

        Assert.AreEqual(new string('A', 64), task.VerifiedSha256);
    }

    [TestMethod]
    public void RecordVerifiedSha256_InvalidValue_IsRejected()
    {
        var task = DownloadTask.Restore(
            Guid.NewGuid(),
            new Uri("https://example.test/file.bin"),
            "file.bin",
            DownloadState.Verifying,
            confirmedBytes: 0);

        AssertThrowsExactly<ArgumentException>(() => task.RecordVerifiedSha256("invalid"));
    }

    [TestMethod]
    public void Restore_DownloadingWithVerifiedSha256_IsRejected()
    {
        AssertThrowsExactly<InvalidDataException>(() => DownloadTask.Restore(
            Guid.NewGuid(),
            new Uri("https://example.test/file.bin"),
            "file.bin",
            DownloadState.Downloading,
            confirmedBytes: 0,
            verifiedSha256: new string('A', 64)));
    }

    private static DownloadTask NewTask() =>
        new(Guid.NewGuid(), new Uri("https://example.test/file.bin"), "file.bin");

    private static void AssertThrowsExactly<TException>(Action action)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }

        Assert.Fail($"Expected {typeof(TException).Name}.");
    }
}
