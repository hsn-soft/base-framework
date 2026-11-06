using Hsx.CommercialWeb.Host.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Hsx.CommercialWeb.Host.Pages;

public sealed class IndexModel : PageModel
{
    public string? TenantId { get; private set; }

    public bool IsSystem { get; private set; }

    public IReadOnlyList<string> Roles { get; private set; } = [];

    public IReadOnlyList<string> AllowedTenants { get; private set; } = [];

    public IReadOnlyList<string> ScopeKeys { get; private set; } = [];

    public void OnGet()
    {
        string? token = User.FindFirst("access_token")?.Value;
        TenantId = TokenClaims.Read(token, "tenant_id") is [string tenant, ..] ? tenant : null;
        IsSystem = TokenClaims.Read(token, "is_system_tenant").Count > 0;
        Roles = TokenClaims.Read(token, "role");
        AllowedTenants = TokenClaims.Read(token, "allowed_tenant_id");
        ScopeKeys = TokenClaims.Read(token, "allowed_scope_key");
    }
}
