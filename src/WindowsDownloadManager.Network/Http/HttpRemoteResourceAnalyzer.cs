using System.Net;
using WindowsDownloadManager.Application.Abstractions;

namespace WindowsDownloadManager.Network.Http;

public sealed class HttpRemoteResourceAnalyzer : IRemoteResourceAnalyzer
{
    private const int MaximumRedirects = 10;
    private readonly HttpClient _httpClient;
    private readonly IUriSafetyValidator _uriSafetyValidator;

    public HttpRemoteResourceAnalyzer(
        HttpClient httpClient,
        IUriSafetyValidator uriSafetyValidator)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _uriSafetyValidator = uriSafetyValidator ?? throw new ArgumentNullException(nameof(uriSafetyValidator));
    }

    public async ValueTask<RemoteResourceInfo> AnalyzeAsync(
        Uri uri,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(uri);
        var currentUri = uri;
        HttpResponseMessage? response = null;
        try
        {
            for (var redirect = 0; redirect <= MaximumRedirects; redirect++)
            {
                await _uriSafetyValidator.ValidateAsync(currentUri, cancellationToken).ConfigureAwait(false);
                using var request = RangeRequestFactory.Create(currentUri, 0, 0);
                response = await _httpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken).ConfigureAwait(false);

                if (!IsRedirect(response.StatusCode))
                {
                    break;
                }

                if (redirect == MaximumRedirects || response.Headers.Location is null)
                {
                    throw new HttpRequestException("The redirect chain is invalid or too long.");
                }

                currentUri = response.Headers.Location.IsAbsoluteUri
                    ? response.Headers.Location
                    : new Uri(currentUri, response.Headers.Location);
                response.Dispose();
                response = null;
            }

            return ParseResponse(uri, currentUri, response
                ?? throw new HttpRequestException("The remote server returned no response."));
        }
        finally
        {
            response?.Dispose();
        }
    }

    private static RemoteResourceInfo ParseResponse(Uri originalUri, Uri finalUri, HttpResponseMessage response)
    {
        if (response.StatusCode == HttpStatusCode.RequestedRangeNotSatisfiable &&
            response.Content.Headers.ContentRange?.Length == 0)
        {
            return CreateInfo(originalUri, finalUri, response, 0, supportsByteRanges: false);
        }

        if (!response.IsSuccessStatusCode)
        {
            var transient = response.StatusCode == HttpStatusCode.TooManyRequests ||
                            (int)response.StatusCode is >= 500 and <= 599;
            throw new RemoteHttpException(response.StatusCode, transient, GetRetryAfter(response));
        }

        var supportsByteRanges = response.StatusCode == HttpStatusCode.PartialContent;
        long? length;

        if (supportsByteRanges)
        {
            var range = response.Content.Headers.ContentRange;
            if (range is null ||
                !string.Equals(range.Unit, "bytes", StringComparison.OrdinalIgnoreCase) ||
                range.From != 0 ||
                range.To != 0 ||
                range.Length is null ||
                range.Length <= 0)
            {
                throw new InvalidRangeResponseException(
                    "The server returned 206 without the exact requested Content-Range bytes 0-0/length.");
            }

            length = range.Length;
        }
        else
        {
            length = response.Content.Headers.ContentLength;
        }

        return CreateInfo(originalUri, finalUri, response, length, supportsByteRanges);
    }

    private static RemoteResourceInfo CreateInfo(
        Uri originalUri,
        Uri finalUri,
        HttpResponseMessage response,
        long? length,
        bool supportsByteRanges)
    {
        var disposition = response.Content.Headers.ContentDisposition;
        var suggestedFileName = disposition?.FileNameStar ?? disposition?.FileName?.Trim('"');

        return new RemoteResourceInfo(
            originalUri,
            finalUri,
            length,
            suggestedFileName,
            response.Content.Headers.ContentType?.MediaType,
            response.Headers.ETag?.ToString(),
            response.Content.Headers.LastModified,
            supportsByteRanges);
    }

    private static bool IsRedirect(HttpStatusCode statusCode) => statusCode is
        HttpStatusCode.MovedPermanently or HttpStatusCode.Redirect or HttpStatusCode.SeeOther or
        HttpStatusCode.TemporaryRedirect or HttpStatusCode.PermanentRedirect;

    private static TimeSpan? GetRetryAfter(HttpResponseMessage response)
    {
        var retryAfter = response.Headers.RetryAfter;
        if (retryAfter?.Delta is { } delta)
        {
            return delta;
        }

        return retryAfter?.Date is { } date
            ? TimeSpan.FromTicks(Math.Max(0, (date - DateTimeOffset.UtcNow).Ticks))
            : null;
    }
}
