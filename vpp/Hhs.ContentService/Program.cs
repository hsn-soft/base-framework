using Hhs.ContentService;
using Hhs.ContentService.Data;
using Hhs.ContentService.Handlers;
using Hhs.ContentService.Infrastructure;
using Hhs.ContentService.Services;
using Hhs.Shared.Events;
using Hhs.Shared.RabbitMQ;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ContentDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMq"));
builder.Services.AddSingleton<IEventBus, RabbitMqEventBus>();

builder.Services.AddScoped<ContentInboxStore>();
builder.Services.AddScoped<ContentOperationAppService>();

builder.Services.AddScoped<NormalizerResultPublishedEventHandler>();
builder.Services.AddScoped<VideoGenerationResultPublishedEventHandler>();
builder.Services.AddScoped<StepFailedEventHandler>();

builder.Services.AddHostedService<RabbitMqConsumerHostedService<StepFailedEvent, StepFailedEventHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<NormalizerResultPublishedEvent, NormalizerResultPublishedEventHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<VideoGenerationResultPublishedEvent, VideoGenerationResultPublishedEventHandler>>();

var app = builder.Build();

app.MapPost("/customer-contents", async (
    CreateCustomerContentRequest request,
    ContentOperationAppService appService,
    CancellationToken cancellationToken) =>
{
    var id = await appService.CreateCustomerContentAsync(request, cancellationToken);
    return Results.Ok(new { id });
});

app.MapGet("/customer-contents/{id}", async (
    Guid id,
    ContentDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var content = await dbContext.CustomerContents.FindAsync(new object[] { id }, cancellationToken: cancellationToken);
    if (content == null)
        return Results.NotFound();
    return Results.Ok(new
    {
        id = content.Id,
        status = content.NormalizeStatus == "COMPLETED" && content.VideoStatus == "COMPLETED" ? "COMPLETED" : "PROCESSING",
        normalizeStatus = content.NormalizeStatus,
        videoStatus = content.VideoStatus,
        url = content.Url,
        finalVideoUrl = content.FinalVideoUrl,
        createdAt = content.CreatedAtUtc,
        updatedAt = content.UpdatedAtUtc
    });
});

app.MapPost("/analysis-contents", async (
    CreateAnalysisContentRequest request,
    ContentOperationAppService appService,
    CancellationToken cancellationToken) =>
{
    var id = await appService.CreateAnalysisContentAsync(request, cancellationToken);
    return Results.Ok(new { id });
});

app.MapGet("/analysis-contents/{id}", async (
    Guid id,
    ContentDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var content = await dbContext.AnalysisContents
        .Include(x => x.Items)
        .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    if (content == null)
        return Results.NotFound();

    return Results.Ok(new
    {
        id = content.Id,
        status = content.NormalizeStatus == "COMPLETED" && content.VideoStatus == "COMPLETED" ? "COMPLETED" : "PROCESSING",
        normalizeStatus = content.NormalizeStatus,
        videoStatus = content.VideoStatus,
        title = content.Title,
        customerContentIds = content.Items.OrderBy(x => x.SortOrder).Select(x => x.CustomerContentId).ToList(),
        createdAt = content.CreatedAtUtc,
        updatedAt = content.UpdatedAtUtc
    });
});

app.MapPost("/demo/customer1/openai", async (
    CreateCustomerContentRequest request,
    ContentOperationAppService appService,
    CancellationToken cancellationToken) =>
{
    var id = await appService.CreateCustomerContentAsync(
        request with { OutlineProviderKey = "openai" },
        cancellationToken);

    return Results.Ok(new
    {
        message = "Customer1 created with OpenAI (immediate result - 5sec processing)",
        contentId = id,
        providerKey = "openai",
        processingMs = 5000
    });
});

app.MapPost("/demo/customer2/custom-xyz", async (
    CreateCustomerContentRequest request,
    ContentOperationAppService appService,
    CancellationToken cancellationToken) =>
{
    var id = await appService.CreateCustomerContentAsync(
        request with { OutlineProviderKey = "custom-xyz" },
        cancellationToken);

    return Results.Ok(new
    {
        message = "Customer2 created with CustomXyz (polling - 30sec wait)",
        contentId = id,
        providerKey = "custom-xyz",
        pollingWindowSec = 30
    });
});

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ContentDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.Run();

namespace Hhs.ContentService
{
    public sealed record CreateCustomerContentRequest(
        string Url,
        string? OutlineProviderKey = "openai",
        string? VideoProviderKey = "video-external",
        string? AudioProviderKey = "audio-def");

    public sealed record CreateAnalysisContentRequest(
        string Title,
        List<Guid> CustomerContentIds,
        string OutlineProviderKey = "openai",
        string VideoProviderKey = "video-a",
        string? AudioProviderKey = "audio-a");
}