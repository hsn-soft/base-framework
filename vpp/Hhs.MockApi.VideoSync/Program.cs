var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<VideoSyncService>();

var app = builder.Build();

app.MapPost("/video/generate", async (VideoRequest request, VideoSyncService service, CancellationToken ct) =>
{
    var fileUrl = await service.GenerateVideoAsync(request.AudioFilePaths, ct);
    return Results.Ok(new { provider = "video-sync", remoteFileUrl = fileUrl, processingMs = 5000 });
});

app.Run();

public sealed class VideoSyncService
{
    public async Task<string> GenerateVideoAsync(List<string> audioFilePaths, CancellationToken cancellationToken)
    {
        await Task.Delay(5000, cancellationToken);
        var videoId = Guid.NewGuid().ToString("N");
        var audioInfo = audioFilePaths.Count > 0 ? $" (with {audioFilePaths.Count} audio file(s))" : "";
        return $"https://video-sync-api.internal/video/{videoId}/output.mp4{audioInfo}";
    }
}

public sealed record VideoRequest(List<string> AudioFilePaths);
