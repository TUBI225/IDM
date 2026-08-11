using Microsoft.VisualStudio.TestTools.UnitTesting;
using WindowsDownloadManager.Application.RateLimiting;

namespace WindowsDownloadManager.Application.Tests;

[TestClass]
public sealed class BandwidthControllerTests
{
    private static readonly DateTimeOffset T0 = DateTimeOffset.Parse("2026-08-11T12:00:00Z");
    private static readonly Guid TaskA = Guid.NewGuid();
    private static readonly Guid TaskB = Guid.NewGuid();

    private sealed class FakeClock
    {
        public DateTimeOffset Now = T0;

        public Func<DateTimeOffset> Read => () => Now;

        public Func<TimeSpan, CancellationToken, ValueTask> Wait => (delay, _) =>
        {
            Now += delay;
            return ValueTask.CompletedTask;
        };
    }

    [TestMethod]
    public async Task Acquire_UnderLimit_ReturnsImmediately()
    {
        var clock = new FakeClock();
        var controller = new BandwidthController(
            globalBytesPerSecond: 1000,
            burstBytes: 1024,
            clock: clock.Read,
            waiter: clock.Wait);

        var startedAt = clock.Now;
        await controller.AcquireAsync(TaskA, "cdn.example.test", 100, CancellationToken.None);

        Assert.AreEqual(TimeSpan.Zero, clock.Now - startedAt);
    }

    [TestMethod]
    public async Task Acquire_ExceedingGlobalRate_ThrottlesByExpectedTotalDelay()
    {
        var clock = new FakeClock();
        var controller = new BandwidthController(
            globalBytesPerSecond: 10,
            burstBytes: 10,
            clock: clock.Read,
            waiter: clock.Wait);

        var startedAt = clock.Now;
        await controller.AcquireAsync(TaskA, "cdn.example.test", 10, CancellationToken.None); // immédiat
        await controller.AcquireAsync(TaskA, "cdn.example.test", 10, CancellationToken.None); // +1s
        await controller.AcquireAsync(TaskA, "cdn.example.test", 10, CancellationToken.None); // +1s

        Assert.IsTrue((clock.Now - startedAt) >= TimeSpan.FromSeconds(1.9));
        Assert.IsTrue((clock.Now - startedAt) <= TimeSpan.FromSeconds(2.1));
    }

    [TestMethod]
    public async Task Acquire_PerTaskLimit_IsIndependentBetweenTasks()
    {
        var clock = new FakeClock();
        var controller = new BandwidthController(
            perTaskBytesPerSecond: 10,
            burstBytes: 10,
            clock: clock.Read,
            waiter: clock.Wait);

        var startedAt = clock.Now;
        await controller.AcquireAsync(TaskA, "cdn.example.test", 10, CancellationToken.None);
        await controller.AcquireAsync(TaskB, "cdn.example.test", 10, CancellationToken.None);

        // Chaque tâche a son propre bucket : aucune attente.
        Assert.AreEqual(TimeSpan.Zero, clock.Now - startedAt);
    }

    [TestMethod]
    public async Task Acquire_PerDomainLimit_IsIndependentBetweenDomains()
    {
        var clock = new FakeClock();
        var controller = new BandwidthController(
            perDomainBytesPerSecond: 10,
            burstBytes: 10,
            clock: clock.Read,
            waiter: clock.Wait);

        var startedAt = clock.Now;
        await controller.AcquireAsync(TaskA, "cdn1.example.test", 10, CancellationToken.None);
        await controller.AcquireAsync(TaskA, "cdn2.example.test", 10, CancellationToken.None);

        // Chaque domaine a son propre bucket : aucune attente.
        Assert.AreEqual(TimeSpan.Zero, clock.Now - startedAt);
    }

    [TestMethod]
    public async Task Acquire_GlobalLimit_IsSharedAcrossTasks()
    {
        var clock = new FakeClock();
        var controller = new BandwidthController(
            globalBytesPerSecond: 10,
            burstBytes: 10,
            clock: clock.Read,
            waiter: clock.Wait);

        var startedAt = clock.Now;
        await controller.AcquireAsync(TaskA, "cdn.example.test", 10, CancellationToken.None); // immédiat
        await controller.AcquireAsync(TaskB, "cdn.example.test", 10, CancellationToken.None); // +1s

        Assert.IsTrue((clock.Now - startedAt) >= TimeSpan.FromSeconds(0.9));
        Assert.IsTrue((clock.Now - startedAt) <= TimeSpan.FromSeconds(1.1));
    }

    [TestMethod]
    public async Task Acquire_ZeroBytes_ReturnsImmediately()
    {
        var clock = new FakeClock();
        var controller = new BandwidthController(
            globalBytesPerSecond: 1,
            burstBytes: 1,
            clock: clock.Read,
            waiter: clock.Wait);

        var startedAt = clock.Now;
        await controller.AcquireAsync(TaskA, "cdn.example.test", 0, CancellationToken.None);

        Assert.AreEqual(TimeSpan.Zero, clock.Now - startedAt);
    }

    [TestMethod]
    public void Constructor_RejectsInvalidBurst()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            new BandwidthController(burstBytes: 0));
    }
}
