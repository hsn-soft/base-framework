using Microsoft.AspNetCore.Mvc;

namespace Abc.CommercialWeb.Controllers;

public sealed class HomeController : Controller
{
    public IActionResult Index() => View();

    public IActionResult Error() => View();
}
