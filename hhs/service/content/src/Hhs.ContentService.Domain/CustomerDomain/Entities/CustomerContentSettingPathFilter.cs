using Hhs.ContentService.Domain.CustomerDomain.Consts;
using Hhs.ContentService.Domain.Enums;
using Hhs.Shared.Localization;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.Text;
using JetBrains.Annotations;

namespace Hhs.ContentService.Domain.CustomerDomain.Entities;

public sealed class CustomerContentSettingPathFilter : Entity<Guid>, IMultiTenant
{
    public Guid TenantId { get; private set; }

    public Guid CustomerContentSettingId { get; set; }

    [NotNull] public string PathFilterName { get; private set; }

    public CustomerSettingFilterTypes CustomerSettingFilterType { get; internal set; }


    private CustomerContentSettingPathFilter()
    {
        // Not-Null string fields
        PathFilterName = string.Empty;
    }

    internal CustomerContentSettingPathFilter(
        Guid tenantId,
        Guid customerContentSettingId,
        [NotNull] string pathFilterName,
        CustomerSettingFilterTypes customerSettingFilterType
    ) : this(Guid.CreateVersion7(), tenantId, customerContentSettingId, pathFilterName, customerSettingFilterType)
    {
    }

    internal CustomerContentSettingPathFilter(Guid id,
        Guid tenantId,
        Guid customerContentSettingId,
        [NotNull] string pathFilterName,
        CustomerSettingFilterTypes customerSettingFilterType
    ) : this()
    {
        Id = id;
        TenantId = tenantId;
        CustomerContentSettingId = customerContentSettingId;

        SetPathFilterName(pathFilterName);

        CustomerSettingFilterType = customerSettingFilterType;
    }

    internal void SetPathFilterName(string pathFilterName)
    {
        string checkFilterName = LocalizedModelValidator.NotNullOrWhiteSpace(pathFilterName, $"{nameof(CustomerContentSettingPathFilter)}:{nameof(PathFilterName)}", CustomerContentSettingPathFilterConsts.PathFilterNameMaxLength);
        PathFilterName = StringHelper.Minimize(checkFilterName);
    }
}