using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using WindowsDownloadManager.Network.Http;
using WindowsDownloadManager.Network.Security;

namespace WindowsDownloadManager.Network.Tests;

[TestClass]
public sealed class HttpRemoteResourceAnalyzerTests
{
    [TestMethod]
    public void RangeRequest_IsExact()
    {
        using var request = RangeRequestFactory.Create(
            new Uri("https://example.test/file.bin"),
            100,
            199);

        Assert.AreEqual("bytes=100-199", request.Headers.Range?.ToString());
    }

    [TestMethod]
    public async Task Analyze_Valid206Probe_IsAccepted()
    {
        await using var server = new LoopbackHttpServer(
            "HTTP/1.1 206 Partial Content\r\n" +
            "Content-Range: bytes 0-0/1000\r\nContent-Length: 1\r\n" +
            "Content-Type: application/octet-stream\r\n" +
            "Content-Disposition: attachment; filename=fixture.bin\r\n" +
            "ETag: \"v1\"\r\nConnection: close\r\n\r\nx");
        using var client = LoopbackClient();
        var analyzer = Analyzer(client);

        var info = await analyzer.AnalyzeAsync(server.Uri, CancellationToken.None);

        Assert.IsTrue(info.SupportsByteRanges);
        Assert.AreEqual(1000, info.Length);
        Assert.AreEqual("fixture.bin", info.SuggestedFileName);
        Assert.IsTrue(
            server.RequestText?.Contains("Range: bytes=0-0", StringComparison.OrdinalIgnoreCase) == true,
            "The probe did not request exactly one byte.");
    }

    [TestMethod]
    public async Task Analyze_200Response_DisablesByteRanges()
    {
        await using var server = new LoopbackHttpServer(
            "HTTP/1.1 200 OK\r\nContent-Length: 3\r\nConnection: close\r\n\r\nabc");
        using var client = LoopbackClient();
        var analyzer = Analyzer(client);

        var info = await analyzer.AnalyzeAsync(server.Uri, CancellationToken.None);

        Assert.IsFalse(info.SupportsByteRanges);
        Assert.AreEqual(3, info.Length);
    }

    [TestMethod]
    public async Task Analyze_Malformed206_IsRejected()
    {
        await using var server = new LoopbackHttpServer(
            "HTTP/1.1 206 Partial Content\r\nContent-Range: bytes 1-1/1000\r\n" +
            "Content-Length: 1\r\nConnection: close\r\n\r\nx");
        using var client = LoopbackClient();
        var analyzer = Analyzer(client);

        await AssertThrowsExactlyAsync<InvalidRangeResponseException>(() =>
            analyzer.AnalyzeAsync(server.Uri, CancellationToken.None).AsTask());
    }

    [TestMethod]
    public async Task Validate_PrivateAndDocumentationAddresses_AreRejected()
    {
        var validator = new PublicHttpUriSafetyValidator();

        await AssertThrowsExactlyAsync<UnsafeUriException>(() =>
            validator.ValidateAsync(new Uri("http://127.0.0.1/file.bin"), CancellationToken.None).AsTask());
        await AssertThrowsExactlyAsync<UnsafeUriException>(() =>
            validator.ValidateAsync(new Uri("http://192.0.2.1/file.bin"), CancellationToken.None).AsTask());
    }

    [TestMethod]
    public async Task Analyze_Redirect_ValidatesEveryHop()
    {
        await using var destination = new LoopbackHttpServer(
            "HTTP/1.1 206 Partial Content\r\nContent-Range: bytes 0-0/10\r\n" +
            "Content-Length: 1\r\nConnection: close\r\n\r\nx");
        await using var redirect = new LoopbackHttpServer(
            $"HTTP/1.1 302 Found\r\nLocation: {destination.Uri}\r\n" +
            "Content-Length: 0\r\nConnection: close\r\n\r\n");
        var validator = new RecordingUriSafetyValidator();
        using var client = LoopbackClient();
        var analyzer = new HttpRemoteResourceAnalyzer(client, validator);

        var info = await analyzer.AnalyzeAsync(redirect.Uri, CancellationToken.None);

        Assert.AreEqual(destination.Uri, info.FinalUri);
        Assert.AreEqual(2, validator.ValidatedUris.Count);
        Assert.AreEqual(redirect.Uri, validator.ValidatedUris[0]);
        Assert.AreEqual(destination.Uri, validator.ValidatedUris[1]);
    }

    [TestMethod]
    public async Task Analyze_RedirectToRejectedDestination_StopsBeforeConnecting()
    {
        await using var destination = new LoopbackHttpServer(
            "HTTP/1.1 200 OK\r\nContent-Length: 0\r\nConnection: close\r\n\r\n");
        await using var redirect = new LoopbackHttpServer(
            $"HTTP/1.1 302 Found\r\nLocation: {destination.Uri}\r\n" +
            "Content-Length: 0\r\nConnection: close\r\n\r\n");
        var validator = new RecordingUriSafetyValidator(uri => uri == destination.Uri);
        using var client = LoopbackClient();
        var analyzer = new HttpRemoteResourceAnalyzer(client, validator);

        await AssertThrowsExactlyAsync<UnsafeUriException>(() =>
            analyzer.AnalyzeAsync(redirect.Uri, CancellationToken.None).AsTask());

        Assert.AreEqual(2, validator.ValidatedUris.Count);
        Assert.IsNull(destination.RequestText, "The rejected destination must not receive a request.");
    }

    [TestMethod]
    public async Task Analyze_EmptyFile416_IsRecognized()
    {
        await using var server = new LoopbackHttpServer(
            "HTTP/1.1 416 Range Not Satisfiable\r\nContent-Range: bytes */0\r\n" +
            "Content-Length: 0\r\nConnection: close\r\n\r\n");
        using var client = LoopbackClient();
        var analyzer = Analyzer(client);

        var info = await analyzer.AnalyzeAsync(server.Uri, CancellationToken.None);

        Assert.AreEqual(0, info.Length);
        Assert.IsFalse(info.SupportsByteRanges);
    }

    [TestMethod]
    public async Task Analyze_429_PreservesRetryAfter()
    {
        await using var server = new LoopbackHttpServer(
            "HTTP/1.1 429 Too Many Requests\r\nRetry-After: 30\r\n" +
            "Content-Length: 0\r\nConnection: close\r\n\r\n");
        using var client = LoopbackClient();
        var analyzer = Analyzer(client);

        var exception = await CaptureAsync<RemoteHttpException>(() =>
            analyzer.AnalyzeAsync(server.Uri, CancellationToken.None).AsTask());

        Assert.IsTrue(exception.IsTransient);
        Assert.AreEqual(TimeSpan.FromSeconds(30), exception.RetryAfter);
    }

    [TestMethod]
    public async Task Analyze_503_IsTransient()
    {
        await using var server = new LoopbackHttpServer(
            "HTTP/1.1 503 Service Unavailable\r\nContent-Length: 0\r\n" +
            "Connection: close\r\n\r\n");
        using var client = LoopbackClient();
        var analyzer = Analyzer(client);

        var exception = await CaptureAsync<RemoteHttpException>(() =>
            analyzer.AnalyzeAsync(server.Uri, CancellationToken.None).AsTask());

        Assert.IsTrue(exception.IsTransient);
    }

    [TestMethod]
    public async Task Analyze_Cancellation_IsPropagated()
    {
        await using var server = new LoopbackHttpServer(
            "HTTP/1.1 200 OK\r\nContent-Length: 0\r\nConnection: close\r\n\r\n",
            TimeSpan.FromMilliseconds(250));
        using var client = LoopbackClient();
        var analyzer = Analyzer(client);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(30));

        await AssertThrowsExactlyAsync<OperationCanceledException>(() =>
            analyzer.AnalyzeAsync(server.Uri, cancellation.Token).AsTask());
    }

    [TestMethod]
    public async Task Analyze_DnsRebindingToLoopback_IsRejectedBeforeConnection()
    {
        await using var destination = new LoopbackHttpServer(
            "HTTP/1.1 200 OK\r\nContent-Length: 0\r\nConnection: close\r\n\r\n");
        var resolver = new SequenceHostAddressResolver(
            new[] { IPAddress.Parse("93.184.216.34") },
            new[] { IPAddress.Loopback });
        var policy = new PublicNetworkAddressPolicy();
        var validator = new PublicHttpUriSafetyValidator(resolver, policy);
        using var client = HttpNetworkClientFactory.Create(resolver, policy);
        var analyzer = new HttpRemoteResourceAnalyzer(client, validator);

        var exception = await CaptureAsync<HttpRequestException>(() =>
            analyzer.AnalyzeAsync(destination.CreateUri("rebind.test"), CancellationToken.None).AsTask());

        Assert.AreEqual(2, resolver.CallCount);
        Assert.IsInstanceOfType<UnsafeUriException>(exception.InnerException);
        Assert.IsNull(destination.RequestText, "The rebound private address must not receive a request.");
    }

    [TestMethod]
    public void PublicAddressPolicy_MixedPublicAndPrivateResult_IsRejected()
    {
        var policy = new PublicNetworkAddressPolicy();

        Assert.ThrowsExactly<UnsafeUriException>(() => policy.Validate(
            new[] { IPAddress.Parse("93.184.216.34"), IPAddress.Parse("10.0.0.1") }));
    }

    [TestMethod]
    public void PublicAddressPolicy_PublicIpv4AndIpv6_AreAccepted()
    {
        var policy = new PublicNetworkAddressPolicy();

        policy.Validate(new[] { IPAddress.Parse("93.184.216.34"), IPAddress.Parse("2606:2800:220:1:248:1893:25c8:1946") });
    }

    [TestMethod]
    public async Task Analyze_ExtractsSha256Header_WhenPresent()
    {
        await using var server = new LoopbackHttpServer(
            "HTTP/1.1 200 OK\r\nContent-Length: 5\r\n" +
            "Digest: sha-256=2CF24DBA5FB0A30E26E83B2AC5B9E29E1B161E5C1FA7425E73043362938B9824\r\n" +
            "Connection: close\r\n\r\nhello");
        using var client = LoopbackClient();
        var analyzer = Analyzer(client);

        var info = await analyzer.AnalyzeAsync(server.Uri, CancellationToken.None);

        Assert.AreEqual("2CF24DBA5FB0A30E26E83B2AC5B9E29E1B161E5C1FA7425E73043362938B9824", info.Sha256);
    }

    [TestMethod]
    public void HttpHandler_UsesExplicitSafeDefaults()
    {
        using var handler = HttpNetworkClientFactory.CreateHandler(
            new DnsHostAddressResolver(),
            new PublicNetworkAddressPolicy());

        Assert.IsFalse(handler.AllowAutoRedirect);
        Assert.IsFalse(handler.UseProxy);
        Assert.AreEqual(DecompressionMethods.None, handler.AutomaticDecompression);
        Assert.IsNotNull(handler.ConnectCallback);
    }

    private static HttpClient LoopbackClient() => HttpNetworkClientFactory.Create(
        new DnsHostAddressResolver(),
        new AllowAllNetworkAddressPolicy());

    private static HttpRemoteResourceAnalyzer Analyzer(HttpClient client) =>
        new(client, new RecordingUriSafetyValidator());

    private static async Task AssertThrowsExactlyAsync<TException>(Func<Task> action)
        where TException : Exception
    {
        _ = await CaptureAsync<TException>(action);
    }

    private static async Task<TException> CaptureAsync<TException>(Func<Task> action)
        where TException : Exception
    {
        try
        {
            await action();
        }
        catch (TException exception)
        {
            return exception;
        }

        Assert.Fail($"Expected {typeof(TException).Name}.");
        throw new InvalidOperationException("Unreachable assertion path.");
    }
}
