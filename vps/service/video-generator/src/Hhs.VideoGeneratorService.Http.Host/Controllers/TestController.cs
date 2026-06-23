using System.Net;
using Hhs.Shared.Hosting.Extensions;
using Hhs.VideoGeneratorService.Application.Contracts.Providers;
using Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.VideoGenerate;
using Hhs.VideoGeneratorService.Controllers.Base;
using HsnSoft.Base;
using HsnSoft.Base.Logging.Abstracts;
using Microsoft.AspNetCore.Mvc;

namespace Hhs.VideoGeneratorService.Controllers;

[Route("api/video-generator-service/v1/commercial/test")]
public sealed class TestController : BaseServiceController
{
    private readonly IDidAiVideoGenerationProvider _didAiVideoGenerationProvider;
    private readonly IAppConsoleLogger _logger;
    private readonly IWebHostEnvironment _environment;

    public TestController(IServiceProvider provider, IDidAiVideoGenerationProvider didAiVideoGenerationProvider, IAppConsoleLogger logger, IWebHostEnvironment environment) : base(provider)
    {
        _didAiVideoGenerationProvider = didAiVideoGenerationProvider;
        _logger = logger;
        _environment = environment;
    }

    [HttpPost("download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<TestDownloadResultDto> TestDownloadAsync([FromBody] string externalVideoUrl)
    {
        if (_environment.IsHostProduction())
        {
            throw new BaseHttpException((int)HttpStatusCode.Forbidden, "Forbidden endpoint area on production");
        }

        var result = await _didAiVideoGenerationProvider.DownloadAsync(new VideoGenerationDownloadRequestDto { ExternalVideoUrl = externalVideoUrl });


        bool videoDownloadOperationSuccess;
        string errorMessage = null;
        string localVideoPath = null;
        try
        {
            if (string.IsNullOrWhiteSpace(externalVideoUrl))
            {
                throw new ArgumentNullException(nameof(externalVideoUrl));
            }

            var videoDownloadRequest = new VideoGenerationDownloadRequestDto { ExternalVideoUrl = externalVideoUrl };
            var response = await _didAiVideoGenerationProvider.DownloadAsync(videoDownloadRequest);
            if (response is { HasError: false })
            {
                localVideoPath = response.LocalVideoPath;
                videoDownloadOperationSuccess = !string.IsNullOrWhiteSpace(localVideoPath);
            }
            else throw new Exception(response?.ErrorMessage ?? string.Empty);
        }
        catch (Exception ex)
        {
            videoDownloadOperationSuccess = false;
            errorMessage = ex.Message;
        }

        if (!videoDownloadOperationSuccess)
        {
            _logger.LogError("Hata => " + errorMessage);
            return new TestDownloadResultDto { Result = "Hata => " + errorMessage };
        }

        _logger.LogInformation(localVideoPath);
        return new TestDownloadResultDto { Result = localVideoPath };
    }
}

public class TestDownloadResultDto
{
    public string Result { get; set; }
}