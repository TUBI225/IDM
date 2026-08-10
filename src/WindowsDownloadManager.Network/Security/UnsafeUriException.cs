namespace WindowsDownloadManager.Network.Security;

public sealed class UnsafeUriException : Exception
{
    public UnsafeUriException(string message)
        : base(message)
    {
    }
}
