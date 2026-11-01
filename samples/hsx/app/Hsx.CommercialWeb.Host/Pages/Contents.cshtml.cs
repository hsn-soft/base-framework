using Hsx.CommercialWeb.Host.Models;
using Hsx.CommercialWeb.Host.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Hsx.CommercialWeb.Host.Pages;

public sealed class ContentsModel(GatewayClient gateway) : PageModel
{
    public Paged<ContentItem> Data { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        try
        {
            Data = await gateway.ListContentsAsync(null, cancellationToken);
            return Page();
        }
        catch (GatewayUnauthorizedException)
        {
            // the token expired or is not accepted: the sign-in is over
            await HttpContext.SignOutAsync();
            return Redirect("/account/login?expired=true");
        }
    }
}
