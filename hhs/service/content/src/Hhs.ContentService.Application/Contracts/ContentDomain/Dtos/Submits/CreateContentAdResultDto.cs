using JetBrains.Annotations;

namespace Hhs.ContentService.Application.Contracts.ContentDomain.Dtos.Submits;

public sealed class CreateContentAdResultDto
{
    public Guid? CustomerId { get; set; }
    public Guid ReferenceContentId { get; set; }

    [NotNull] public string ReferenceContentType { get; set; }

    public Guid? FeedModuleId { get; set; }

    [NotNull] public string FeedKey { get; set; }
    [CanBeNull] public string FeedMessage { get; set; }
}