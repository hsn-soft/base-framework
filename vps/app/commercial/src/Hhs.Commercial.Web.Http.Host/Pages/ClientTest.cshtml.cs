using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Hhs.Commercial.Web.Pages;

public class ClientTestModel : PageModel
{
    [BindProperty]
    public string TargetClientUrl { get; set; }

    [BindProperty]
    public string TargetClientId { get; set; }

    private readonly ILogger<ClientTestModel> _logger;

    public ClientTestModel(ILogger<ClientTestModel> logger)
    {
        _logger = logger;
    }

    public void OnGet(string clientUrl, string clientId)
    {
        TargetClientUrl = string.IsNullOrEmpty(clientUrl) ? "" : clientUrl;
        TargetClientId = string.IsNullOrEmpty(clientId) ? "" : clientId;
    }
}