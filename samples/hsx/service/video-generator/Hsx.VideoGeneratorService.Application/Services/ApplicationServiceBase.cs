using HsnSoft.Base;
using HsnSoft.Base.Application;
using HsnSoft.Base.MultiTenancy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace Hsx.VideoGeneratorService.Application.Services;

/// <summary>
/// What every application service of the video generator service is: <c>Logger</c> and <c>CurrentUser</c> from the framework's <see cref="ApplicationService"/>, <c>L</c>, which translates a text of a response body into the language of the
/// request, and <c>CurrentTenant</c>, the tenant and the scopes the token of the caller lists.
/// </summary>
public abstract class ApplicationServiceBase(IServiceProvider services) : ApplicationService(services)
{
    protected IStringLocalizer L => field ??= ServiceProvider.GetRequiredService<IStringLocalizer>();

    protected ICurrentTenant CurrentTenant => field ??= ServiceProvider.GetRequiredService<ICurrentTenant>();

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
