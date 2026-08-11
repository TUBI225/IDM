using System.Net.Http.Headers;

namespace WindowsDownloadManager.Network.Http;

public static class Sha256HeaderParser
{
    private static readonly string[] Sha256HeaderNames =
    [
        "Content-Digest",
        "Digest",
        "x-checksum-sha256",
        "x-sha256-checksum",
        "x-goog-hash",
        "x-amz-checksum-sha256"
    ];

    public static string? ExtractSha256(HttpResponseMessage response)
    {
        ArgumentNullException.ThrowIfNull(response);

        foreach (var headerName in Sha256HeaderNames)
        {
            if (TryGetHeaderValues(response, headerName, out var values))
            {
                foreach (var value in values)
                {
                    var parsed = ParseDigestValue(value);
                    if (parsed is not null)
                    {
                        return parsed;
                    }
                }
            }
        }

        return null;
    }

    private static bool TryGetHeaderValues(
        HttpResponseMessage response,
        string headerName,
        out IEnumerable<string> values)
    {
        if (response.Headers.TryGetValues(headerName, out var headerValues))
        {
            values = headerValues;
            return true;
        }

        if (response.Content.Headers.TryGetValues(headerName, out var contentHeaderValues))
        {
            values = contentHeaderValues;
            return true;
        }

        values = Enumerable.Empty<string>();
        return false;
    }

    public static string? ParseDigestValue(string rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return null;
        }

        var tokens = rawValue.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var token in tokens)
        {
            var match = token;
            if (match.StartsWith("sha-256=", StringComparison.OrdinalIgnoreCase))
            {
                match = match["sha-256=".Length..].Trim();
            }
            else if (match.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase))
            {
                match = match["sha256=".Length..].Trim();
            }
            else if (match.StartsWith("SHA-256=", StringComparison.OrdinalIgnoreCase))
            {
                match = match["SHA-256=".Length..].Trim();
            }

            match = match.Trim(':', '"', '\'');

            var normalized = TryNormalizeSha256(match);
            if (normalized is not null)
            {
                return normalized;
            }
        }

        return TryNormalizeSha256(rawValue.Trim(':', '"', '\''));
    }

    private static string? TryNormalizeSha256(string candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return null;
        }

        if (candidate.Length == 64 && candidate.All(Uri.IsHexDigit))
        {
            return candidate.ToUpperInvariant();
        }

        try
        {
            var base64 = candidate.Replace('-', '+').Replace('_', '/');
            switch (base64.Length % 4)
            {
                case 2:
                    base64 += "==";
                    break;
                case 3:
                    base64 += "=";
                    break;
            }

            var bytes = Convert.FromBase64String(base64);
            if (bytes.Length == 32)
            {
                return Convert.ToHexString(bytes).ToUpperInvariant();
            }
        }
        catch (FormatException)
        {
            // Not a valid base64 string
        }

        return null;
    }
}
