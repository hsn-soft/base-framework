using JetBrains.Annotations;

namespace Hhs.ContentService.Application.Contracts.ContentDomain.Dtos;

public sealed class CreateContentAdResultDto
{
    public Guid CustomerId { get; set; }

    public Guid ContentId { get; set; }

    [NotNull] public string ContentType { get; set; }

    [NotNull] public string FeedKey { get; set; }
    [CanBeNull] public string FeedMessage { get; set; }
}