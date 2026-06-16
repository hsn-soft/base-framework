var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<OutlineAbcService>();

var app = builder.Build();

app.MapPost("/outline/generate", async (OutlineRequest request, OutlineAbcService service, CancellationToken ct) =>
{
    var result = await service.GenerateOutlineAsync(request.InputText, ct);
    return Results.Ok(new { provider = "outline-abc", script = result, processingMs = 5000 });
});

app.Run();

public sealed class OutlineAbcService
{
    public async Task<string> GenerateOutlineAsync(string inputText, CancellationToken cancellationToken)
    {
        await Task.Delay(5000, cancellationToken);
        var lines = inputText.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
        var outline = string.Join("\n", lines.Select((line, i) => $"[ABC] Point {i + 1}: {line.Trim()}"));
        return outline;
    }
}

public sealed record OutlineRequest(string InputText);
