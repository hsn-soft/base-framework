using Hhs.ContentService.Domain.ContentDomain.Models;
using HsnSoft.Base.Domain.Entities.Events;

namespace Hhs.ContentService.Application.Contracts.JobDomain.Dtos;

public sealed class ForceAnalysisVideoGenerationResultDto{
    public string errorMessage  { get; set; } = string.Empty;
    public bool operationSuccess { get; set; } = false;
}