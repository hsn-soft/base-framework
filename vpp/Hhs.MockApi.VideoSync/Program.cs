using Hhs.MockApi.VideoSync;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

var mockFilesDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "media");
Directory.CreateDirectory(mockFilesDir);

app.MapPost("/video/generate", async (VideoRequest request, CancellationToken ct) =>
{
    var (trackingId, fileUrl, fileName) = await GenerateVideoAsync(request.AudioUrls, mockFilesDir, ct);
    return Results.Ok(new { provider = "video-sync", remoteFileUrl = fileUrl, fileName, processingMs = 5000, trackingId });
});

app.MapGet("/video/download/{trackingId}", async (string trackingId) =>
{
    var filePath = GetFilePath(trackingId, mockFilesDir);
    if (!System.IO.File.Exists(filePath))
        return Results.NotFound();

    var fileContent = await System.IO.File.ReadAllBytesAsync(filePath);
    return Results.File(fileContent, "application/octet-stream", Path.GetFileName(filePath));
});

app.MapGet("/video/status/{trackingId}", async (string trackingId) =>
{
    var filePath = GetFilePath(trackingId, mockFilesDir);
    if (!System.IO.File.Exists(filePath))
        return Results.NotFound();

    var fileName = Path.GetFileName(filePath);
    var downloadUrl = $"http://localhost:5045/video/download/{trackingId}";
    return Results.Ok(new
    {
        status = "completed",
        remoteFileUrl = downloadUrl,
        fileName
    });
});

app.Run();

static async Task<(string TrackingId, string FileUrl, string FileName)> GenerateVideoAsync(List<string> audioUrls, string mockFilesDir, CancellationToken cancellationToken)
{
    await Task.Delay(5000, cancellationToken);
    var trackingId = Guid.NewGuid().ToString("N");

    var filePath = GetFilePath(trackingId, mockFilesDir);
    var fileName = Path.GetFileName(filePath);
    var audioInfo = audioUrls.Count > 0 ? $"Audio URLs: {string.Join(", ", audioUrls)}" : "No audio";
    await System.IO.File.WriteAllTextAsync(filePath, $"Mock Video File\nTracking ID: {trackingId}\n{audioInfo}\nCreated: {DateTime.UtcNow:O}", cancellationToken);

    var downloadUrl = $"http://localhost:5045/video/download/{trackingId}";
    return (trackingId, downloadUrl, fileName);
}

static string GetFilePath(string trackingId, string mockFilesDir)
{
    return Path.Combine(mockFilesDir, $"mock_video_sync_{trackingId}.mp4.txt");
}

namespace Hhs.MockApi.VideoSync
{
    public sealed record VideoRequest(List<string> AudioUrls);
}
