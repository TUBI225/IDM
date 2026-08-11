using Microsoft.VisualStudio.TestTools.UnitTesting;
using WindowsDownloadManager.Application.Scheduling;

namespace WindowsDownloadManager.Application.Tests;

[TestClass]
public sealed class DownloadSchedulerTests
{
    private static readonly DateTimeOffset T0 = DateTimeOffset.Parse("2026-08-11T12:00:00Z");

    [TestMethod]
    public void AcquireNext_ReturnsHighestPriorityFirst()
    {
        var scheduler = new DownloadScheduler(maxConcurrent: 5);
        var low = new ScheduledDownload(Guid.NewGuid(), Priority: 1, T0);
        var high = new ScheduledDownload(Guid.NewGuid(), Priority: 10, T0.AddSeconds(1));
        scheduler.Submit(low);
        scheduler.Submit(high);

        Assert.AreEqual(high.DownloadId, scheduler.AcquireNext(T0.AddSeconds(2))?.DownloadId);
        Assert.AreEqual(low.DownloadId, scheduler.AcquireNext(T0.AddSeconds(2))?.DownloadId);
    }

    [TestMethod]
    public void AcquireNext_WithEqualPriority_ReturnsFifo()
    {
        var scheduler = new DownloadScheduler(maxConcurrent: 5);
        var first = new ScheduledDownload(Guid.NewGuid(), Priority: 5, T0);
        var second = new ScheduledDownload(Guid.NewGuid(), Priority: 5, T0.AddSeconds(1));
        scheduler.Submit(first);
        scheduler.Submit(second);

        Assert.AreEqual(first.DownloadId, scheduler.AcquireNext(T0.AddSeconds(2))?.DownloadId);
        Assert.AreEqual(second.DownloadId, scheduler.AcquireNext(T0.AddSeconds(2))?.DownloadId);
    }

    [TestMethod]
    public void AcquireNext_RespectsGlobalConcurrencyLimit()
    {
        var scheduler = new DownloadScheduler(maxConcurrent: 2);
        var a = new ScheduledDownload(Guid.NewGuid(), Priority: 5, T0);
        var b = new ScheduledDownload(Guid.NewGuid(), Priority: 5, T0);
        var c = new ScheduledDownload(Guid.NewGuid(), Priority: 5, T0);
        scheduler.Submit(a);
        scheduler.Submit(b);
        scheduler.Submit(c);

        Assert.IsNotNull(scheduler.AcquireNext(T0));
        Assert.IsNotNull(scheduler.AcquireNext(T0));
        Assert.IsNull(scheduler.AcquireNext(T0)); // limite atteinte
        Assert.AreEqual(1, scheduler.PendingCount);

        scheduler.Release(a.DownloadId);
        Assert.AreEqual(c.DownloadId, scheduler.AcquireNext(T0)?.DownloadId);
    }

    [TestMethod]
    public void AcquireNext_EmptyQueue_ReturnsNull()
    {
        var scheduler = new DownloadScheduler(maxConcurrent: 2);
        Assert.IsNull(scheduler.AcquireNext(T0));
    }

    [TestMethod]
    public void Release_RemovesFromActiveAndFreesSlot()
    {
        var scheduler = new DownloadScheduler(maxConcurrent: 1);
        var task = new ScheduledDownload(Guid.NewGuid(), Priority: 5, T0);
        scheduler.Submit(task);
        var acquired = scheduler.AcquireNext(T0);

        Assert.IsNotNull(acquired);
        Assert.IsTrue(scheduler.IsActive(acquired.DownloadId));
        scheduler.Release(acquired.DownloadId);
        Assert.IsFalse(scheduler.IsActive(acquired.DownloadId));
    }

    [TestMethod]
    public void Aging_RaisesLowPriorityAfterWaiting()
    {
        // agingBoost=10, agingInterval=30s : après 30s d'attente, priorité effective +10.
        var scheduler = new DownloadScheduler(
            maxConcurrent: 1,
            agingInterval: TimeSpan.FromSeconds(30),
            agingBoost: 10);
        var low = new ScheduledDownload(Guid.NewGuid(), Priority: 1, T0);
        var high = new ScheduledDownload(Guid.NewGuid(), Priority: 5, T0.AddSeconds(1));
        scheduler.Submit(low);
        scheduler.Submit(high);

        // Au départ, la haute priorité passe.
        Assert.AreEqual(high.DownloadId, scheduler.AcquireNext(T0.AddSeconds(2))?.DownloadId);

        // Après la libération, le bas attendu depuis 90s (3 intervalles -> +30) dépasse la haute (5).
        scheduler.Release(high.DownloadId);
        Assert.AreEqual(low.DownloadId, scheduler.AcquireNext(T0.AddSeconds(92))?.DownloadId);
    }

    [TestMethod]
    public void Constructor_RejectsInvalidArguments()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new DownloadScheduler(maxConcurrent: 0));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            new DownloadScheduler(maxConcurrent: 1, agingInterval: TimeSpan.Zero));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            new DownloadScheduler(maxConcurrent: 1, agingBoost: -1));
    }
}
