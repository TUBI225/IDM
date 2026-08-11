using WindowsDownloadManager.Application.Abstractions;

namespace WindowsDownloadManager.Network.Http;

public sealed class HttpTransientFailureClassifier : ITransientFailureClassifier
{
    public bool IsTransient(Exception exception) => exception switch
    {
        RemoteHttpException http => http.IsTransient,
        HttpRequestException => true,
        IOException => true,
        TimeoutException => true,
        _ => false,
    };

    public TimeSpan? GetRetryAfter(Exception exception) =>
        (exception as RemoteHttpException)?.RetryAfter;
}
