using HsnSoft.Base;

namespace Hsx.ContentService.Domain.CustomerContentVpSettingDomain.Exceptions;

/// <summary>There is no such customer vp setting: answered as 404.</summary>
public static class CustomerContentVpSettingNotFound
{
    public static NotFoundException For(Guid id) => new NotFoundException($"CustomerContentVpSetting {id} was not found.").WithKey("customer_content_vp_setting_not_found", id);
}
