using Abc.PublicWeb.Host.Models;
using Abc.PublicWeb.Host.Services;
using Microsoft.AspNetCore.Mvc;

namespace Abc.PublicWeb.Host.Controllers;

public sealed class HomeController(GatewayClient gateway) : Controller
{
    [HttpGet("/")]
    public IActionResult Index() => View(new CheckModel());

    /// <summary>What a sign-up form asks before it submits: is this user name free?</summary>
    [HttpPost("/")]
    public async Task<IActionResult> IndexAsync(CheckModel model, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(model.UserName))
        {
            model.Error = "Type a user name.";
            return View(model);
        }

        model.Result = await gateway.CheckUserNameAsync(model.UserName, cancellationToken);
        model.Error = model.Result is null ? "The user name could not be checked, try again." : null;
        return View(model);
    }

    public IActionResult Error() => View();
}
