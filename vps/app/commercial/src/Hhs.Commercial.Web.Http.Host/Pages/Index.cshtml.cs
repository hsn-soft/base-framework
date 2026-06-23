using Hhs.Commercial.Web.Models;
using HsnSoft.Base.Communication;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;

namespace Hhs.Commercial.Web.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly HttpClient _httpGatewayClient;
    // private readonly HttpClient _httpAuthClient;

    public IndexModel(ILogger<IndexModel> logger, IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;

        // _httpAuthClient = httpClientFactory.CreateClient();
        // _httpAuthClient.BaseAddress = new Uri(configuration.GetValue<string>("AuthServer:Authority"));

        _httpGatewayClient = httpClientFactory.CreateClient();
        _httpGatewayClient.BaseAddress = new Uri(configuration.GetValue<string>("RemoteServices:Default:BaseUrl"));
    }

    public async Task OnGet()
    {
        try
        {
            //https://localhost:7201/administration-service/v1/fakes/filter-list
            var response = await _httpGatewayClient.PostAsJsonAsync("/administration-service/v1/fakes/filter-list", new { });

            var result = await CheckResultAndReturnModel<List<FakeDto>>(response);

            ViewData["ContentFakes"] = (result is { Count: > 0 }) ? result.Select(x => x.FakeCode).ToList() : new List<string> { "#NO_RECORDS_FOUND#" };
        }
        catch (Exception e)
        {
            ViewData["ContentFakes"] = new List<string> { e.Message };
        }
    }

    [ItemCanBeNull]
    private async Task<T> CheckResultAndReturnModel<T>(HttpResponseMessage response) where T : class
    {
        await CheckResult(response);
        BaseResponse<T> baseModel = null;
        try
        {
            string responseContentJson = await response.Content.ReadAsStringAsync();

            baseModel = JsonConvert.DeserializeObject<BaseResponse<T>>(responseContentJson);
        }
        catch (Exception ex)
        {
            throw new Exception($"Response unhandled exception! Message: {ex.Message}");
        }

        if (baseModel == null)
        {
            throw new Exception("Response unhandled exception!");
        }

        if (baseModel.StatusCode >= 400)
        {
            throw new Exception($"error, resul code: {baseModel.StatusCode}, error message: {baseModel.StatusMessagesToSingleMessage()}");
        }

        return baseModel.Payload;
    }

    private async Task CheckResult(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode) return;

        BaseResponse err;
        try { err = await response.Content.ReadFromJsonAsync<BaseResponse>(); }
        catch (Exception) { err = null; }

        err ??= new BaseResponse()
        {
            StatusCode = (int)response.StatusCode,
            StatusMessages = new List<string> { response.ReasonPhrase ?? string.Empty }
        };

        if (err != null)
        {
            throw new Exception($"{err.StatusCode.ToString()}:{err.StatusMessagesToSingleMessage()}");
        }
    }
}