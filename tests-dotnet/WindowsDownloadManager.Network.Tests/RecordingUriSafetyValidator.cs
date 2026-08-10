using WindowsDownloadManager.Application.Abstractions;
using WindowsDownloadManager.Network.Security;

namespace WindowsDownloadManager.Network.Tests;

internal sealed class RecordingUriSafetyValidator : IUriSafetyValidator
{
    private readonly Func<Uri, bool> _reject;

    public RecordingUriSafetyValidator(Func<Uri, bool>? reject = null)
    {
        _reject = reject ?? (_ => false);
    }

    public List<Uri> ValidatedUris { get; } = [];

    public ValueTask ValidateAsync(Uri uri, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidatedUris.Add(uri);
        if (_reject(uri))
        {
            throw new UnsafeUriException("The test policy rejected the URI.");
        }

        return ValueTask.CompletedTask;
    }
}
