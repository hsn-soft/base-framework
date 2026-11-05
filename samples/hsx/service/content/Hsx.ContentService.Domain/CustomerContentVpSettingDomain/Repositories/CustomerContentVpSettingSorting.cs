using HsnSoft.Base.Domain.Sorting;
using Hsx.ContentService.Domain.CustomerContentVpSettingDomain.Entities;

namespace Hsx.ContentService.Domain.CustomerContentVpSettingDomain.Repositories;

/// <summary>The fields a list of customer vp settings may be sorted by and the sorting when it sends none.</summary>
public static class CustomerContentVpSettingSorting
{
    public const string Default = "domainName asc";

    public static readonly SortFields<CustomerContentVpSetting> Fields = new SortFields<CustomerContentVpSetting>()
        .Add("domainName", entity => entity.NormalizedDomainName)
        .Add("createdAt", entity => entity.CreatedAt);
}
