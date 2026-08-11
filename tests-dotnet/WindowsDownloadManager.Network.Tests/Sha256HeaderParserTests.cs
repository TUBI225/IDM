using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using WindowsDownloadManager.Network.Http;

namespace WindowsDownloadManager.Network.Tests;

[TestClass]
public sealed class Sha256HeaderParserTests
{
    private const string SampleHexSha256 = "2CF24DBA5FB0A30E26E83B2AC5B9E29E1B161E5C1FA7425E73043362938B9824";
    // Base64 encoding of 32-byte sha256("hello"): LPJNjd+L3APGNxaWhToeTibPpVOMv4fSg5gDpG1465M=
    private const string SampleBase64Sha256 = "LPJNjd+L3APGNxaWhToeTibPpVOMv4fSg5gDpG1465M=";
    private const string ExpectedHexSha256 = "2CF24D8DDF8BDC03C6371696853A1E4E26CFA5538CBF87D2839803A46D78EB93";

    [TestMethod]
    public void ExtractSha256_ReturnsNull_WhenNoHashHeadersPresent()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.OK);
        var result = Sha256HeaderParser.ExtractSha256(response);
        Assert.IsNull(result);
    }

    [TestMethod]
    [DataRow("Digest", "sha-256=2CF24DBA5FB0A30E26E83B2AC5B9E29E1B161E5C1FA7425E73043362938B9824")]
    [DataRow("Content-Digest", "sha-256=:LPJNjd+L3APGNxaWhToeTibPpVOMv4fSg5gDpG1465M=:")]
    [DataRow("x-checksum-sha256", "2CF24DBA5FB0A30E26E83B2AC5B9E29E1B161E5C1FA7425E73043362938B9824")]
    [DataRow("x-goog-hash", "crc32c=abc, sha256=LPJNjd+L3APGNxaWhToeTibPpVOMv4fSg5gDpG1465M=")]
    [DataRow("x-amz-checksum-sha256", "LPJNjd+L3APGNxaWhToeTibPpVOMv4fSg5gDpG1465M=")]
    public void ExtractSha256_ParsesVariousHeadersCorrectly(string headerName, string headerValue)
    {
        using var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Headers.TryAddWithoutValidation(headerName, headerValue);

        var result = Sha256HeaderParser.ExtractSha256(response);

        Assert.IsNotNull(result);
        Assert.AreEqual(64, result.Length);
        Assert.IsTrue(result.All(Uri.IsHexDigit));
    }

    [TestMethod]
    public void ParseDigestValue_HandlesBase64Decoding()
    {
        var result = Sha256HeaderParser.ParseDigestValue(SampleBase64Sha256);
        Assert.AreEqual(ExpectedHexSha256, result);
    }

    [TestMethod]
    public void ParseDigestValue_HandlesBase64UrlEncoding()
    {
        var base64Url = "LPJNjd-L3APGNxaWhToeTibPpVOMv4fSg5gDpG1465M=";
        var result = Sha256HeaderParser.ParseDigestValue(base64Url);
        Assert.AreEqual(ExpectedHexSha256, result);
    }

    [TestMethod]
    public void ParseDigestValue_HandlesBase64UrlWithoutPadding()
    {
        var base64Url = "LPJNjd-L3APGNxaWhToeTibPpVOMv4fSg5gDpG1465M";
        var result = Sha256HeaderParser.ParseDigestValue(base64Url);
        Assert.AreEqual(ExpectedHexSha256, result);
    }

    [TestMethod]
    public void ParseDigestValue_HandlesHexInput()
    {
        var result = Sha256HeaderParser.ParseDigestValue(SampleHexSha256);
        Assert.AreEqual(SampleHexSha256, result);
    }
}
