using Hsx.IdentityService.Domain.Enums;
using Hsx.IdentityService.Domain.MenuDomain.Catalog;
using Hsx.IdentityService.Domain.PermissionDomain.Catalog;
using Hsx.Shared.Contracts.Permissions;
using Microsoft.Extensions.DependencyInjection;

namespace Hsx.IdentityService.Application.Permissions;

/// <summary>The catalog of the system as the code of the services says it: the codes of <c>Hsx.Shared.Contracts</c> (shared ADR-0015), the menu of <see cref="MenuCatalog"/> and the starting grants of <see cref="DefaultPermissionGrants"/>.</summary>
public sealed class IdentityPermissionCatalog : IPermissionCatalog
{
    public IReadOnlyList<string> Codes => HsxPermissionCatalog.Codes;

    public IReadOnlyList<MenuDefinition> BackOfficeMenu => MenuCatalog.BackOffice;

    public IReadOnlyList<string> GrantsFor(AppRoleType roleType, string roleName) => DefaultPermissionGrants.ForRole(roleType, roleName);

    public IReadOnlyDictionary<string, string> ConstraintsFor(AppRoleType roleType, string roleName) => DefaultPermissionGrants.ConstraintsFor(roleType, roleName);
}

public static class PermissionCatalogServiceCollectionExtensions
{
    /// <summary>Registers the catalog of the system (what the permissions seeder and the provisioning of roles read).</summary>
    public static IServiceCollection AddPermissionCatalog(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services.AddSingleton<IPermissionCatalog, IdentityPermissionCatalog>();
    }
}
