using JetBrains.Annotations;

namespace Hhs.FeedRService.Application.Contracts.JobDomain.Dtos.Reporting;

public sealed class ReportingRequestDto(string JobName)
{
    [NotNull]
    public string AdUnit { get; set; }

    [NotNull]
    public string Parent { get; set; }

    public string JobName { get; } = JobName;
}