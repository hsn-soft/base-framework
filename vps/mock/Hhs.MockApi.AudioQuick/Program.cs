using Hhs.MockApi.AudioQuick;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<MockApiOptions>(builder.Configuration.GetSection("MockApi"));
builder.Services.AddSingleton<AudioQuickService>();
builder.Services.AddHostedService<StaleMockFileCleanupWorker>();

var app = builder.Build();

var options = app.Services.GetRequiredService<IOptions<MockApiOptions>>().Value;
var mockFilesDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", options.MediaDirectory);
Directory.CreateDirectory(mockFilesDir);

app.MapPost("/audio/generate", async (AudioRequest request, AudioQuickService service, CancellationToken ct) =>
{
    var (trackingId, fileUrl, fileName) = await service.GenerateAudioAsync(request.InputText, mockFilesDir, options, ct);
    return Results.Ok(new { provider = "audio-quick", trackingId, remoteFileUrl = fileUrl, fileName, processingMs = options.ProcessingDelaySeconds * 1000 });
});

app.MapGet("/audio/download/{trackingId}", async (string trackingId) =>
{
    var filePath = AudioQuickService.GetFilePath(trackingId, mockFilesDir, options);
    if (!System.IO.File.Exists(filePath))
        return Results.NotFound();

    var fileContent = await System.IO.File.ReadAllBytesAsync(filePath);
    return Results.File(fileContent, "application/octet-stream", Path.GetFileName(filePath));
});

app.MapGet("/audio/status/{trackingId}", async (string trackingId, AudioQuickService service) =>
{
    var filePath = AudioQuickService.GetFilePath(trackingId, mockFilesDir, options);
    if (!System.IO.File.Exists(filePath))
        return Results.NotFound();

    var fileName = Path.GetFileName(filePath);
    var downloadUrl = $"{service.GetBaseUrl()}/audio/download/{trackingId}";
    return Results.Ok(new
    {
        status = "completed",
        remoteFileUrl = downloadUrl,
        fileName
    });
});

app.Run();

namespace Hhs.MockApi.AudioQuick
{
    public sealed class MockApiOptions
    {
        public string SelfBaseUrl { get; set; } = string.Empty;
        public int ProcessingDelaySeconds { get; set; } = 3;
        public string MediaDirectory { get; set; } = "media";
    }

    public sealed class AudioQuickService
    {
        private readonly IOptions<MockApiOptions> _options;
        private const string ProviderName = "audio-quick";

        public AudioQuickService(IOptions<MockApiOptions> options)
        {
            _options = options;
        }

        public string GetBaseUrl() => _options.Value.SelfBaseUrl;

        public async Task<(string TrackingId, string FileUrl, string FileName)> GenerateAudioAsync(string inputText, string mockFilesDir, MockApiOptions options, CancellationToken cancellationToken)
        {
            await Task.Delay(options.ProcessingDelaySeconds * 1000, cancellationToken);
            var trackingId = Guid.CreateVersion7().ToString("N").ToLower();

            var filePath = GetFilePath(trackingId, mockFilesDir, options);
            var fileName = Path.GetFileName(filePath);

            await System.IO.File.WriteAllTextAsync(filePath, $"Mock Audio File\nTracking ID: {trackingId}\nCreated: {DateTime.UtcNow:O}", cancellationToken);

            var downloadUrl = $"{_options.Value.SelfBaseUrl}/audio/download/{trackingId}";
            return (trackingId, downloadUrl, fileName);
        }

        public static string GetFilePath(string trackingId, string mockFilesDir, MockApiOptions options)
        {
            return Path.Combine(mockFilesDir, $"{ProviderName}-{trackingId}.mp3");
        }
    }

    public sealed record AudioRequest(string InputText);

    /// <summary>
    /// Deletes generated mock media files older than 1 hour (by creation time) so the mock's
    /// media directory doesn't grow unbounded across repeated local/CI runs. MinIO/CDN storage
    /// is a separate mock (Hhs.MockApi.CdnLocalMinio) and is intentionally untouched here.
    /// </summary>
    public sealed class StaleMockFileCleanupWorker(IOptions<MockApiOptions> options, ILogger<StaleMockFileCleanupWorker> logger) : BackgroundService
    {
        private static readonly TimeSpan MaxFileAge = TimeSpan.FromHours(1);
        private static readonly TimeSpan ScanInterval = TimeSpan.FromMinutes(10);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            string mockFilesDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", options.Value.MediaDirectory);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    CleanupStaleFiles(mockFilesDir);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Stale mock file cleanup failed for directory {MockFilesDir}", mockFilesDir);
                }

                try
                {
                    await Task.Delay(ScanInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private void CleanupStaleFiles(string mockFilesDir)
        {
            if (!Directory.Exists(mockFilesDir)) return;

            var cutoffUtc = DateTime.UtcNow - MaxFileAge;

            foreach (string filePath in Directory.EnumerateFiles(mockFilesDir))
            {
                try
                {
                    if (File.GetCreationTimeUtc(filePath) >= cutoffUtc) continue;

                    File.Delete(filePath);
                    logger.LogInformation("Deleted stale mock file: {FilePath}", filePath);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to delete stale mock file: {FilePath}", filePath);
                }
            }
        }
    }
}
