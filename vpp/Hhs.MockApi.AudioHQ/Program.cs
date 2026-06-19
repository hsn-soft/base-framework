using System.Collections.Concurrent;
using Hhs.MockApi.AudioHQ;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<MockApiOptions>(builder.Configuration.GetSection("MockApi"));
builder.Services.AddSingleton<AudioHQService>();

var app = builder.Build();

// Create local storage directory for mock files
var mockFilesDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "media");
Directory.CreateDirectory(mockFilesDir);

app.MapPost("/audio/generate", (AudioRequest request, AudioHQService service) =>
{
    var trackingId = service.CreateRequest(request.InputText);
    return Results.Ok(new { provider = "audio-hq", trackingId, pollingWindowSec = 60 });
});

app.MapGet("/audio/status/{trackingId}", async (string trackingId, AudioHQService service, CancellationToken ct) =>
{
    var (isReady, fileUrl, error, fileName) = await service.GetStatusAsync(trackingId, mockFilesDir, ct);
    if (!isReady)
        return Results.Ok(new { provider = "audio-hq", trackingId, status = "processing", error });
    return Results.Ok(new { provider = "audio-hq", trackingId, status = "completed", remoteFileUrl = fileUrl, fileName });
});

app.MapGet("/audio/download/{trackingId}", async (string trackingId) =>
{
    var filePath = AudioHQService.GetFilePath(trackingId, mockFilesDir);
    if (!File.Exists(filePath))
        return Results.NotFound();

    var fileContent = await File.ReadAllBytesAsync(filePath);
    return Results.File(fileContent, "application/octet-stream", Path.GetFileName(filePath));
});

app.Run();

namespace Hhs.MockApi.AudioHQ
{
    public sealed class MockApiOptions
    {
        public string SelfBaseUrl { get; set; } = string.Empty;
    }

    public sealed class AudioHQService
    {
        private static readonly ConcurrentDictionary<string, AudioHQEntry> Store = new();
        private readonly IOptions<MockApiOptions> _options;

        public AudioHQService(IOptions<MockApiOptions> options)
        {
            _options = options;
        }

        public string CreateRequest(string inputText)
        {
            var trackingId = Guid.NewGuid().ToString("N");
            var createdAt = DateTime.UtcNow;
            Store[trackingId] = new AudioHQEntry { InputText = inputText, CreatedAt = createdAt };
            return trackingId;
        }

        public async Task<(bool IsReady, string? FileUrl, string? Error, string? FileName)> GetStatusAsync(string trackingId, string mockFilesDir, CancellationToken cancellationToken)
        {
            if (!Store.TryGetValue(trackingId, out var entry))
                return (false, null, "Not found", null);

            var elapsed = DateTime.UtcNow - entry.CreatedAt;
            if (elapsed.TotalSeconds < 20)
                return (false, null, null, null);

            // Create mock file if it doesn't exist
            var filePath = GetFilePath(trackingId, mockFilesDir);
            var fileName = Path.GetFileName(filePath);

            if (!File.Exists(filePath))
            {
                await File.WriteAllTextAsync(filePath, $"Mock Audio File\nTracking ID: {trackingId}\nCreated: {DateTime.UtcNow:O}", cancellationToken);
            }

            // Return download URL that points to our endpoint
            var downloadUrl = $"{_options.Value.SelfBaseUrl}/audio/download/{trackingId}";
            return (true, downloadUrl, null, fileName);
        }

        public static string GetFilePath(string trackingId, string mockFilesDir)
        {
            return Path.Combine(mockFilesDir, $"mock_audio_hq_{trackingId}.mp3.txt");
        }
    }

    public sealed class AudioHQEntry
    {
        public string InputText { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public sealed record AudioRequest(string InputText);
}