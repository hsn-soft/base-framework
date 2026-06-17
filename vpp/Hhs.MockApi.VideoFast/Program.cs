var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<VideoFastService>();

var app = builder.Build();

var mockFilesDir = Path.Combine(Path.GetTempPath(), "mock-provider-files");
Directory.CreateDirectory(mockFilesDir);

app.MapPost("/video/generate", async (VideoRequest request, CancellationToken ct) =>
{
    var (trackingId, fileUrl, fileName) = await VideoFastService.GenerateVideoAsync(request.AudioUrls, mockFilesDir, ct);
    return Results.Ok(new { provider = "video-fast", remoteFileUrl = fileUrl, fileName, processingMs = 5000, trackingId });
});

app.MapGet("/video/download/{trackingId}", async (string trackingId) =>
{
    var filePath = VideoFastService.GetFilePath(trackingId, mockFilesDir);
    if (!System.IO.File.Exists(filePath))
        return Results.NotFound();

    var fileContent = await System.IO.File.ReadAllBytesAsync(filePath);
    return Results.File(fileContent, "application/octet-stream", Path.GetFileName(filePath));
});

app.Run();

public sealed class VideoFastService
{
    public static async Task<(string TrackingId, string FileUrl, string FileName)> GenerateVideoAsync(List<string> audioUrls, string mockFilesDir, CancellationToken cancellationToken)
    {
        await Task.Delay(5000, cancellationToken);
        var trackingId = Guid.NewGuid().ToString("N");

        var filePath = GetFilePath(trackingId, mockFilesDir);
        var fileName = Path.GetFileName(filePath);
        var audioInfo = audioUrls.Count > 0 ? $"Audio URLs: {string.Join(", ", audioUrls)}" : "No audio";
        await System.IO.File.WriteAllTextAsync(filePath, $"Mock Video File\nTracking ID: {trackingId}\n{audioInfo}\nCreated: {DateTime.UtcNow:O}", cancellationToken);

        var downloadUrl = $"http://localhost:5044/video/download/{trackingId}";
        return (trackingId, downloadUrl, fileName);
    }

    public static string GetFilePath(string trackingId, string mockFilesDir)
    {
        return Path.Combine(mockFilesDir, $"mock_video_fast_{trackingId}.mp4.txt");
    }
}

public sealed record VideoRequest(List<string> AudioUrls);
