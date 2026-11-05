using Hsx.IdentityService.Domain.Enums;
using Hsx.IdentityService.Domain.MenuDomain.Catalog;
using Hsx.Shared.Contracts.Permissions;

namespace Hsx.IdentityService.Application.Permissions;

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
            Visibility(IdentityPermissions.Pages.Tenants.View),
            Required(IdentityPermissions.Endpoints.Tenants.Read),
            Action(IdentityPermissions.Endpoints.Tenants.Create),
            Action(IdentityPermissions.Endpoints.TenantManagement.CreateReseller),
            Action(IdentityPermissions.Endpoints.Tenants.Update),
            Action(IdentityPermissions.Endpoints.Tenants.Move),
            Action(IdentityPermissions.Endpoints.Tenants.Delete)),
        new(
            $"{Administration}.users", MenuNodeType.Leaf, "Kullanıcılar", "/users", "user", 20, Administration,
            Visibility(IdentityPermissions.Pages.Users.View),
            Required(IdentityPermissions.Endpoints.AppUsers.Read),
            Action(IdentityPermissions.Endpoints.AppUsers.Create),
            Action(IdentityPermissions.Endpoints.AppUsers.Update),
            Action(IdentityPermissions.Endpoints.AppUsers.SetPassword),
            Action(IdentityPermissions.Endpoints.AppUsers.SetRoles),
            Action(IdentityPermissions.Endpoints.AppUsers.Delete)),
        new(
            $"{Administration}.roles", MenuNodeType.Leaf, "Roller ve izinler", "/roles", "role", 30, Administration,
            Visibility(IdentityPermissions.Pages.Roles.View),
            Required(IdentityPermissions.Endpoints.AppRoles.Read),
            Required(IdentityPermissions.Endpoints.Permissions.Read),
            Required(IdentityPermissions.Endpoints.RolePermissions.Read),
            Action(IdentityPermissions.Endpoints.AppRoles.Create),
            Action(IdentityPermissions.Endpoints.AppRoles.Update),
            Action(IdentityPermissions.Endpoints.RolePermissions.Update),
            Action(IdentityPermissions.Endpoints.AppRoles.Delete)),
        new(
            $"{Administration}.companies", MenuNodeType.Leaf, "Şirketler", "/companies", "company", 40, Administration,
            Visibility(IdentityPermissions.Pages.Companies.View),
            Required(IdentityPermissions.Endpoints.Companies.Read),
            Action(IdentityPermissions.Endpoints.Companies.Create),
            Action(IdentityPermissions.Endpoints.Companies.Update),
            Action(IdentityPermissions.Endpoints.Companies.Delete)),
        new(
            $"{Administration}.customers", MenuNodeType.Leaf, "Müşteriler", "/customers", "customer", 50, Administration,
            Visibility(IdentityPermissions.Pages.Customers.View),
            Required(IdentityPermissions.Endpoints.Customers.Read),
            Action(IdentityPermissions.Endpoints.Customers.Create),
            Action(IdentityPermissions.Endpoints.Customers.Update),
            Action(IdentityPermissions.Endpoints.Customers.Delete)),
        new(
            $"{Administration}.subscriptions", MenuNodeType.Leaf, "Abonelikler", "/subscriptions", "subscription", 60, Administration,
            Visibility(IdentityPermissions.Pages.Subscriptions.View),
            Required(IdentityPermissions.Endpoints.Subscriptions.Read),
            Action(IdentityPermissions.Endpoints.Subscriptions.Create),
            Action(IdentityPermissions.Endpoints.Subscriptions.Update),
            Action(IdentityPermissions.Endpoints.Subscriptions.Transfer),
            Action(IdentityPermissions.Endpoints.Subscriptions.Delete)),
        new("caption.video-platform", MenuNodeType.Caption, "Ürünler", null, null, 30, null),
        new(VideoPlatform, MenuNodeType.Node, "Video platformu", null, "video", 40, null),
        new(
            $"{VideoPlatform}.contents", MenuNodeType.Leaf, "İçerikler", "/contents", "content", 10, VideoPlatform,
            Visibility(ContentPermissions.Pages.CustomerContents.View),
            Required(ContentPermissions.Endpoints.CustomerContents.Read),
            Action(ContentPermissions.Endpoints.CustomerContents.Create),
            Action(ContentPermissions.Endpoints.CustomerContents.Update),
            Action(ContentPermissions.Endpoints.CustomerContents.Delete)),
        new(
            $"{VideoPlatform}.analysis-contents", MenuNodeType.Leaf, "Analizler", "/analysis-contents", "analysis", 20, VideoPlatform,
            Visibility(ContentPermissions.Pages.AnalysisContents.View),
            Required(ContentPermissions.Endpoints.AnalysisContents.Read),
            Required(ContentPermissions.Endpoints.AnalysisContentItems.Read),
            Action(ContentPermissions.Endpoints.AnalysisContents.Create),
            Action(ContentPermissions.Endpoints.AnalysisContents.Update),
            Action(ContentPermissions.Endpoints.AnalysisContents.Delete),
            Action(ContentPermissions.Endpoints.AnalysisContentItems.Create),
            Action(ContentPermissions.Endpoints.AnalysisContentItems.Update),
            Action(ContentPermissions.Endpoints.AnalysisContentItems.Delete)),
        new(
            $"{VideoPlatform}.visits", MenuNodeType.Leaf, "Ziyaretler", "/visits", "visit", 30, VideoPlatform,
            Visibility(ContentPermissions.Pages.CustomerContentVisits.View),
            Required(ContentPermissions.Endpoints.CustomerContentVisits.Read),
            Action(ContentPermissions.Endpoints.CustomerContentVisits.Create),
            Action(ContentPermissions.Endpoints.CustomerContentVisits.Update),
            Action(ContentPermissions.Endpoints.CustomerContentVisits.Delete)),
        new(
            $"{VideoPlatform}.settings", MenuNodeType.Leaf, "Müşteri ayarları", "/customer-settings", "settings", 40, VideoPlatform,
            Visibility(ContentPermissions.Pages.CustomerContentVpSettings.View),
            Required(ContentPermissions.Endpoints.CustomerContentVpSettings.Read),
            Action(ContentPermissions.Endpoints.CustomerContentVpSettings.Create),
            Action(ContentPermissions.Endpoints.CustomerContentVpSettings.Update),
            Action(ContentPermissions.Endpoints.CustomerContentVpSettings.Delete)),
        new(
            $"{VideoPlatform}.normalize-requests", MenuNodeType.Leaf, "Metin hazırlama", "/normalize-requests", "text", 50, VideoPlatform,
            Visibility(TextNormalizerPermissions.Pages.NormalizeRequests.View),
            Required(TextNormalizerPermissions.Endpoints.CustomerContentNormalizedRequests.Read),
            Action(TextNormalizerPermissions.Endpoints.CustomerContentNormalizedRequests.Create),
            Action(TextNormalizerPermissions.Endpoints.CustomerContentNormalizedRequests.Update),
            Action(TextNormalizerPermissions.Endpoints.CustomerContentNormalizedRequests.Delete)),
        new(
            $"{VideoPlatform}.video-requests", MenuNodeType.Leaf, "Video istekleri", "/video-requests", "video", 60, VideoPlatform,
            Visibility(VideoGeneratorPermissions.Pages.VideoRequests.View),
            Required(VideoGeneratorPermissions.Endpoints.VideoRequests.Read),
            Action(VideoGeneratorPermissions.Endpoints.VideoRequests.Create),
            Action(VideoGeneratorPermissions.Endpoints.VideoRequests.Update),
            Action(VideoGeneratorPermissions.Endpoints.VideoRequests.Delete)),
    ];

    private static MenuPermissionDefinition Visibility(string permission) => new(permission, MenuPermissionRelationType.Visibility);

    private static MenuPermissionDefinition Required(string permission) => new(permission, MenuPermissionRelationType.Required);

    private static MenuPermissionDefinition Action(string permission) => new(permission, MenuPermissionRelationType.Action);
}
