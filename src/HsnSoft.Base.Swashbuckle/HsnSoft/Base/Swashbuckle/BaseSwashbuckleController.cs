using HsnSoft.Base.AspNetCore.Mvc;
using HsnSoft.Base.Auditing;
using Microsoft.AspNetCore.Mvc;

namespace HsnSoft.Base.Swashbuckle;

[Area("Base")]
[Route("Base/Swashbuckle/[action]")]
[DisableAuditing]
[ApiExplorerSettings(IgnoreApi = true)]
public class BaseSwashbuckleController : BaseController
{
    // private readonly IBaseAntiForgeryManager _antiForgeryManager;
    //
    // public BaseSwashbuckleController(IBaseAntiForgeryManager antiForgeryManager)
    // {
    //     _antiForgeryManager = antiForgeryManager;
    // }

    [HttpGet]
    public void SetCsrfCookie()
    {
        // _antiForgeryManager.SetCookie();
    }
}