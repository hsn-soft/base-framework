using JetBrains.Annotations;

namespace Hhs.ContentService.Application.Contracts.ContentDomain.Dtos;

public sealed class GetOrCreateCustomerContentResponseDto
{
    [CanBeNull]
    public Guid? ReferenceContentId { get; set; }

    [NotNull]
    public string ReferenceContentType { get; set; }

    [NotNull]
    public string ContentStatus { get; set; }

    [CanBeNull]
    public string ContentVideoUrl { get; set; }
}