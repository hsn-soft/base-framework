var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<OutlineFastService>();

var app = builder.Build();

app.MapPost("/outline/generate", async (OutlineRequest request, OutlineFastService service, CancellationToken ct) =>
{
    var result = await service.GenerateOutlineAsync(request.InputText, ct);
    return Results.Ok(new { provider = "outline-fast", script = result, processingMs = 5000 });
});

app.Run();

public sealed class OutlineFastService
{
    public async Task<string> GenerateOutlineAsync(string inputText, CancellationToken cancellationToken)
    {
        await Task.Delay(5000, cancellationToken);
        var lines = inputText.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
        var outline = string.Join("\n", lines.Select((line, i) => $"[Fast] Point {i + 1}: {line.Trim()}"));
        return outline;
    }
}

public sealed record OutlineRequest(string InputText);
