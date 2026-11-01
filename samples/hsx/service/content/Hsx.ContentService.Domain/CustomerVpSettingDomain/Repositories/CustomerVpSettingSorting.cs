using HsnSoft.Base.Domain.Sorting;
using Hsx.ContentService.Domain.CustomerVpSettingDomain.Entities;

namespace Hsx.ContentService.Domain.CustomerVpSettingDomain.Repositories;

/// <summary>The fields a list of customer vp settings may be sorted by and the sorting when it sends none.</summary>
public static class CustomerVpSettingSorting
{
    public const string Default = "domainName asc";

    public static readonly SortFields<CustomerVpSetting> Fields = new SortFields<CustomerVpSetting>()
        .Add("domainName", entity => entity.NormalizedDomainName)
        .Add("createdAt", entity => entity.CreatedAt);
}
