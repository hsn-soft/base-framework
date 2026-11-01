using HsnSoft.Base;

namespace Hsx.ContentService.Domain.CustomerVpSettingDomain.Exceptions;

/// <summary>There is no such customer vp setting: answered as 404.</summary>
public static class CustomerVpSettingNotFound
{
    public static NotFoundException For(Guid id) => new NotFoundException($"CustomerVpSetting {id} was not found.").WithKey("customer_vp_setting_not_found", id);
}
