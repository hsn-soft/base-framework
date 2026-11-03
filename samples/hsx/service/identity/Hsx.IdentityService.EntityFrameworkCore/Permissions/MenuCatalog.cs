using Hsx.IdentityService.Domain.MenuDomain.Entities;
using Hsx.Shared.Helper;

namespace Hsx.IdentityService.EntityFrameworkCore.Permissions;

/// <summary>A permission of a menu item and what it is to the item.</summary>
public sealed record MenuPermissionDefinition(string Permission, MenuPermissionRelationType Relation);

/// <summary>An item of the menu as the code defines it. The items are kept in step with the table at every start (<c>PermissionDatabase.EnsurePermissionsAsync</c>).</summary>
public sealed record MenuDefinition(string Code, MenuNodeType NodeType, string Title, string? Route, string? Icon, int SortOrder, string? Parent, params MenuPermissionDefinition[] Permissions);

/// <summary>
/// The menu of the back office (its titles are what the people of the back office read: Turkish): what a user may go to, and for each page the permissions it needs. A leaf has its visibility permission (the page opens and is in the menu with it), what it reads
/// with (required) and its buttons (actions); the front end asks for the menu of the user and gets only the items it may see, with these as granted or not. The menu is in the code: an item is added here, with the pages of the service that brings it.
/// </summary>
public static class MenuCatalog
{
    public const string Administration = "administration";

    public const string VideoPlatform = "video-platform";

    public static IReadOnlyList<MenuDefinition> BackOffice { get; } =
    [
        new("caption.administration", MenuNodeType.Caption, "Yönetim", null, null, 10, null),
        new(Administration, MenuNodeType.Node, "Kullanıcı ve kiracılar", null, "settings", 20, null),
        new(
            $"{Administration}.tenants", MenuNodeType.Leaf, "Kiracılar", "/tenants", "tenant", 10, Administration,
            Visibility(HsxPagePermissions.Identity.Tenants.View),
            Required(HsxEndpointPermissions.Identity.Tenants.Read),
            Action(HsxEndpointPermissions.Identity.Tenants.Create),
            Action(HsxEndpointPermissions.Identity.TenantManagement.CreateReseller),
            Action(HsxEndpointPermissions.Identity.Tenants.Update),
            Action(HsxEndpointPermissions.Identity.Tenants.Move),
            Action(HsxEndpointPermissions.Identity.Tenants.Delete)),
        new(
            $"{Administration}.users", MenuNodeType.Leaf, "Kullanıcılar", "/users", "user", 20, Administration,
            Visibility(HsxPagePermissions.Identity.Users.View),
            Required(HsxEndpointPermissions.Identity.AppUsers.Read),
            Action(HsxEndpointPermissions.Identity.AppUsers.Create),
            Action(HsxEndpointPermissions.Identity.AppUsers.Update),
            Action(HsxEndpointPermissions.Identity.AppUsers.SetPassword),
            Action(HsxEndpointPermissions.Identity.AppUsers.SetRoles),
            Action(HsxEndpointPermissions.Identity.AppUsers.Delete)),
        new(
            $"{Administration}.roles", MenuNodeType.Leaf, "Roller ve izinler", "/roles", "role", 30, Administration,
            Visibility(HsxPagePermissions.Identity.Roles.View),
            Required(HsxEndpointPermissions.Identity.AppRoles.Read),
            Required(HsxEndpointPermissions.Identity.Permissions.Read),
            Required(HsxEndpointPermissions.Identity.RolePermissions.Read),
            Action(HsxEndpointPermissions.Identity.AppRoles.Create),
            Action(HsxEndpointPermissions.Identity.AppRoles.Update),
            Action(HsxEndpointPermissions.Identity.RolePermissions.Update),
            Action(HsxEndpointPermissions.Identity.AppRoles.Delete)),
        new(
            $"{Administration}.companies", MenuNodeType.Leaf, "Şirketler", "/companies", "company", 40, Administration,
            Visibility(HsxPagePermissions.Identity.Companies.View),
            Required(HsxEndpointPermissions.Identity.Companies.Read),
            Action(HsxEndpointPermissions.Identity.Companies.Create),
            Action(HsxEndpointPermissions.Identity.Companies.Update),
            Action(HsxEndpointPermissions.Identity.Companies.Delete)),
        new(
            $"{Administration}.customers", MenuNodeType.Leaf, "Müşteriler", "/customers", "customer", 50, Administration,
            Visibility(HsxPagePermissions.Identity.Customers.View),
            Required(HsxEndpointPermissions.Identity.Customers.Read),
            Action(HsxEndpointPermissions.Identity.Customers.Create),
            Action(HsxEndpointPermissions.Identity.Customers.Update),
            Action(HsxEndpointPermissions.Identity.Customers.Delete)),
        new(
            $"{Administration}.subscriptions", MenuNodeType.Leaf, "Abonelikler", "/subscriptions", "subscription", 60, Administration,
            Visibility(HsxPagePermissions.Identity.Subscriptions.View),
            Required(HsxEndpointPermissions.Identity.Subscriptions.Read),
            Action(HsxEndpointPermissions.Identity.Subscriptions.Create),
            Action(HsxEndpointPermissions.Identity.Subscriptions.Update),
            Action(HsxEndpointPermissions.Identity.Subscriptions.Transfer),
            Action(HsxEndpointPermissions.Identity.Subscriptions.Delete)),
        new("caption.video-platform", MenuNodeType.Caption, "Ürünler", null, null, 30, null),
        new(VideoPlatform, MenuNodeType.Node, "Video platformu", null, "video", 40, null),
        new(
            $"{VideoPlatform}.contents", MenuNodeType.Leaf, "İçerikler", "/contents", "content", 10, VideoPlatform,
            Visibility(HsxPagePermissions.Content.CustomerContents.View),
            Required(HsxEndpointPermissions.Content.CustomerContents.Read),
            Action(HsxEndpointPermissions.Content.CustomerContents.Create),
            Action(HsxEndpointPermissions.Content.CustomerContents.Update),
            Action(HsxEndpointPermissions.Content.CustomerContents.Delete)),
        new(
            $"{VideoPlatform}.analysis-contents", MenuNodeType.Leaf, "Analizler", "/analysis-contents", "analysis", 20, VideoPlatform,
            Visibility(HsxPagePermissions.Content.AnalysisContents.View),
            Required(HsxEndpointPermissions.Content.AnalysisContents.Read),
            Required(HsxEndpointPermissions.Content.AnalysisContentItems.Read),
            Action(HsxEndpointPermissions.Content.AnalysisContents.Create),
            Action(HsxEndpointPermissions.Content.AnalysisContents.Update),
            Action(HsxEndpointPermissions.Content.AnalysisContents.Delete),
            Action(HsxEndpointPermissions.Content.AnalysisContentItems.Create),
            Action(HsxEndpointPermissions.Content.AnalysisContentItems.Update),
            Action(HsxEndpointPermissions.Content.AnalysisContentItems.Delete)),
        new(
            $"{VideoPlatform}.visits", MenuNodeType.Leaf, "Ziyaretler", "/visits", "visit", 30, VideoPlatform,
            Visibility(HsxPagePermissions.Content.CustomerContentVisits.View),
            Required(HsxEndpointPermissions.Content.CustomerContentVisits.Read),
            Action(HsxEndpointPermissions.Content.CustomerContentVisits.Create),
            Action(HsxEndpointPermissions.Content.CustomerContentVisits.Update),
            Action(HsxEndpointPermissions.Content.CustomerContentVisits.Delete)),
        new(
            $"{VideoPlatform}.settings", MenuNodeType.Leaf, "Müşteri ayarları", "/customer-settings", "settings", 40, VideoPlatform,
            Visibility(HsxPagePermissions.Content.CustomerContentVpSettings.View),
            Required(HsxEndpointPermissions.Content.CustomerContentVpSettings.Read),
            Action(HsxEndpointPermissions.Content.CustomerContentVpSettings.Create),
            Action(HsxEndpointPermissions.Content.CustomerContentVpSettings.Update),
            Action(HsxEndpointPermissions.Content.CustomerContentVpSettings.Delete)),
        new(
            $"{VideoPlatform}.normalize-requests", MenuNodeType.Leaf, "Metin hazırlama", "/normalize-requests", "text", 50, VideoPlatform,
            Visibility(HsxPagePermissions.TextNormalizer.NormalizeRequests.View),
            Required(HsxEndpointPermissions.TextNormalizer.CustomerContentNormalizedRequests.Read),
            Action(HsxEndpointPermissions.TextNormalizer.CustomerContentNormalizedRequests.Create),
            Action(HsxEndpointPermissions.TextNormalizer.CustomerContentNormalizedRequests.Update),
            Action(HsxEndpointPermissions.TextNormalizer.CustomerContentNormalizedRequests.Delete)),
        new(
            $"{VideoPlatform}.video-requests", MenuNodeType.Leaf, "Video istekleri", "/video-requests", "video", 60, VideoPlatform,
            Visibility(HsxPagePermissions.VideoGenerator.VideoRequests.View),
            Required(HsxEndpointPermissions.VideoGenerator.VideoRequests.Read),
            Action(HsxEndpointPermissions.VideoGenerator.VideoRequests.Create),
            Action(HsxEndpointPermissions.VideoGenerator.VideoRequests.Update),
            Action(HsxEndpointPermissions.VideoGenerator.VideoRequests.Delete)),
    ];

    private static MenuPermissionDefinition Visibility(string permission) => new(permission, MenuPermissionRelationType.Visibility);

    private static MenuPermissionDefinition Required(string permission) => new(permission, MenuPermissionRelationType.Required);

    private static MenuPermissionDefinition Action(string permission) => new(permission, MenuPermissionRelationType.Action);
}
