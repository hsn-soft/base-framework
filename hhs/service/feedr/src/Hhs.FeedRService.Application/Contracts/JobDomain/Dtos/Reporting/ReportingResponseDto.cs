using JetBrains.Annotations;

namespace Hhs.FeedRService.Application.Contracts.JobDomain.Dtos.Reporting;

public sealed class ReportingResponseDto
{
    [NotNull]
    public string Report { get; set; }
}