using Microsoft.VisualStudio.TestTools.UnitTesting;
using WindowsDownloadManager.Application.Abstractions;
using WindowsDownloadManager.Domain.Downloads;
using WindowsDownloadManager.Network.Http;
using WindowsDownloadManager.Network.Security;

namespace WindowsDownloadManager.Network.Tests;

[TestClass]
public sealed class HttpRemoteContentSourceTests
{
    [TestMethod]
    public async Task OpenRead_RangeTransfer_ValidatesAndStreamsBody()
    {
        await using var server = new LoopbackHttpServer(
            "HTTP/1.1 206 Partial Content\r\n" +
            "Content-Range: bytes 5-9/10\r\nContent-Length: 5\r\n" +
            "ETag: \"v1\"\r\nConnection: close\r\n\r\nfghij");
        using var client = HttpNetworkClientFactory.Create(
            new DnsHostAddressResolver(),
            new AllowAllNetworkAddressPolicy());
        var source = new HttpRemoteContentSource(client, new RecordingUriSafetyValidator());
        var resource = Resource(server.Uri, length: 10, supportsRanges: true);

        await using var content = await source.OpenReadAsync(resource, 5, CancellationToken.None);
        using var output = new MemoryStream();
        await content.Content.CopyToAsync(output);

        CollectionAssert.AreEqual("fghij"u8.ToArray(), output.ToArray());
        Assert.AreEqual(10, content.TotalLength);
        Assert.IsTrue(server.RequestText?.Contains("Range: bytes=5-", StringComparison.OrdinalIgnoreCase) == true);
        Assert.IsTrue(server.RequestText?.Contains("If-Match: \"v1\"", StringComparison.OrdinalIgnoreCase) == true);
        Assert.IsTrue(server.RequestText?.Contains("Accept-Encoding: identity", StringComparison.OrdinalIgnoreCase) == true);
    }

    [TestMethod]
    public async Task OpenRead_RangeIgnoredByServer_IsRejected()
    {
        await using var server = new LoopbackHttpServer(
            "HTTP/1.1 200 OK\r\nContent-Length: 3\r\nConnection: close\r\n\r\nabc");
        using var client = HttpNetworkClientFactory.Create(
            new DnsHostAddressResolver(),
            new AllowAllNetworkAddressPolicy());
        var source = new HttpRemoteContentSource(client, new RecordingUriSafetyValidator());

        await Assert.ThrowsExactlyAsync<InvalidRangeResponseException>(async () =>
            await source.OpenReadAsync(Resource(server.Uri, 3, supportsRanges: true), 0, CancellationToken.None));
    }

    [TestMethod]
    public async Task OpenRead_NonRangeResource_NonZeroOffsetIsRejectedBeforeNetwork()
    {
        await using var server = new LoopbackHttpServer(
            "HTTP/1.1 200 OK\r\nContent-Length: 3\r\nConnection: close\r\n\r\nabc");
        using var client = HttpNetworkClientFactory.Create(
            new DnsHostAddressResolver(),
            new AllowAllNetworkAddressPolicy());
        var source = new HttpRemoteContentSource(client, new RecordingUriSafetyValidator());

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () =>
            await source.OpenReadAsync(Resource(server.Uri, 3, supportsRanges: false), 1, CancellationToken.None));

        Assert.IsNull(server.RequestText);
    }

    [TestMethod]
    public async Task ReadRange_ExactBoundedResponse_ReturnsOnlyRequestedBytes()
    {
        await using var server = new LoopbackHttpServer(
            "HTTP/1.1 206 Partial Content\r\n" +
            "Content-Range: bytes 2-4/5\r\nContent-Length: 3\r\n" +
            "ETag: \"v1\"\r\nConnection: close\r\n\r\ncde");
        using var client = HttpNetworkClientFactory.Create(
            new DnsHostAddressResolver(),
            new AllowAllNetworkAddressPolicy());
        var source = new HttpRemoteContentSource(client, new RecordingUriSafetyValidator());

        var content = await source.ReadRangeAsync(
            Identity(server.Uri, length: 5),
            offset: 2,
            length: 3,
            CancellationToken.None);

        CollectionAssert.AreEqual("cde"u8.ToArray(), content.ToArray());
        Assert.IsTrue(server.RequestText?.Contains("Range: bytes=2-4", StringComparison.OrdinalIgnoreCase) == true);
        Assert.IsTrue(server.RequestText?.Contains("If-Match: \"v1\"", StringComparison.OrdinalIgnoreCase) == true);
        Assert.IsTrue(server.RequestText?.Contains("Accept-Encoding: identity", StringComparison.OrdinalIgnoreCase) == true);
    }

    [TestMethod]
    public async Task ReadRange_WrongContentRange_IsRejected()
    {
        await using var server = new LoopbackHttpServer(
            "HTTP/1.1 206 Partial Content\r\n" +
            "Content-Range: bytes 1-3/5\r\nContent-Length: 3\r\n" +
            "Connection: close\r\n\r\nbcd");
        using var client = HttpNetworkClientFactory.Create(
            new DnsHostAddressResolver(),
            new AllowAllNetworkAddressPolicy());
        var source = new HttpRemoteContentSource(client, new RecordingUriSafetyValidator());

        await Assert.ThrowsExactlyAsync<InvalidRangeResponseException>(async () =>
            await source.ReadRangeAsync(
                Identity(server.Uri, length: 5),
                offset: 2,
                length: 3,
                CancellationToken.None));
    }

    [TestMethod]
    public async Task ReadRange_ShortBody_IsRejected()
    {
        await using var server = new LoopbackHttpServer(
            "HTTP/1.1 206 Partial Content\r\n" +
            "Content-Range: bytes 2-4/5\r\nContent-Length: 3\r\n" +
            "Connection: close\r\n\r\ncd");
        using var client = HttpNetworkClientFactory.Create(
            new DnsHostAddressResolver(),
            new AllowAllNetworkAddressPolicy());
        var source = new HttpRemoteContentSource(client, new RecordingUriSafetyValidator());

        await Assert.ThrowsExactlyAsync<EndOfStreamException>(async () =>
            await source.ReadRangeAsync(
                Identity(server.Uri, length: 5),
                offset: 2,
                length: 3,
                CancellationToken.None));
    }

    [TestMethod]
    public async Task ReadRange_WithoutByteRangeSupport_IsRejectedBeforeNetwork()
    {
        await using var server = new LoopbackHttpServer(
            "HTTP/1.1 200 OK\r\nContent-Length: 3\r\nConnection: close\r\n\r\nabc");
        using var client = HttpNetworkClientFactory.Create(
            new DnsHostAddressResolver(),
            new AllowAllNetworkAddressPolicy());
        var source = new HttpRemoteContentSource(client, new RecordingUriSafetyValidator());

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () =>
            await source.ReadRangeAsync(
                Identity(server.Uri, length: 3, supportsRanges: false),
                offset: 0,
                length: 3,
                CancellationToken.None));

        Assert.IsNull(server.RequestText);
    }

    [TestMethod]
    public async Task ReadRange_Redirect_RevalidatesEveryUri()
    {
        await using var target = new LoopbackHttpServer(
            "HTTP/1.1 206 Partial Content\r\n" +
            "Content-Range: bytes 0-2/3\r\nContent-Length: 3\r\n" +
            "Connection: close\r\n\r\nabc");
        await using var redirect = new LoopbackHttpServer(
            "HTTP/1.1 302 Found\r\nLocation: " + target.Uri.AbsoluteUri +
            "\r\nContent-Length: 0\r\nConnection: close\r\n\r\n");
        using var client = HttpNetworkClientFactory.Create(
            new DnsHostAddressResolver(),
            new AllowAllNetworkAddressPolicy());
        var validator = new RecordingUriSafetyValidator();
        var source = new HttpRemoteContentSource(client, validator);

        var content = await source.ReadRangeAsync(
            Identity(redirect.Uri, length: 3),
            offset: 0,
            length: 3,
            CancellationToken.None);

        CollectionAssert.AreEqual("abc"u8.ToArray(), content.ToArray());
        CollectionAssert.AreEqual(new[] { redirect.Uri, target.Uri }, validator.ValidatedUris);
    }

    [TestMethod]
    public async Task ReadRange_RedirectTargetRejected_DoesNotContactTarget()
    {
        await using var target = new LoopbackHttpServer(
            "HTTP/1.1 206 Partial Content\r\n" +
            "Content-Range: bytes 0-2/3\r\nContent-Length: 3\r\n" +
            "Connection: close\r\n\r\nabc");
        await using var redirect = new LoopbackHttpServer(
            "HTTP/1.1 302 Found\r\nLocation: " + target.Uri.AbsoluteUri +
            "\r\nContent-Length: 0\r\nConnection: close\r\n\r\n");
        using var client = HttpNetworkClientFactory.Create(
            new DnsHostAddressResolver(),
            new AllowAllNetworkAddressPolicy());
        var validator = new RecordingUriSafetyValidator(uri => uri == target.Uri);
        var source = new HttpRemoteContentSource(client, validator);

        await Assert.ThrowsExactlyAsync<UnsafeUriException>(async () =>
            await source.ReadRangeAsync(
                Identity(redirect.Uri, length: 3),
                offset: 0,
                length: 3,
                CancellationToken.None));

        Assert.IsNull(target.RequestText);
    }

    private static RemoteResourceInfo Resource(Uri uri, long length, bool supportsRanges) =>
        new(uri, uri, length, null, "application/octet-stream", "\"v1\"", null, supportsRanges);

    private static RemoteIdentity Identity(Uri uri, long length, bool supportsRanges = true) =>
        new(uri, length, "\"v1\"", null, supportsRanges);
}
