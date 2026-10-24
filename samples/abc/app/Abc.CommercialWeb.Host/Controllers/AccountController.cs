using System.Security.Claims;
using Abc.CommercialWeb.Host.Models;
using Abc.CommercialWeb.Host.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace Abc.CommercialWeb.Host.Controllers;

public sealed class AccountController(GatewayClient gateway) : Controller
{
    [HttpGet("/account/login")]
    public IActionResult Login(bool expired = false) => View(new LoginModel { Expired = expired });

    [HttpPost("/account/login")]
    public async Task<IActionResult> LoginAsync(LoginModel model, CancellationToken cancellationToken)
    {
        TokenPayload? token = await gateway.LoginAsync(model.UserName, model.Password, cancellationToken);
        if (token is null)
        {
            model.Password = string.Empty;
            model.Error = "The user name or the password is wrong.";
            return View("Login", model);
        }

        // the token of the user is kept in his cookie and sent with every call to the gateway; the sign-in lasts as long as the token does
        ClaimsIdentity identity = new([new Claim(ClaimTypes.Name, model.UserName), new Claim("access_token", token.AccessToken)], CookieAuthenticationDefaults.AuthenticationScheme);
        AuthenticationProperties properties = new() { IsPersistent = false, ExpiresUtc = DateTimeOffset.UtcNow.AddSeconds(token.ExpiresIn) };
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), properties);
        return Redirect("/users");
    }

    [HttpPost("/account/logout")]
    public async Task<IActionResult> LogoutAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Redirect("/");
    }
}
