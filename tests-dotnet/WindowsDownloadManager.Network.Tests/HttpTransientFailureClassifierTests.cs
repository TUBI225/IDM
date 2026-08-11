using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WindowsDownloadManager.Network.Http;

namespace WindowsDownloadManager.Network.Tests;

[TestClass]
public sealed class HttpTransientFailureClassifierTests
{
    private static readonly HttpTransientFailureClassifier Classifier = new();

    [TestMethod]
    [DataRow(HttpStatusCode.TooManyRequests)]
    [DataRow(HttpStatusCode.InternalServerError)]
    [DataRow(HttpStatusCode.BadGateway)]
    [DataRow(HttpStatusCode.ServiceUnavailable)]
    [DataRow(HttpStatusCode.GatewayTimeout)]
    public void IsTransient_ReturnsTrueForTransientStatusCodes(HttpStatusCode statusCode)
    {
        var exception = new RemoteHttpException(statusCode, isTransient: true, retryAfter: null);
        Assert.IsTrue(Classifier.IsTransient(exception));
    }

    [TestMethod]
    public void IsTransient_ReturnsFalseForPermanentHttpFailure()
    {
        var exception = new RemoteHttpException(HttpStatusCode.NotFound, isTransient: false, retryAfter: null);
        Assert.IsFalse(Classifier.IsTransient(exception));
    }

    [TestMethod]
    public void IsTransient_ReturnsTrueForHttpRequestException()
    {
        Assert.IsTrue(Classifier.IsTransient(new HttpRequestException("connection reset")));
    }

    [TestMethod]
    public void IsTransient_ReturnsTrueForIoAndTimeoutExceptions()
    {
        Assert.IsTrue(Classifier.IsTransient(new IOException("disk transient")));
        Assert.IsTrue(Classifier.IsTransient(new TimeoutException("slow")));
    }

    [TestMethod]
    public void IsTransient_ReturnsFalseForOtherExceptions()
    {
        Assert.IsFalse(Classifier.IsTransient(new InvalidOperationException("permanent")));
    }

    [TestMethod]
    public void GetRetryAfter_ReturnsServerHint()
    {
        var exception = new RemoteHttpException(
            HttpStatusCode.TooManyRequests,
            isTransient: true,
            retryAfter: TimeSpan.FromSeconds(3));
        Assert.AreEqual(TimeSpan.FromSeconds(3), Classifier.GetRetryAfter(exception));
    }

    [TestMethod]
    public void GetRetryAfter_ReturnsNullWhenAbsentOrNonHttp()
    {
        var exception = new RemoteHttpException(
            HttpStatusCode.ServiceUnavailable,
            isTransient: true,
            retryAfter: null);
        Assert.IsNull(Classifier.GetRetryAfter(exception));
        Assert.IsNull(Classifier.GetRetryAfter(new IOException("no hint")));
    }
}
