var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<AudioQuickService>();

var app = builder.Build();

var mockFilesDir = Path.Combine(Path.GetTempPath(), "mock-provider-files");
Directory.CreateDirectory(mockFilesDir);

app.MapPost("/audio/generate", async (AudioRequest request, CancellationToken ct) =>
{
    var (trackingId, fileUrl) = await AudioQuickService.GenerateAudioAsync(request.InputText, mockFilesDir, ct);
    return Results.Ok(new { provider = "audio-quick", remoteFileUrl = fileUrl, processingMs = 3000, trackingId });
});

app.MapGet("/audio/download/{trackingId}", async (string trackingId) =>
{
    var filePath = AudioQuickService.GetFilePath(trackingId, mockFilesDir);
    if (!System.IO.File.Exists(filePath))
        return Results.NotFound();

    var fileContent = await System.IO.File.ReadAllBytesAsync(filePath);
    return Results.File(fileContent, "application/octet-stream", Path.GetFileName(filePath));
});

app.Run();

public sealed class AudioQuickService
{
    public static async Task<(string TrackingId, string FileUrl)> GenerateAudioAsync(string inputText, string mockFilesDir, CancellationToken cancellationToken)
    {
        await Task.Delay(3000, cancellationToken);
        var trackingId = Guid.NewGuid().ToString("N");

        var filePath = GetFilePath(trackingId, mockFilesDir);
        await System.IO.File.WriteAllTextAsync(filePath, $"Mock Audio File\nTracking ID: {trackingId}\nCreated: {DateTime.UtcNow:O}", cancellationToken);

        var downloadUrl = $"http://localhost:5042/audio/download/{trackingId}";
        return (trackingId, downloadUrl);
    }

    public static string GetFilePath(string trackingId, string mockFilesDir)
    {
        return Path.Combine(mockFilesDir, $"audio-quick-{trackingId}.mp3.txt");
    }
}

public sealed record AudioRequest(string InputText);
