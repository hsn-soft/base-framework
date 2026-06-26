using JetBrains.Annotations;

namespace Hhs.ContentService.Application.Contracts.ContentDomain.Submits;

public sealed class CreateContentAdResultDto
{
    public Guid CustomerId { get; set; }
    public Guid ContentId { get; set; }

    [NotNull] public string ContentType { get; set; }

    public Guid FeedModuleId { get; set; }

    [NotNull] public string FeedKey { get; set; }
    [CanBeNull] public string FeedMessage { get; set; }
}