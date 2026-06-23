namespace Hhs.ContentService.Domain.ContentDomain.Models;

public sealed class CustomerContentVisitCountModel
{
    public Guid CustomerContentId { get; set; }
    public long VisitCount { get; set; }
}