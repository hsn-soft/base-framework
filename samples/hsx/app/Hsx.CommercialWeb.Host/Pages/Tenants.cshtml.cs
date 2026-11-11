using HsnSoft.Base;
using Hsx.CommercialWeb.Models;
using Hsx.CommercialWeb.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Hsx.CommercialWeb.Pages;

public sealed class TenantsModel(GatewayClient gateway) : PageModel
{
    public Paged<TenantItem> Data { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        try
        {
            Data = await gateway.ListTenantsAsync(cancellationToken);
            return Page();
        }
        catch (BaseHttpException exception) when (exception.HttpStatusCode == 401)
        {
            // the token expired or is not accepted: the sign-in is over
            await HttpContext.SignOutAsync();
            return Redirect("/account/login?expired=true");
        }
    }
}
