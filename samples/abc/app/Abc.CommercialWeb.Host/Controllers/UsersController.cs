using Abc.CommercialWeb.Host.Models;
using Abc.CommercialWeb.Host.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Abc.CommercialWeb.Host.Controllers;

[Authorize]
public sealed class UsersController(GatewayClient gateway) : Controller
{
    [HttpGet("/users")]
    public async Task<IActionResult> IndexAsync(string? search, CancellationToken cancellationToken)
        => View(new UsersModel { Search = search, Users = await gateway.ListUsersAsync(search, cancellationToken) });
}
