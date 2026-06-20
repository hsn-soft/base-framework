using Hhs.MockApi.VideoFastInternal;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<MockApiOptions>(builder.Configuration.GetSection("MockApi"));
builder.Services.AddSingleton<VideoFastInternalService>();

var app = builder.Build();

var mockFilesDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "media");
Directory.CreateDirectory(mockFilesDir);

app.MapPost("/video/generate", async (VideoRequest request, VideoFastInternalService service, CancellationToken ct) =>
{
    var (trackingId, fileUrl, fileName) = await service.GenerateVideoAsync(mockFilesDir, ct);
    return Results.Ok(new { provider = "video-fast-internal", remoteFileUrl = fileUrl, fileName, processingMs = 5000, trackingId });
});

app.MapGet("/video/download/{trackingId}", async (string trackingId) =>
{
    var filePath = VideoFastInternalService.GetFilePath(trackingId, mockFilesDir);
    if (!System.IO.File.Exists(filePath))
        return Results.NotFound();

    var fileContent = await System.IO.File.ReadAllBytesAsync(filePath);
    return Results.File(fileContent, "application/octet-stream", Path.GetFileName(filePath));
});

app.MapGet("/video/status/{trackingId}", async (string trackingId, VideoFastInternalService service) =>
{
    var filePath = VideoFastInternalService.GetFilePath(trackingId, mockFilesDir);
    if (!System.IO.File.Exists(filePath))
        return Results.NotFound();

    var fileName = Path.GetFileName(filePath);
    var downloadUrl = $"{service.GetBaseUrl()}/video/download/{trackingId}";
    return Results.Ok(new
    {
        status = "completed",
        remoteFileUrl = downloadUrl,
        fileName
    });
});

app.Run();

namespace Hhs.MockApi.VideoFastInternal
{
    public sealed class MockApiOptions
    {
        public string SelfBaseUrl { get; set; } = string.Empty;
    }

    public sealed class VideoFastInternalService
    {
        private readonly IOptions<MockApiOptions> _options;

        public VideoFastInternalService(IOptions<MockApiOptions> options)
        {
            _options = options;
        }

        public string GetBaseUrl() => _options.Value.SelfBaseUrl;

        public async Task<(string TrackingId, string FileUrl, string FileName)> GenerateVideoAsync(string mockFilesDir, CancellationToken cancellationToken)
        {
            await Task.Delay(5000, cancellationToken);
            var trackingId = Guid.NewGuid().ToString("N");

            var filePath = GetFilePath(trackingId, mockFilesDir);
            var fileName = Path.GetFileName(filePath);
            await System.IO.File.WriteAllTextAsync(filePath, $"Mock Video File (Internal Audio)\nTracking ID: {trackingId}\nCreated: {DateTime.UtcNow:O}", cancellationToken);

            var downloadUrl = $"{_options.Value.SelfBaseUrl}/video/download/{trackingId}";
            return (trackingId, downloadUrl, fileName);
        }

        public static string GetFilePath(string trackingId, string mockFilesDir)
        {
            return Path.Combine(mockFilesDir, $"mock_video_fast_internal_{trackingId}.mp4.txt");
        }
    }

    public sealed record VideoRequest(List<string> AudioUrls);
}
