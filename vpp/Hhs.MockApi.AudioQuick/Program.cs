var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<AudioQuickService>();

var app = builder.Build();

app.MapPost("/audio/generate", async (AudioRequest request, AudioQuickService service, CancellationToken ct) =>
{
    var fileUrl = await service.GenerateAudioAsync(request.InputText, ct);
    return Results.Ok(new { provider = "audio-quick", remoteFileUrl = fileUrl, processingMs = 3000 });
});

app.Run();

public sealed class AudioQuickService
{
    public async Task<string> GenerateAudioAsync(string inputText, CancellationToken cancellationToken)
    {
        await Task.Delay(3000, cancellationToken);
        var audioId = Guid.NewGuid().ToString("N");
        return $"https://audio-quick-api.internal/audio/{audioId}/output.mp3";
    }
}

public sealed record AudioRequest(string InputText);
