using WindowsDownloadManager.Application.Abstractions;
using WindowsDownloadManager.Domain.Downloads;

namespace WindowsDownloadManager.Application.Downloads;

public sealed class RemoteIdentityReconciler(IRemoteResourceAnalyzer remoteResourceAnalyzer)
{
    private const RemoteIdentityDifference Contradictions =
        RemoteIdentityDifference.FinalUriChanged |
        RemoteIdentityDifference.LengthChanged |
        RemoteIdentityDifference.EntityTagChanged |
        RemoteIdentityDifference.LastModifiedChanged;

    private const RemoteIdentityDifference MissingEvidence =
        RemoteIdentityDifference.LengthEvidenceMissing |
        RemoteIdentityDifference.EntityTagEvidenceMissing |
        RemoteIdentityDifference.LastModifiedEvidenceMissing |
        RemoteIdentityDifference.SufficientIdentityEvidenceMissing;

    public async ValueTask<RemoteIdentityReconciliationResult> ReconcileAsync(
        DownloadTask task,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(task);

        if (task.RemoteIdentity is null)
        {
            return new RemoteIdentityReconciliationResult(
                task.Id,
                RemoteIdentityReconciliationStatus.RecoveryMetadataAbsent,
                RemoteIdentityDifference.None,
                PersistedIdentity: null,
                ObservedIdentity: null);
        }

        var resource = await remoteResourceAnalyzer
            .AnalyzeAsync(task.OriginalUri, cancellationToken)
            .ConfigureAwait(false);
        var persistedIdentity = NormalizeIdentity(task.RemoteIdentity);
        var observedIdentity = new RemoteIdentity(
            NormalizeUri(resource.FinalUri),
            resource.Length,
            resource.EntityTag,
            resource.LastModified,
            resource.SupportsByteRanges);
        var differences = Compare(persistedIdentity, observedIdentity);

        return new RemoteIdentityReconciliationResult(
            task.Id,
            Classify(differences),
            differences,
            persistedIdentity,
            observedIdentity);
    }

    private static RemoteIdentityDifference Compare(
        RemoteIdentity persisted,
        RemoteIdentity observed)
    {
        var differences = RemoteIdentityDifference.None;

        if (!UrisMatch(persisted.FinalUri, observed.FinalUri))
        {
            differences |= RemoteIdentityDifference.FinalUriChanged;
        }

        differences |= CompareOptional(
            persisted.Length,
            observed.Length,
            RemoteIdentityDifference.LengthChanged,
            RemoteIdentityDifference.LengthEvidenceMissing);
        differences |= CompareOptional(
            persisted.EntityTag,
            observed.EntityTag,
            RemoteIdentityDifference.EntityTagChanged,
            RemoteIdentityDifference.EntityTagEvidenceMissing);
        differences |= CompareOptional(
            persisted.LastModified,
            observed.LastModified,
            RemoteIdentityDifference.LastModifiedChanged,
            RemoteIdentityDifference.LastModifiedEvidenceMissing);

        if (persisted.SupportsByteRanges && !observed.SupportsByteRanges)
        {
            differences |= RemoteIdentityDifference.ByteRangeSupportLost;
        }

        if (!HasStrongIdentityEvidence(persisted, observed))
        {
            differences |= RemoteIdentityDifference.SufficientIdentityEvidenceMissing;
        }

        return differences;
    }

    private static RemoteIdentityDifference CompareOptional<T>(
        T? persisted,
        T? observed,
        RemoteIdentityDifference changed,
        RemoteIdentityDifference missing)
        where T : struct
    {
        if (persisted is null)
        {
            return RemoteIdentityDifference.None;
        }

        if (observed is null)
        {
            return missing;
        }

        return EqualityComparer<T>.Default.Equals(persisted.Value, observed.Value)
            ? RemoteIdentityDifference.None
            : changed;
    }

    private static RemoteIdentityDifference CompareOptional(
        string? persisted,
        string? observed,
        RemoteIdentityDifference changed,
        RemoteIdentityDifference missing)
    {
        if (persisted is null)
        {
            return RemoteIdentityDifference.None;
        }

        if (observed is null)
        {
            return missing;
        }

        return string.Equals(persisted, observed, StringComparison.Ordinal)
            ? RemoteIdentityDifference.None
            : changed;
    }

    private static bool HasStrongIdentityEvidence(
        RemoteIdentity persisted,
        RemoteIdentity observed)
    {
        var hasMatchingStrongEntityTag =
            persisted.EntityTag is { } persistedTag &&
            observed.EntityTag is { } observedTag &&
            !persistedTag.StartsWith("W/", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(persistedTag, observedTag, StringComparison.Ordinal);
        var hasMatchingLengthAndDate =
            persisted.Length is { } persistedLength &&
            observed.Length == persistedLength &&
            persisted.LastModified is { } persistedDate &&
            observed.LastModified == persistedDate;

        return hasMatchingStrongEntityTag || hasMatchingLengthAndDate;
    }

    private static RemoteIdentityReconciliationStatus Classify(RemoteIdentityDifference differences)
    {
        if ((differences & Contradictions) != 0)
        {
            return RemoteIdentityReconciliationStatus.Contradictory;
        }

        if ((differences & MissingEvidence) != 0)
        {
            return RemoteIdentityReconciliationStatus.InsufficientEvidence;
        }

        if ((differences & RemoteIdentityDifference.ByteRangeSupportLost) != 0)
        {
            return RemoteIdentityReconciliationStatus.ResumeCapabilityLost;
        }

        return RemoteIdentityReconciliationStatus.Compatible;
    }

    private static RemoteIdentity NormalizeIdentity(RemoteIdentity identity) =>
        new(
            NormalizeUri(identity.FinalUri),
            identity.Length,
            identity.EntityTag,
            identity.LastModified,
            identity.SupportsByteRanges);

    private static bool UrisMatch(Uri first, Uri second) =>
        Uri.Compare(
            NormalizeUri(first),
            NormalizeUri(second),
            UriComponents.SchemeAndServer | UriComponents.Path,
            UriFormat.UriEscaped,
            StringComparison.Ordinal) == 0;

    private static Uri NormalizeUri(Uri uri) =>
        new UriBuilder(uri)
        {
            Query = string.Empty,
            Fragment = string.Empty,
            UserName = string.Empty,
            Password = string.Empty,
        }.Uri;
}
