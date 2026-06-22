using Hhs.MockApi.OutlineFast;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<MockApiOptions>(builder.Configuration.GetSection("MockApi"));

var app = builder.Build();

var options = app.Services.GetRequiredService<IOptions<MockApiOptions>>().Value;

app.MapPost("/outline/generate", async (OutlineRequest request, CancellationToken ct) =>
{
    var script = await GenerateOutlineAsync(request.InputText, options, ct);
    return Results.Ok(new { provider = "outline-fast", script, processingMs = options.ProcessingDelaySeconds * 1000 });
});

app.Run();

async Task<string> GenerateOutlineAsync(string inputText, MockApiOptions options, CancellationToken cancellationToken)
{
    await Task.Delay(options.ProcessingDelaySeconds * 1000, cancellationToken);
    var lines = inputText.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
    var outline = string.Join("\n", lines.Select((line, i) => $"[Fast] Point {i + 1}: {line.Trim()}"));
    return outline;
}

namespace Hhs.MockApi.OutlineFast
{
    public sealed class MockApiOptions
    {
        public int ProcessingDelaySeconds { get; set; } = 5;
    }

    public sealed record OutlineRequest(string InputText);
}
