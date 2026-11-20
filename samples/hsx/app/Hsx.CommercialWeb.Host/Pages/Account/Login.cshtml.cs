using System.Security.Claims;
using Hsx.CommercialWeb.Models;
using Hsx.CommercialWeb.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Hsx.CommercialWeb.Pages.Account;

public sealed class LoginModel(GatewayClient gateway) : PageModel
{
    /// <summary>The tenant of the user: an email or a user name is unique only inside a tenant.</summary>
    [BindProperty]
    public string Tenant { get; set; } = string.Empty;

    /// <summary>The email or the user name.</summary>
    [BindProperty]
    public string Account { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    public string? Error { get; private set; }

    /// <summary>The sign-in ended (the token expired or the gateway did not accept it): the page says so.</summary>
    public bool Expired { get; private set; }

    public void OnGet(bool expired = false) => Expired = expired;

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        TokenPayload? token = await gateway.LoginAsync(Tenant, Account, Password, cancellationToken);
        if (token is null)
        {
            Password = string.Empty;
            Error = "The tenant, the account or the password is wrong.";
            return Page();
        }

        // the token of the user is kept in their cookie and sent with every call to the gateway; the sign-in lasts as long as the token does
        ClaimsIdentity identity = new([new Claim(ClaimTypes.Name, Account), new Claim("access_token", token.AccessToken)], CookieAuthenticationDefaults.AuthenticationScheme);
        AuthenticationProperties properties = new() { IsPersistent = false, ExpiresUtc = DateTimeOffset.UtcNow.AddSeconds(token.ExpiresIn) };
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), properties);
        return Redirect("/");
    }
}
