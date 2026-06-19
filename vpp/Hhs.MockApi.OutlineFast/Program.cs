using Hhs.MockApi.OutlineFast;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapPost("/outline/generate", async (OutlineRequest request, CancellationToken ct) =>
{
    var script = await GenerateOutlineAsync(request.InputText, ct);
    return Results.Ok(new { provider = "outline-fast", script, processingMs = 5000 });
});

app.Run();

static async Task<string> GenerateOutlineAsync(string inputText, CancellationToken cancellationToken)
{
    await Task.Delay(5000, cancellationToken);
    var lines = inputText.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
    var outline = string.Join("\n", lines.Select((line, i) => $"[Fast] Point {i + 1}: {line.Trim()}"));
    return outline;
}

namespace Hhs.MockApi.OutlineFast
{
    public sealed record OutlineRequest(string InputText);
}
