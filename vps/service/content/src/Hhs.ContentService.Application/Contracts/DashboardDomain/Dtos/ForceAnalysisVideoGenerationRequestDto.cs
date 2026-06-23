using Hhs.ContentService.Domain.ContentDomain.Models;
using HsnSoft.Base.Domain.Entities.Events;

namespace Hhs.ContentService.Application.Contracts.JobDomain.Dtos;

public sealed class ForceAnalysisVideoGenerationRequestDto
{
    public Guid AppClientId { get; set; } = Guid.Empty;
    public List<Guid> AppContentIds { get; set; } = new List<Guid>();
}