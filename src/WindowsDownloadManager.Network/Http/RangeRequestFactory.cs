using System.Net.Http.Headers;

namespace WindowsDownloadManager.Network.Http;

public static class RangeRequestFactory
{
    public static HttpRequestMessage Create(Uri uri, long start, long? end = null)
    {
        ArgumentNullException.ThrowIfNull(uri);
        if (uri.Scheme is not ("http" or "https"))
        {
            throw new ArgumentException("Only HTTP and HTTPS are supported.", nameof(uri));
        }

        if (start < 0 || end < start)
        {
            throw new ArgumentOutOfRangeException(nameof(start), "The requested byte range is invalid.");
        }

        var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Range = new RangeHeaderValue(start, end);
        request.Headers.AcceptEncoding.ParseAdd("identity");
        return request;
    }
}
