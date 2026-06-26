using Hhs.ContentService.Domain.Enums;

namespace Hhs.ContentService.Application.Contracts.ContentDomain.Dtos;

public sealed class CustomerContentStatusDto
{
    public Guid CustomerContentId { get; set; }
    public CustomerContentOperationStates OperationStatus { get; set; }
    public string StorageVideoUrl { get; set; }
}