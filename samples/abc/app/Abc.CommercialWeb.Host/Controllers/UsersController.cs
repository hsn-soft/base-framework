using Abc.CommercialWeb.Models;
using Abc.CommercialWeb.Services;
using HsnSoft.Base;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Abc.CommercialWeb.Controllers;

[Authorize]
public sealed class UsersController(GatewayClient gateway) : Controller
{
    [HttpGet("/users")]
    public async Task<IActionResult> IndexAsync(string? search, CancellationToken cancellationToken)
    {
        try
        {
            return View(new UsersModel { Search = search, Users = await gateway.ListUsersAsync(search, cancellationToken) });
        }
        catch (BaseHttpException exception) when (exception.HttpStatusCode == 401)
        {
            // the token is no longer good: the sign-in ends here, and the user is told why
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Redirect("/account/login?expired=true");
        }
    }
}
