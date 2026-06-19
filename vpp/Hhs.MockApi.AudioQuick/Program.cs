using Hhs.MockApi.AudioQuick;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<MockApiOptions>(builder.Configuration.GetSection("MockApi"));
builder.Services.AddSingleton<AudioQuickService>();

var app = builder.Build();

var mockFilesDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "media");
Directory.CreateDirectory(mockFilesDir);

app.MapPost("/audio/generate", async (AudioRequest request, AudioQuickService service, CancellationToken ct) =>
{
    var (trackingId, fileUrl, fileName) = await service.GenerateAudioAsync(request.InputText, mockFilesDir, ct);
    return Results.Ok(new { provider = "audio-quick", remoteFileUrl = fileUrl, fileName, processingMs = 3000, trackingId });
});

app.MapGet("/audio/download/{trackingId}", async (string trackingId) =>
{
    var filePath = AudioQuickService.GetFilePath(trackingId, mockFilesDir);
    if (!System.IO.File.Exists(filePath))
        return Results.NotFound();

    var fileContent = await System.IO.File.ReadAllBytesAsync(filePath);
    return Results.File(fileContent, "application/octet-stream", Path.GetFileName(filePath));
});

app.MapGet("/audio/status/{trackingId}", async (string trackingId, AudioQuickService service) =>
{
    var filePath = AudioQuickService.GetFilePath(trackingId, mockFilesDir);
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
    }

    public sealed class AudioQuickService
    {
        private readonly IOptions<MockApiOptions> _options;

        public AudioQuickService(IOptions<MockApiOptions> options)
        {
            _options = options;
        }

        public string GetBaseUrl() => _options.Value.SelfBaseUrl;

        public async Task<(string TrackingId, string FileUrl, string FileName)> GenerateAudioAsync(string inputText, string mockFilesDir, CancellationToken cancellationToken)
        {
            await Task.Delay(3000, cancellationToken);
            var trackingId = Guid.NewGuid().ToString("N");

            var filePath = GetFilePath(trackingId, mockFilesDir);
            var fileName = Path.GetFileName(filePath);

            await System.IO.File.WriteAllTextAsync(filePath, $"Mock Audio File\nTracking ID: {trackingId}\nCreated: {DateTime.UtcNow:O}", cancellationToken);

            var downloadUrl = $"{_options.Value.SelfBaseUrl}/audio/download/{trackingId}";
            return (trackingId, downloadUrl, fileName);
        }

        public static string GetFilePath(string trackingId, string mockFilesDir)
        {
            return Path.Combine(mockFilesDir, $"mock_audio_quick_{trackingId}.mp3.txt");
        }
    }

    public sealed record AudioRequest(string InputText);
}