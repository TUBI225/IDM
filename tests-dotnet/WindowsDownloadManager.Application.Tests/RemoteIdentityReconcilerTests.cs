using Microsoft.VisualStudio.TestTools.UnitTesting;
using WindowsDownloadManager.Application.Abstractions;
using WindowsDownloadManager.Application.Downloads;
using WindowsDownloadManager.Domain.Downloads;

namespace WindowsDownloadManager.Application.Tests;

[TestClass]
public sealed class RemoteIdentityReconcilerTests
{
    private static readonly DateTimeOffset LastModified = new(2026, 8, 4, 0, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public async Task Reconcile_RecoveryMetadataAbsent_DoesNotAnalyzeRemote()
    {
        var analyzer = new StubAnalyzer(Observed());
        var reconciler = new RemoteIdentityReconciler(analyzer);
        var task = DownloadTask.Restore(
            Guid.NewGuid(),
            new Uri("https://example.test/file.bin"),
            "C:\\Downloads\\file.bin",
            DownloadState.Downloading,
            confirmedBytes: 5);

        var result = await reconciler.ReconcileAsync(task, CancellationToken.None);

        Assert.AreEqual(RemoteIdentityReconciliationStatus.RecoveryMetadataAbsent, result.Status);
        Assert.AreEqual(RemoteIdentityDifference.None, result.Differences);
        Assert.AreEqual(0, analyzer.AnalysisCount);
        Assert.IsNull(result.PersistedIdentity);
        Assert.IsNull(result.ObservedIdentity);
        AssertTaskUnchanged(task);
    }

    [TestMethod]
    public async Task Reconcile_MatchingStrongEntityTag_IsCompatibleAndRedactsUris()
    {
        var observed = Observed(finalUri: "https://cdn.example.test/file.bin?new-token=secret#part");

        var result = await ReconcileAsync(
            Persisted(finalUri: "https://cdn.example.test/file.bin?old-token=secret#old"),
            observed);

        Assert.AreEqual(RemoteIdentityReconciliationStatus.Compatible, result.Status);
        Assert.AreEqual(RemoteIdentityDifference.None, result.Differences);
        Assert.IsNotNull(result.PersistedIdentity);
        Assert.IsNotNull(result.ObservedIdentity);
        Assert.AreEqual(string.Empty, result.PersistedIdentity.FinalUri.Query);
        Assert.AreEqual(string.Empty, result.ObservedIdentity.FinalUri.Query);
        Assert.AreEqual(string.Empty, result.ObservedIdentity.FinalUri.Fragment);
    }

    [TestMethod]
    public async Task Reconcile_FinalUriChanged_IsContradictory()
    {
        var result = await ReconcileAsync(
            Persisted(),
            Observed(finalUri: "https://cdn.example.test/other.bin"));

        AssertContradiction(result, RemoteIdentityDifference.FinalUriChanged);
    }

    [TestMethod]
    public async Task Reconcile_LengthChanged_IsContradictory()
    {
        var result = await ReconcileAsync(Persisted(), Observed(length: 11));

        AssertContradiction(result, RemoteIdentityDifference.LengthChanged);
    }

    [TestMethod]
    public async Task Reconcile_EntityTagChanged_IsContradictory()
    {
        var result = await ReconcileAsync(Persisted(), Observed(entityTag: "\"v2\""));

        AssertContradiction(result, RemoteIdentityDifference.EntityTagChanged);
    }

    [TestMethod]
    public async Task Reconcile_LastModifiedChanged_IsContradictory()
    {
        var result = await ReconcileAsync(
            Persisted(),
            Observed(lastModified: LastModified.AddMinutes(1)));

        AssertContradiction(result, RemoteIdentityDifference.LastModifiedChanged);
    }

    [TestMethod]
    public async Task Reconcile_PreviouslyKnownEvidenceMissing_IsInsufficient()
    {
        var result = await ReconcileAsync(
            Persisted(),
            Observed(length: null, entityTag: null, hasLastModified: false));

        Assert.AreEqual(RemoteIdentityReconciliationStatus.InsufficientEvidence, result.Status);
        Assert.IsTrue(result.Differences.HasFlag(RemoteIdentityDifference.LengthEvidenceMissing));
        Assert.IsTrue(result.Differences.HasFlag(RemoteIdentityDifference.EntityTagEvidenceMissing));
        Assert.IsTrue(result.Differences.HasFlag(RemoteIdentityDifference.LastModifiedEvidenceMissing));
        Assert.IsTrue(result.Differences.HasFlag(RemoteIdentityDifference.SufficientIdentityEvidenceMissing));
    }

    [TestMethod]
    public async Task Reconcile_WeakEntityTagAlone_IsInsufficient()
    {
        var result = await ReconcileAsync(
            Persisted(length: null, entityTag: "W/\"v1\"", hasLastModified: false),
            Observed(length: null, entityTag: "W/\"v1\"", hasLastModified: false));

        Assert.AreEqual(RemoteIdentityReconciliationStatus.InsufficientEvidence, result.Status);
        Assert.AreEqual(
            RemoteIdentityDifference.SufficientIdentityEvidenceMissing,
            result.Differences);
    }

    [TestMethod]
    public async Task Reconcile_MatchingLengthAndDateWithoutEntityTag_IsCompatible()
    {
        var result = await ReconcileAsync(
            Persisted(entityTag: null),
            Observed(entityTag: null));

        Assert.AreEqual(RemoteIdentityReconciliationStatus.Compatible, result.Status);
        Assert.AreEqual(RemoteIdentityDifference.None, result.Differences);
    }

    [TestMethod]
    public async Task Reconcile_ByteRangeSupportLost_IsClassifiedSeparately()
    {
        var result = await ReconcileAsync(Persisted(), Observed(supportsByteRanges: false));

        Assert.AreEqual(RemoteIdentityReconciliationStatus.ResumeCapabilityLost, result.Status);
        Assert.AreEqual(RemoteIdentityDifference.ByteRangeSupportLost, result.Differences);
    }

    private static async Task<RemoteIdentityReconciliationResult> ReconcileAsync(
        RemoteIdentity persisted,
        RemoteResourceInfo observed)
    {
        var analyzer = new StubAnalyzer(observed);
        var reconciler = new RemoteIdentityReconciler(analyzer);
        var task = PreparedTask(persisted);

        var result = await reconciler.ReconcileAsync(task, CancellationToken.None);

        Assert.AreEqual(1, analyzer.AnalysisCount);
        Assert.AreEqual(task.OriginalUri, analyzer.AnalyzedUri);
        AssertTaskUnchanged(task);
        return result;
    }

    private static void AssertContradiction(
        RemoteIdentityReconciliationResult result,
        RemoteIdentityDifference difference)
    {
        Assert.AreEqual(RemoteIdentityReconciliationStatus.Contradictory, result.Status);
        Assert.IsTrue(result.Differences.HasFlag(difference));
    }

    private static DownloadTask PreparedTask(RemoteIdentity identity) => DownloadTask.Restore(
        Guid.NewGuid(),
        new Uri("https://example.test/file.bin"),
        "C:\\Downloads\\file.bin",
        DownloadState.Downloading,
        confirmedBytes: 5,
        "C:\\Downloads\\file.download",
        identity);

    private static RemoteIdentity Persisted(
        string finalUri = "https://cdn.example.test/file.bin",
        long? length = 10,
        string? entityTag = "\"v1\"",
        DateTimeOffset? lastModified = null,
        bool hasLastModified = true,
        bool supportsByteRanges = true) =>
        new(
            new Uri(finalUri),
            length,
            entityTag,
            hasLastModified ? lastModified ?? LastModified : null,
            supportsByteRanges);

    private static RemoteResourceInfo Observed(
        string finalUri = "https://cdn.example.test/file.bin",
        long? length = 10,
        string? entityTag = "\"v1\"",
        DateTimeOffset? lastModified = null,
        bool hasLastModified = true,
        bool supportsByteRanges = true) =>
        new(
            new Uri("https://example.test/file.bin"),
            new Uri(finalUri),
            length,
            null,
            null,
            entityTag,
            hasLastModified ? lastModified ?? LastModified : null,
            supportsByteRanges);

    private static void AssertTaskUnchanged(DownloadTask task)
    {
        Assert.AreEqual(DownloadState.Downloading, task.State);
        Assert.AreEqual(5, task.ConfirmedBytes);
    }

    private sealed class StubAnalyzer(RemoteResourceInfo resource) : IRemoteResourceAnalyzer
    {
        public int AnalysisCount { get; private set; }
        public Uri? AnalyzedUri { get; private set; }

        public ValueTask<RemoteResourceInfo> AnalyzeAsync(
            Uri uri,
            CancellationToken cancellationToken)
        {
            AnalysisCount++;
            AnalyzedUri = uri;
            return ValueTask.FromResult(resource);
        }
    }
}
