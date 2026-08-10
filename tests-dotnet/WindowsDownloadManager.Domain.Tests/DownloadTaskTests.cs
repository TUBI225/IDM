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
                     DownloadState.Finalizing,
                     DownloadState.Completed,
                 })
        {
            task.TransitionTo(state);
        }

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
