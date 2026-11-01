using HsnSoft.Base.Application.Dtos;

namespace Hsx.ContentService.Application.Contracts.CustomerContentVisitDomain.Dtos;

public sealed class CustomerContentVisitDto : EntityDto<Guid>
{
    public string ScopeKey { get; init; } = string.Empty;
    public Guid CustomerContentId { get; init; }
    public DateTimeOffset VisitTime { get; init; }
    public string VisitResponse { get; init; } = string.Empty;
}
