using WindowsDownloadManager.Domain.Downloads;

namespace WindowsDownloadManager.Application.Downloads;

public enum DownloadResumeStatus
{
    Blocked,
    ResumedToVerification,
}

public sealed record DownloadResumeResult(
    Guid DownloadId,
    DownloadResumeStatus Status,
    long ConfirmedBytes,
    DownloadState State,
    StartupRecoveryAssessmentResult Assessment);
