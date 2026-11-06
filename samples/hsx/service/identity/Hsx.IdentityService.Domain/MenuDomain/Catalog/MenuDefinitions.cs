using Hsx.IdentityService.Domain.Enums;

namespace Hsx.IdentityService.Domain.MenuDomain.Catalog;

/// <summary>A permission of a menu item and what it is to the item.</summary>
public sealed record MenuPermissionDefinition(string Permission, MenuPermissionRelationType Relation);

/// <summary>An item of the menu as the code defines it. The items are kept in step with the table at every start (the permissions seeder).</summary>
public sealed record MenuDefinition(string Code, MenuNodeType NodeType, string Title, string? Route, string? Icon, int SortOrder, string? Parent, params MenuPermissionDefinition[] Permissions);
