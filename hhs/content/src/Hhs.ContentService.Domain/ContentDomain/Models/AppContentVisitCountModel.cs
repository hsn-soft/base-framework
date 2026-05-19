namespace Hhs.ContentService.Domain.ContentDomain.Models;

public sealed class AppContentVisitCountModel
{
    public Guid AppContentId { get; set; }
    public long VisitCount { get; set; }
}