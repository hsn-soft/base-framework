using Hhs.TextNormalizerService.Application.Contracts.Providers;
using Hhs.TextNormalizerService.Application.Contracts.Providers.Dtos.Scraping;
using Hhs.TextNormalizerService.Controllers.Base;
using HsnSoft.Base.Logging.Abstracts;
using HsnSoft.Base.PuppeTeer;
using Microsoft.AspNetCore.Mvc;

namespace Hhs.TextNormalizerService.Controllers;

[Route("api/text-normalizer-service/v1/commercial/scraping")]
public sealed class ScrapingController : BaseServiceController
{
    private readonly IAppConsoleLogger _logger;
    private readonly IPuppeteerBrowser _puppeTeer;
    private readonly IScrapingProvider _scrapingProvider;

    public ScrapingController(IServiceProvider provider, IAppConsoleLogger logger, IPuppeteerBrowser puppeTeer, IScrapingProvider scrapingProvider) : base(provider)
    {
        _logger = logger;
        _puppeTeer = puppeTeer;
        _scrapingProvider = scrapingProvider;
    }

    [HttpPost("test")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ScrapingResponseDto> TestAsync([FromBody] ScrapingRequestDto input)
    {
        // var env = ServiceProvider.GetRequiredService<IWebHostEnvironment>();
        // if (env.IsHostProduction()) throw new BaseHttpException((int)HttpStatusCode.Forbidden);

        var res = await _scrapingProvider.ScrapingAsync(input);

        return res.HasError ? throw new Exception("Test failed: " + string.Join(",", res.Errors)) : res;
    }

    [HttpPost("is-puppeteer-ready")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IsPuppeTeerReadyDto> IsPuppeTeerReadyAsync()
    {
        string returnString = "";
        try
        {
            await Task.Delay(100);

            if (_puppeTeer != null)
            {
                if (await _puppeTeer.GetBrowserSafelyAsync() != null)
                {
                    var blankPage = await (await _puppeTeer.GetBrowserSafelyAsync()).NewPageAsync();
                    if (blankPage != null)
                    {
                        returnString = "PUPPETEER_IS_READY";
                        blankPage.Dispose();
                    }
                    else
                    {
                        returnString = "PUPPETEER BLANK PAGE IS NULL : ";
                    }
                }
                else
                {
                    returnString = "PUPPETEER BROWSER IS NULL : " + _puppeTeer.InitializationResult;
                }
            }
            else
            {
                returnString = "PUPPETEER SERVICE IS NULL";
            }
        }
        catch (Exception e)
        {
            string inContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER");
            bool containerStatus = !string.IsNullOrWhiteSpace(inContainer) && inContainer == "true";
            returnString = $"DOTNET_RUNNING_IN_CONTAINER: {containerStatus.ToString()},PUPPETEER_UNKNOWN_ERROR : {e.Message}";
        }

        return new IsPuppeTeerReadyDto { Result = returnString };
    }
}

public class IsPuppeTeerReadyDto
{
    public string Result { get; set; }
}