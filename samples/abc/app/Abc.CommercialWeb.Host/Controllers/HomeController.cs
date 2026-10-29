using Microsoft.AspNetCore.Mvc;

namespace Abc.CommercialWeb.Host.Controllers;

public sealed class HomeController : Controller
{
    public IActionResult Index() => View();

    public IActionResult Error() => View();
}
