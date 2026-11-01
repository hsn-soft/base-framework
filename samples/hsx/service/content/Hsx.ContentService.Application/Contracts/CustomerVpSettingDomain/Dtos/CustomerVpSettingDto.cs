using HsnSoft.Base.Application.Dtos;

namespace Hsx.ContentService.Application.Contracts.CustomerVpSettingDomain.Dtos;

public sealed class CustomerVpSettingDto : AuditedEntityDto<Guid>
{
    public string ScopeKey { get; init; } = string.Empty;
    public string DomainName { get; init; } = string.Empty;
    public bool IsBlocked { get; init; }
    public int DailyDirectVideoGenerationLimit { get; init; }
    public int DailyTrendVideoGenerationLimit { get; init; }
    public int DailyAnalysisVideoGenerationLimit { get; init; }
    public bool IsAnalysisSourcesEnabled { get; init; }
}
