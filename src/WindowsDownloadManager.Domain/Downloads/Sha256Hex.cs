namespace WindowsDownloadManager.Domain.Downloads;

public static class Sha256Hex
{
    public static string Normalize(string sha256, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sha256, parameterName);
        if (sha256.Length != 64 || sha256.Any(character => !Uri.IsHexDigit(character)))
        {
            throw new ArgumentException("SHA-256 must contain exactly 64 hexadecimal characters.", parameterName);
        }

        return sha256.ToUpperInvariant();
    }
}
