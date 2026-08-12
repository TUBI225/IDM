using Microsoft.VisualStudio.TestTools.UnitTesting;
using WindowsDownloadManager.Application.Abstractions;
using WindowsDownloadManager.Host;

namespace WindowsDownloadManager.Host.Tests;

[TestClass]
public sealed class DownloadStrategyTests
{
    private static readonly Uri Uri = new("https://example.test/fixture.bin");

    [TestMethod]
    public void Select_UnknownLength_ChoosesSingle()
    {
        var resource = Resource(length: null, supportsRanges: true);

        Assert.AreEqual(
            DownloadRunKind.Single,
            DownloadStrategy.Select(resource, new DownloadHostOptions()));
    }

    [TestMethod]
    public void Select_ZeroLength_ChoosesSingle()
    {
        var resource = Resource(length: 0, supportsRanges: true);

        Assert.AreEqual(
            DownloadRunKind.Single,
            DownloadStrategy.Select(resource, new DownloadHostOptions()));
    }

    [TestMethod]
    public void Select_WithoutByteRanges_ChoosesSingle()
    {
        var resource = Resource(length: 100, supportsRanges: false);

        Assert.AreEqual(
            DownloadRunKind.Single,
            DownloadStrategy.Select(resource, new DownloadHostOptions(Connections: 4, Segments: 4)));
    }

    [TestMethod]
    public void Select_MultipleConnectionsWithDynamicChunks_ChoosesDynamic()
    {
        var resource = Resource(length: 100, supportsRanges: true);

        Assert.AreEqual(
            DownloadRunKind.Dynamic,
            DownloadStrategy.Select(
                resource,
                new DownloadHostOptions(Connections: 4, Segments: 2, DynamicChunkSize: 64 * 1024)));
    }

    [TestMethod]
    public void Select_SingleConnectionWithSegments_ChoosesSegmented()
    {
        var resource = Resource(length: 100, supportsRanges: true);

        Assert.AreEqual(
            DownloadRunKind.Segmented,
            DownloadStrategy.Select(
                resource,
                new DownloadHostOptions(Connections: 1, Segments: 4, DynamicChunkSize: 0)));
    }

    [TestMethod]
    public void Select_SingleConnectionAndSingleSegment_ChoosesSingle()
    {
        var resource = Resource(length: 100, supportsRanges: true);

        Assert.AreEqual(
            DownloadRunKind.Single,
            DownloadStrategy.Select(
                resource,
                new DownloadHostOptions(Connections: 1, Segments: 1, DynamicChunkSize: 0)));
    }

    [TestMethod]
    public void Select_NullResource_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(
            () => DownloadStrategy.Select(null!, new DownloadHostOptions()));
    }

    [TestMethod]
    public void Select_RangeWithoutStrongIdentity_ChoosesSingle()
    {
        var resource = WeakIdentityResource(length: 100, supportsRanges: true);

        Assert.AreEqual(
            DownloadRunKind.Single,
            DownloadStrategy.Select(
                resource,
                new DownloadHostOptions(Connections: 4, Segments: 4, DynamicChunkSize: 64 * 1024)));
    }

    [TestMethod]
    public void Select_WeakEntityTag_ChoosesSingle()
    {
        var resource = new RemoteResourceInfo(
            Uri,
            Uri,
            100,
            SuggestedFileName: null,
            ContentType: null,
            EntityTag: "W/\"v1\"",
            LastModified: null,
            SupportsByteRanges: true);

        Assert.AreEqual(
            DownloadRunKind.Single,
            DownloadStrategy.Select(
                resource,
                new DownloadHostOptions(Connections: 4, Segments: 4, DynamicChunkSize: 64 * 1024)));
    }

    private static RemoteResourceInfo WeakIdentityResource(long? length, bool supportsRanges) =>
        new(
            Uri,
            Uri,
            length,
            SuggestedFileName: null,
            ContentType: null,
            EntityTag: null,
            LastModified: null,
            supportsRanges);

    private static RemoteResourceInfo Resource(long? length, bool supportsRanges) =>
        new(
            Uri,
            Uri,
            length,
            SuggestedFileName: null,
            ContentType: null,
            EntityTag: "\"v1\"",
            LastModified: null,
            supportsRanges);
}

[TestClass]
public sealed class ThrottledRemoteContentSourceTests
{
    private static readonly byte[] Content = [1, 2, 3, 4];

    [TestMethod]
    public async Task OpenReadAsync_ReadEachBlock_AcquiresTokensPerRead()
    {
        var acquired = new List<int>();
        var inner = new StubSource(Content);
        var throttled = new ThrottledRemoteContentSource(
            inner,
            (byteCount, cancellationToken) =>
            {
                acquired.Add(byteCount);
                return ValueTask.CompletedTask;
            });
        var resource = new RemoteResourceInfo(
            new Uri("https://example.test/f.bin"),
            new Uri("https://example.test/f.bin"),
            Content.Length,
            SuggestedFileName: null,
            ContentType: null,
            EntityTag: null,
            LastModified: null,
            SupportsByteRanges: true);

        await using var lease = await throttled.OpenReadAsync(resource, 0, CancellationToken.None);
        var buffer = new byte[4];
        var read = await lease.Content.ReadAsync(buffer);

        Assert.AreEqual(4, read);
        CollectionAssert.AreEqual(new[] { 4 }, acquired);
        CollectionAssert.AreEqual(Content, buffer);
    }

    [TestMethod]
    public async Task OpenReadAsync_NullResource_ThrowsArgumentNullException()
    {
        var throttled = new ThrottledRemoteContentSource(
            new StubSource(Content),
            (byteCount, cancellationToken) => ValueTask.CompletedTask);

        await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => throttled
            .OpenReadAsync(null!, 0, CancellationToken.None)
            .AsTask());
    }

    private sealed class StubSource(byte[] content) : IRemoteContentSource
    {
        public ValueTask<RemoteContentLease> OpenReadAsync(
            RemoteResourceInfo resource,
            long offset,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult<RemoteContentLease>(
                new(new MemoryStream(content, writable: false), content.Length));
    }
}
