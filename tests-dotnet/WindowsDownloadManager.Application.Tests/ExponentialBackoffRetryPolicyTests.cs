using Microsoft.VisualStudio.TestTools.UnitTesting;
using WindowsDownloadManager.Application.Abstractions;
using WindowsDownloadManager.Application.Retries;

namespace WindowsDownloadManager.Application.Tests;

[TestClass]
public sealed class ExponentialBackoffRetryPolicyTests
{
    [TestMethod]
    public void Evaluate_TransientFailure_AllowsRetryWithBoundedDelay()
    {
        var policy = CreatePolicy(isTransient: true, maxAttempts: 5);

        var decision = policy.Evaluate(1, new IOException("transient"));

        Assert.IsTrue(decision.ShouldRetry);
        Assert.IsTrue(decision.Delay > TimeSpan.Zero);
        Assert.IsTrue(decision.Delay <= TimeSpan.FromSeconds(30));
    }

    [TestMethod]
    public void Evaluate_PermanentFailure_DoesNotRetry()
    {
        var policy = CreatePolicy(isTransient: false, maxAttempts: 5);

        var decision = policy.Evaluate(1, new InvalidOperationException("permanent"));

        Assert.IsFalse(decision.ShouldRetry);
        Assert.AreEqual(TimeSpan.Zero, decision.Delay);
    }

    [TestMethod]
    public void Evaluate_AfterMaxAttempts_DoesNotRetry()
    {
        var policy = CreatePolicy(isTransient: true, maxAttempts: 3);

        var decision = policy.Evaluate(3, new IOException("transient"));

        Assert.IsFalse(decision.ShouldRetry);
    }

    [TestMethod]
    public void Evaluate_BackoffGrowsWithAttempts()
    {
        var policy = CreatePolicy(
            isTransient: true,
            maxAttempts: 5,
            baseDelay: TimeSpan.FromMilliseconds(100),
            maxDelay: TimeSpan.FromMinutes(1));

        var first = policy.Evaluate(1, new IOException("transient")).Delay;
        var third = policy.Evaluate(3, new IOException("transient")).Delay;

        // IsGreaterThan(threshold, actual) : vérifie que third dépasse first.
        Assert.IsGreaterThan(first, third);
    }

    [TestMethod]
    public void Evaluate_RetryAfterHint_IsUsedAndCappedByMaximum()
    {
        var policy = new ExponentialBackoffRetryPolicy(
            new StubClassifier(isTransient: true, retryAfter: TimeSpan.FromSeconds(5)),
            maxAttempts: 5,
            maxDelay: TimeSpan.FromSeconds(30));

        var decision = policy.Evaluate(1, new IOException("transient"));

        Assert.IsTrue(decision.ShouldRetry);
        Assert.AreEqual(TimeSpan.FromSeconds(5), decision.Delay);

        var capped = new ExponentialBackoffRetryPolicy(
            new StubClassifier(isTransient: true, retryAfter: TimeSpan.FromMinutes(5)),
            maxAttempts: 5,
            maxDelay: TimeSpan.FromSeconds(30));
        Assert.AreEqual(TimeSpan.FromSeconds(30), capped.Evaluate(1, new IOException("transient")).Delay);
    }

    [TestMethod]
    public void Constructor_RejectsNonPositiveMaxAttempts()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => CreatePolicy(isTransient: true, maxAttempts: 0));
    }

    [TestMethod]
    public void Evaluate_RejectsNonPositiveAttempt()
    {
        var policy = CreatePolicy(isTransient: true, maxAttempts: 5);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => policy.Evaluate(0, new IOException("transient")));
    }

    private static ExponentialBackoffRetryPolicy CreatePolicy(
        bool isTransient,
        int maxAttempts,
        TimeSpan? baseDelay = null,
        TimeSpan? maxDelay = null) =>
        new(
            new StubClassifier(isTransient),
            maxAttempts,
            baseDelay,
            maxDelay,
            new Random(1));

    private sealed class StubClassifier(bool isTransient, TimeSpan? retryAfter = null) : ITransientFailureClassifier
    {
        public bool IsTransient(Exception exception) => isTransient;

        public TimeSpan? GetRetryAfter(Exception exception) => retryAfter;
    }
}
