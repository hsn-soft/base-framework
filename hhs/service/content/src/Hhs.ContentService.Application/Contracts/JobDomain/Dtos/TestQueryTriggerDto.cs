namespace Hhs.ContentService.Application.Contracts.JobDomain.Dtos;

public sealed record TestQueryTriggerDto(string JobName)
{
    public string JobName { get; } = JobName;
}