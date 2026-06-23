using System.Collections.Concurrent;
using Hhs.MockApi.AudioHQ;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<MockApiOptions>(builder.Configuration.GetSection("MockApi"));
builder.Services.AddSingleton<AudioHQService>();

var app = builder.Build();

var options = app.Services.GetRequiredService<IOptions<MockApiOptions>>().Value;
var mockFilesDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", options.MediaDirectory);
Directory.CreateDirectory(mockFilesDir);

app.MapPost("/audio/generate", (AudioRequest request, AudioHQService service) =>
{
    var trackingId = service.CreateRequest(request.InputText);
    var downloadUrl = $"{service.GetBaseUrl()}/audio/download/{trackingId}";
    return Results.Ok(new { provider = "audio-hq", trackingId, remoteFileUrl = downloadUrl, pollingWindowSec = options.PollingWindowSeconds });
});

app.MapGet("/audio/status/{trackingId}", async (string trackingId, AudioHQService service, CancellationToken ct) =>
{
    var (isReady, fileUrl, error, fileName) = await service.GetStatusAsync(trackingId, mockFilesDir, options, ct);
    if (!isReady)
        return Results.Ok(new { provider = "audio-hq", trackingId, status = "processing", error });
    return Results.Ok(new { provider = "audio-hq", trackingId, status = "completed", remoteFileUrl = fileUrl, fileName });
});

app.MapGet("/audio/download/{trackingId}", async (string trackingId) =>
{
    var filePath = AudioHQService.GetFilePath(trackingId, mockFilesDir, options);
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
        public int ProcessingDelaySeconds { get; set; } = 20;
        public int PollingWindowSeconds { get; set; } = 150;
        public string MediaDirectory { get; set; } = "media";
    }

    public sealed class AudioHQService
    {
        private static readonly ConcurrentDictionary<string, AudioHQEntry> Store = new();
        private readonly IOptions<MockApiOptions> _options;
        private const string ProviderName = "audio-hq";

        public AudioHQService(IOptions<MockApiOptions> options)
        {
            _options = options;
        }

        public string GetBaseUrl() => _options.Value.SelfBaseUrl;

        public string CreateRequest(string inputText)
        {
            var trackingId = Guid.CreateVersion7().ToString("N").ToLower();
            var createdAt = DateTime.UtcNow;
            Store[trackingId] = new AudioHQEntry { InputText = inputText, CreatedAt = createdAt };
            return trackingId;
        }

        public async Task<(bool IsReady, string? FileUrl, string? Error, string? FileName)> GetStatusAsync(string trackingId, string mockFilesDir, MockApiOptions options, CancellationToken cancellationToken)
        {
            if (!Store.TryGetValue(trackingId, out var entry))
                return (false, null, "Not found", null);

            var elapsed = DateTime.UtcNow - entry.CreatedAt;
            if (elapsed.TotalSeconds < options.ProcessingDelaySeconds)
                return (false, null, null, null);

            var filePath = GetFilePath(trackingId, mockFilesDir, options);
            var fileName = Path.GetFileName(filePath);

            if (!File.Exists(filePath))
            {
                await File.WriteAllTextAsync(filePath, $"Mock Audio File\nTracking ID: {trackingId}\nCreated: {DateTime.UtcNow:O}", cancellationToken);
            }

            var downloadUrl = $"{_options.Value.SelfBaseUrl}/audio/download/{trackingId}";
            return (true, downloadUrl, null, fileName);
        }

        public static string GetFilePath(string trackingId, string mockFilesDir, MockApiOptions options)
        {
            return Path.Combine(mockFilesDir, $"{ProviderName}-{trackingId}.mp3");
        }
    }

    public sealed class AudioHQEntry
    {
        public string InputText { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public sealed record AudioRequest(string InputText);
}
