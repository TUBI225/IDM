using System.Net;
using System.Net.Http.Headers;
using WindowsDownloadManager.Application.Abstractions;
using WindowsDownloadManager.Domain.Downloads;

namespace WindowsDownloadManager.Network.Http;

public sealed class HttpRemoteContentSource : IRemoteContentSource, IRemoteRangeReader, IRemoteBoundedContentSource
{
    private const int MaximumRedirects = 10;
    private readonly HttpClient _httpClient;
    private readonly IUriSafetyValidator _uriSafetyValidator;

    public HttpRemoteContentSource(HttpClient httpClient, IUriSafetyValidator uriSafetyValidator)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _uriSafetyValidator = uriSafetyValidator ?? throw new ArgumentNullException(nameof(uriSafetyValidator));
    }

    public async ValueTask<RemoteContentLease> OpenReadAsync(
        RemoteResourceInfo resource,
        long offset,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(resource);
        if (offset < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }

        if (!resource.SupportsByteRanges && offset != 0)
        {
            throw new InvalidOperationException("This remote resource does not support a non-zero offset.");
        }

        var currentUri = resource.FinalUri;
        HttpResponseMessage? response = null;
        try
        {
            for (var redirect = 0; redirect <= MaximumRedirects; redirect++)
            {
                await _uriSafetyValidator.ValidateAsync(currentUri, cancellationToken).ConfigureAwait(false);
                using var request = CreateRequest(currentUri, resource, offset);
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

            var acceptedResponse = response
                ?? throw new HttpRequestException("The remote server returned no response.");
            ValidateResponse(acceptedResponse, resource, offset);
            var totalLength = resource.SupportsByteRanges
                ? acceptedResponse.Content.Headers.ContentRange?.Length
                : acceptedResponse.Content.Headers.ContentLength;
            var stream = await acceptedResponse.Content.ReadAsStreamAsync(cancellationToken)
                .ConfigureAwait(false);
            response = null;
            return new RemoteContentLease(stream, totalLength, acceptedResponse);
        }
        finally
        {
            response?.Dispose();
        }
    }

    public async ValueTask<RemoteContentLease> OpenBoundedReadAsync(
        RemoteResourceInfo resource,
        long start,
        long end,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(resource);
        if (start < 0 || end < start)
        {
            throw new ArgumentOutOfRangeException(nameof(start));
        }

        if (!resource.SupportsByteRanges)
        {
            throw new InvalidOperationException("This remote resource does not support bounded byte ranges.");
        }

        var currentUri = resource.FinalUri;
        HttpResponseMessage? response = null;
        try
        {
            for (var redirect = 0; redirect <= MaximumRedirects; redirect++)
            {
                await _uriSafetyValidator.ValidateAsync(currentUri, cancellationToken).ConfigureAwait(false);
                using var request = RangeRequestFactory.Create(currentUri, start, end);
                ApplyValidators(request, resource.EntityTag, resource.LastModified);
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

            var acceptedResponse = response
                ?? throw new HttpRequestException("The remote server returned no response.");
            ValidateBoundedRangeResponse(
                acceptedResponse,
                new RemoteIdentity(
                    resource.FinalUri,
                    resource.Length,
                    resource.EntityTag,
                    resource.LastModified,
                    resource.SupportsByteRanges),
                start,
                end,
                checked((int)(end - start + 1)));
            var stream = await acceptedResponse.Content
                .ReadAsStreamAsync(cancellationToken)
                .ConfigureAwait(false);
            response = null;
            return new RemoteContentLease(stream, resource.Length, acceptedResponse);
        }
        finally
        {
            response?.Dispose();
        }
    }

    public async ValueTask<ReadOnlyMemory<byte>> ReadRangeAsync(
        RemoteIdentity identity,
        long offset,
        int length,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(identity);
        if (offset < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }

        if (length <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(length));
        }

        if (!identity.SupportsByteRanges)
        {
            throw new InvalidOperationException("Overlap verification requires byte-range support.");
        }

        var end = checked(offset + length - 1L);
        if (identity.Length is { } identityLength && end >= identityLength)
        {
            throw new ArgumentOutOfRangeException(nameof(length), "The requested range exceeds the remote length.");
        }

        var currentUri = identity.FinalUri;
        HttpResponseMessage? response = null;
        try
        {
            for (var redirect = 0; redirect <= MaximumRedirects; redirect++)
            {
                await _uriSafetyValidator.ValidateAsync(currentUri, cancellationToken).ConfigureAwait(false);
                using var request = CreateBoundedRangeRequest(currentUri, identity, offset, end);
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

            var acceptedResponse = response
                ?? throw new HttpRequestException("The remote server returned no response.");
            ValidateBoundedRangeResponse(acceptedResponse, identity, offset, end, length);
            await using var stream = await acceptedResponse.Content
                .ReadAsStreamAsync(cancellationToken)
                .ConfigureAwait(false);
            var content = new byte[length];
            try
            {
                await stream.ReadExactlyAsync(content, cancellationToken).ConfigureAwait(false);
            }
            catch (HttpIOException exception) when (exception.HttpRequestError == HttpRequestError.ResponseEnded)
            {
                throw new EndOfStreamException("The bounded remote range ended prematurely.", exception);
            }

            var extra = new byte[1];
            if (await stream.ReadAsync(extra, cancellationToken).ConfigureAwait(false) != 0)
            {
                throw new InvalidDataException("The bounded remote range returned excess content.");
            }

            return content;
        }
        finally
        {
            response?.Dispose();
        }
    }

    private static HttpRequestMessage CreateRequest(Uri uri, RemoteResourceInfo resource, long offset)
    {
        var request = resource.SupportsByteRanges
            ? RangeRequestFactory.Create(uri, offset)
            : new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.AcceptEncoding.Clear();
        request.Headers.AcceptEncoding.ParseAdd("identity");

        ApplyValidators(request, resource.EntityTag, resource.LastModified);

        return request;
    }

    private static HttpRequestMessage CreateBoundedRangeRequest(
        Uri uri,
        RemoteIdentity identity,
        long offset,
        long end)
    {
        var request = RangeRequestFactory.Create(uri, offset, end);
        ApplyValidators(request, identity.EntityTag, identity.LastModified);
        return request;
    }

    private static void ApplyValidators(
        HttpRequestMessage request,
        string? entityTagValue,
        DateTimeOffset? lastModified)
    {
        if (EntityTagHeaderValue.TryParse(entityTagValue, out var entityTag) && !entityTag.IsWeak)
        {
            request.Headers.IfMatch.Add(entityTag);
        }
        else if (lastModified is { } date)
        {
            request.Headers.IfUnmodifiedSince = date;
        }
    }

    private static void ValidateResponse(
        HttpResponseMessage response,
        RemoteResourceInfo resource,
        long offset)
    {
        if (!response.IsSuccessStatusCode)
        {
            var transient = response.StatusCode == HttpStatusCode.TooManyRequests ||
                            (int)response.StatusCode is >= 500 and <= 599;
            throw new RemoteHttpException(response.StatusCode, transient, GetRetryAfter(response));
        }

        if (resource.SupportsByteRanges)
        {
            var range = response.Content.Headers.ContentRange;
            if (response.StatusCode != HttpStatusCode.PartialContent ||
                range is null ||
                !string.Equals(range.Unit, "bytes", StringComparison.OrdinalIgnoreCase) ||
                range.From != offset ||
                range.Length is null ||
                range.To != range.Length - 1)
            {
                throw new InvalidRangeResponseException(
                    "The transfer response did not match the requested open-ended byte range.");
            }
        }
        else if (response.StatusCode != HttpStatusCode.OK)
        {
            throw new InvalidDataException("A non-range transfer must return HTTP 200.");
        }

        if (resource.Length is { } analyzedLength)
        {
            var responseLength = resource.SupportsByteRanges
                ? response.Content.Headers.ContentRange?.Length
                : response.Content.Headers.ContentLength;
            if (responseLength is { } length && length != analyzedLength)
            {
                throw new InvalidDataException("The remote resource length changed after analysis.");
            }
        }
    }

    private static void ValidateBoundedRangeResponse(
        HttpResponseMessage response,
        RemoteIdentity identity,
        long offset,
        long end,
        int length)
    {
        if (!response.IsSuccessStatusCode)
        {
            var transient = response.StatusCode == HttpStatusCode.TooManyRequests ||
                            (int)response.StatusCode is >= 500 and <= 599;
            throw new RemoteHttpException(response.StatusCode, transient, GetRetryAfter(response));
        }

        var range = response.Content.Headers.ContentRange;
        if (response.StatusCode != HttpStatusCode.PartialContent ||
            range is null ||
            !string.Equals(range.Unit, "bytes", StringComparison.OrdinalIgnoreCase) ||
            range.From != offset ||
            range.To != end ||
            (identity.Length is { } identityLength && range.Length != identityLength) ||
            (response.Content.Headers.ContentLength is { } contentLength && contentLength != length))
        {
            throw new InvalidRangeResponseException(
                "The overlap response did not match the exact requested byte range.");
        }
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
