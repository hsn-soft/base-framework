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

app.MapPost("/analysis-contents", async (
    CreateAnalysisContentRequest request,
    ContentOperationAppService appService,
    CancellationToken cancellationToken) =>
{
    var id = await appService.CreateAnalysisContentAsync(request, cancellationToken);
    return Results.Ok(new { id });
});

app.MapPost("/demo/customer1/outline-abc", async (
    DemoCustomerContentRequest request,
    ContentOperationAppService appService,
    CancellationToken cancellationToken) =>
{
    var id = await appService.CreateCustomerContentAsync(new CreateCustomerContentRequest(
        Url: request.Url,
        OutlineProviderKey: "outline-abc",
        VideoProviderKey: request.VideoProviderKey,
        AudioProviderKey: request.AudioProviderKey
    ), cancellationToken);

    return Results.Ok(new
    {
        message = "Customer1 created with outline-abc (immediate result provider)",
        contentId = id,
        providerKey = "outline-abc",
        processingMs = 5000
    });
});

app.MapPost("/demo/customer2/outline-xyz", async (
    DemoCustomerContentRequest request,
    ContentOperationAppService appService,
    CancellationToken cancellationToken) =>
{
    var id = await appService.CreateCustomerContentAsync(new CreateCustomerContentRequest(
        Url: request.Url,
        OutlineProviderKey: "outline-xyz",
        VideoProviderKey: request.VideoProviderKey,
        AudioProviderKey: request.AudioProviderKey
    ), cancellationToken);

    return Results.Ok(new
    {
        message = "Customer2 created with outline-xyz (polling provider)",
        contentId = id,
        providerKey = "outline-xyz",
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
        string OutlineProviderKey = "openai",
        string VideoProviderKey = "video-a",
        string? AudioProviderKey = "audio-a");

    public sealed record CreateAnalysisContentRequest(
        string Title,
        List<Guid> CustomerContentIds,
        string OutlineProviderKey = "openai",
        string VideoProviderKey = "video-a",
        string? AudioProviderKey = "audio-a");

    public sealed record DemoCustomerContentRequest(
        string Url,
        string VideoProviderKey = "video-a",
        string? AudioProviderKey = "audio-a");
}