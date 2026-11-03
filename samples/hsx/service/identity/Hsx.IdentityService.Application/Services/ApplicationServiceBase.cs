using HsnSoft.Base;
using HsnSoft.Base.Application;
using HsnSoft.Base.MultiTenancy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace Hsx.IdentityService.Application.Services;

/// <summary>
/// What every application service of the identity service is: <c>Logger</c> and <c>CurrentUser</c> from the framework's <see cref="ApplicationService"/>, <c>L</c>, which translates a text of a response body into the language of the
/// request, and <c>CurrentTenant</c>, the tenant and the scopes the token of the caller lists.
/// </summary>
public abstract class ApplicationServiceBase(IServiceProvider services) : ApplicationService(services)
{
    protected IStringLocalizer L => field ??= ServiceProvider.GetRequiredService<IStringLocalizer>();

    protected ICurrentTenant CurrentTenant => field ??= ServiceProvider.GetRequiredService<ICurrentTenant>();

    /// <summary>
    /// What only the system tenant does (the tenants, the catalogue of the companies and the customers, the sales). It is checked here and not only by a permission: the administrator of a tenant grants the permissions of the roles of its own tenant, so a permission alone would not keep a reseller out.
    /// </summary>
    /// <exception cref="BaseHttpException">The caller is not the system tenant (403).</exception>
    protected void EnsureSystemTenant()
    {
        if (!CurrentTenant.IsSystemTenant)
        {
            throw new BaseHttpException(403, "Only the system tenant does this.").WithKey("system_tenant_required");
        }
    }

    /// <summary>
    /// A new row of a subscription is made in a scope the caller is allowed (its token lists the key), or by the system tenant, which is under no isolation: nobody makes data in the subscription of another.
    /// </summary>
    /// <exception cref="BaseHttpException">The caller may not use the scope (403).</exception>
    protected void EnsureScope(string scopeKey)
    {
        if (!CurrentTenant.IsSystemTenant && !CurrentTenant.AllowedScopeKeys.Contains(scopeKey, StringComparer.Ordinal))
        {
            throw new BaseHttpException(403, $"The scope '{scopeKey}' is not one of the caller's.").WithKey("scope_not_allowed", scopeKey);
        }
    }
}
