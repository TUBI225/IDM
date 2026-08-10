using WindowsDownloadManager.Application.Abstractions;
using WindowsDownloadManager.Network.Http;

namespace WindowsDownloadManager.Network.Security;

public sealed class PublicHttpUriSafetyValidator : IUriSafetyValidator
{
    private readonly IHostAddressResolver _addressResolver;
    private readonly INetworkAddressPolicy _addressPolicy;

    public PublicHttpUriSafetyValidator()
        : this(new DnsHostAddressResolver(), new PublicNetworkAddressPolicy())
    {
    }

    public PublicHttpUriSafetyValidator(
        IHostAddressResolver addressResolver,
        INetworkAddressPolicy addressPolicy)
    {
        _addressResolver = addressResolver ?? throw new ArgumentNullException(nameof(addressResolver));
        _addressPolicy = addressPolicy ?? throw new ArgumentNullException(nameof(addressPolicy));
    }

    public async ValueTask ValidateAsync(Uri uri, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(uri);
        if (!uri.IsAbsoluteUri || uri.Scheme is not ("http" or "https"))
        {
            throw new UnsafeUriException("Only absolute HTTP and HTTPS URIs are allowed.");
        }

        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            throw new UnsafeUriException("Credentials embedded in a URI are not allowed.");
        }

        var addresses = await _addressResolver.ResolveAsync(uri.DnsSafeHost, cancellationToken)
            .ConfigureAwait(false);
        _addressPolicy.Validate(addresses);
    }
}
