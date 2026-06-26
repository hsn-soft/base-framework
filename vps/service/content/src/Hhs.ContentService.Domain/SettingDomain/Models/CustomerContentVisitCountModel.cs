namespace Hhs.ContentService.Domain.SettingDomain.Models;

public sealed class CustomerContentVisitCountModel
{
    public Guid CustomerContentId { get; set; }
    public long VisitCount { get; set; }
}