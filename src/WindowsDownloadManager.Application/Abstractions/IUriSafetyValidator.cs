namespace WindowsDownloadManager.Application.Abstractions;

public interface IUriSafetyValidator
{
    ValueTask ValidateAsync(Uri uri, CancellationToken cancellationToken);
}
