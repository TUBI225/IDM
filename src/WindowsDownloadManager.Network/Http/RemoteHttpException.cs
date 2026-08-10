using System.Net;

namespace WindowsDownloadManager.Network.Http;

public sealed class RemoteHttpException : Exception
{
    public RemoteHttpException(HttpStatusCode statusCode, bool isTransient, TimeSpan? retryAfter)
        : base($"The remote server returned HTTP {(int)statusCode}.")
    {
        StatusCode = statusCode;
        IsTransient = isTransient;
        RetryAfter = retryAfter;
    }

    public HttpStatusCode StatusCode { get; }
    public bool IsTransient { get; }
    public TimeSpan? RetryAfter { get; }
}
