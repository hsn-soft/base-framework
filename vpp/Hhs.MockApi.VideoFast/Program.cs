var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<VideoFastService>();

var app = builder.Build();

app.MapPost("/video/generate", async (VideoRequest request, VideoFastService service, CancellationToken ct) =>
{
    var fileUrl = await service.GenerateVideoAsync(request.AudioUrls, ct);
    return Results.Ok(new { provider = "video-fast", remoteFileUrl = fileUrl, processingMs = 5000 });
});

app.Run();

public sealed class VideoFastService
{
    public async Task<string> GenerateVideoAsync(List<string> audioUrls, CancellationToken cancellationToken)
    {
        await Task.Delay(5000, cancellationToken);
        var videoId = Guid.NewGuid().ToString("N");
        var audioInfo = audioUrls.Count > 0 ? $" (with {audioUrls.Count} audio url(s))" : "";
        return $"https://video-fast-api.internal/video/{videoId}/output.mp4{audioInfo}";
    }
}

public sealed record VideoRequest(List<string> AudioUrls);
