namespace Hhs.ContentService.Application.Contracts.JobDomain.Dtos;

public sealed record DashboardResponseStatisticQueryTriggerDto(string JobName)
{
    public string JobName { get; } = JobName;
}