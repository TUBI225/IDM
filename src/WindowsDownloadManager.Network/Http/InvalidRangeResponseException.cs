namespace WindowsDownloadManager.Network.Http;

public sealed class InvalidRangeResponseException : Exception
{
    public InvalidRangeResponseException(string message)
        : base(message)
    {
    }
}
