namespace Hhs.ContentService.Application.Contracts.DashboardDomain.Dtos;

public sealed class ForceAnalysisVideoGenerationResultDto{
    public string errorMessage  { get; set; } = string.Empty;
    public bool operationSuccess { get; set; } = false;
}