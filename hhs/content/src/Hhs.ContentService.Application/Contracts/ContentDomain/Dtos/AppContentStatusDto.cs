using Hhs.ContentService.Domain.Enums;

namespace Hhs.ContentService.Application.Contracts.ContentDomain.Dtos;

public sealed class AppContentStatusDto
{
    public Guid AppContentId { get; set; }
    public AppContentOperationStates OperationStatus { get; set; }
    public string StorageVideoUrl { get; set; }
}