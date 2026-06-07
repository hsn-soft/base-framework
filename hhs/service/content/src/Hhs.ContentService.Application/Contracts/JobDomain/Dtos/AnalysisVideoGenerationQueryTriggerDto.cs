namespace Hhs.ContentService.Application.Contracts.JobDomain.Dtos;

public sealed record AnalysisVideoGenerationQueryTriggerDto(string JobName)
{
    public string JobName { get; } = JobName;
}