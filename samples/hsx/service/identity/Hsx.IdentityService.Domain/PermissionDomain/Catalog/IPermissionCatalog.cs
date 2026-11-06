using Hsx.IdentityService.Domain.Enums;
using Hsx.IdentityService.Domain.MenuDomain.Catalog;

namespace Hsx.IdentityService.Domain.PermissionDomain.Catalog;

/// <summary>
/// What the code of the system says about permissions: the codes of every service, the menu of the back office and what a role starts with. The persistence keeps its tables in step with it at every start and makes the grants of a new role from it; the Application
/// implements it from the shared contracts, so the persistence project (a package that other services take) needs none of them.
/// </summary>
public interface IPermissionCatalog
{
    /// <summary>The codes of every permission of the system.</summary>
    IReadOnlyList<string> Codes { get; }

    /// <summary>The menu of the back office, the parents before their children.</summary>
    IReadOnlyList<MenuDefinition> BackOfficeMenu { get; }

    /// <summary>The codes (of the permissions that carry no value) a role of this name and type starts with.</summary>
    IReadOnlyList<string> GrantsFor(AppRoleType roleType, string roleName);

    /// <summary>The constraints (code and value) a role of this name and type starts with.</summary>
    IReadOnlyDictionary<string, string> ConstraintsFor(AppRoleType roleType, string roleName);
}
