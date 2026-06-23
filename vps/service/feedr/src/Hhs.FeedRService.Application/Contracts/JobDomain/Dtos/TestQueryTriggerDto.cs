namespace Hhs.FeedRService.Application.Contracts.JobDomain.Dtos;

public sealed record TestQueryTriggerDto(string JobName, Guid AppClientId)
{
    public string JobName { get; } = JobName;

    public Guid AppClientId { get; } = AppClientId;
}