using Hhs.ContentService;
using Hhs.ContentService.Data;
using Hhs.ContentService.Handlers;
using Hhs.ContentService.Infrastructure;
using Hhs.ContentService.Services;
using Hhs.Shared.Events;
using Hhs.Shared.RabbitMQ;
using Hhs.Shared.Configuration;
using Microsoft.EntityFrameworkCore;

// Initialize subscription scope registry
SubscriptionScopeRegistry.Initialize();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ContentDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));
builder.Services.AddSingleton<IEventBus, RabbitMqEventBus>();

builder.Services.AddScoped<ContentInboxStore>();
builder.Services.AddScoped<ContentOperationAppService>();

builder.Services.AddScoped<NormalizerResultPublishedEtoHandler>();
builder.Services.AddScoped<VideoGenerationResultPublishedEtoHandler>();
builder.Services.AddScoped<StepFailedEtoHandler>();

builder.Services.AddHostedService<RabbitMqConsumerHostedService<StepFailedEto, StepFailedEtoHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<NormalizerResultPublishedEto, NormalizerResultPublishedEtoHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<VideoGenerationResultPublishedEto, VideoGenerationResultPublishedEtoHandler>>();

var app = builder.Build();

app.MapPost("/customer-contents", async (
    CreateCustomerContentRequest request,
    ContentOperationAppService appService,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var correlationId = httpContext.Request.Headers["X-Correlation-Id"].ToString();
    var id = await appService.CreateCustomerContentAsync(request, correlationId, cancellationToken);
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
        status = content.NormalizeStatus == StatusNames.Completed && content.VideoStatus == StatusNames.Completed ? StatusNames.Completed : StatusNames.Processing,
        normalizeStatus = content.NormalizeStatus,
        videoStatus = content.VideoStatus,
        scopeKey = content.ScopeKey,
        domainName = content.DomainName,
        contentKey = content.ContentKey,
        finalVideoUrl = content.FinalVideoUrl,
        createdAt = content.CreatedAtUtc,
        updatedAt = content.UpdatedAtUtc
    });
});

app.MapPost("/analysis-contents", async (
    CreateAnalysisContentRequest request,
    ContentOperationAppService appService,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var correlationId = httpContext.Request.Headers["X-Correlation-Id"].ToString();
    var id = await appService.CreateAnalysisContentAsync(request, correlationId, cancellationToken);
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
        status = content.NormalizeStatus == StatusNames.Completed && content.VideoStatus == StatusNames.Completed ? StatusNames.Completed : StatusNames.Processing,
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
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var correlationId = httpContext.Request.Headers["X-Correlation-Id"].ToString();
    var id = await appService.CreateCustomerContentAsync(
        request with { OutlineProviderKey = "openai" },
        correlationId,
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
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var correlationId = httpContext.Request.Headers["X-Correlation-Id"].ToString();
    var id = await appService.CreateCustomerContentAsync(
        request with { OutlineProviderKey = "custom-xyz" },
        correlationId,
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
        string ScopeKey,
        string DomainName,
        string ContentKey,
        string? OutlineProviderKey = "openai",
        string? VideoProviderKey = "video-external",
        string? AudioProviderKey = "audio-def");

    public sealed record CreateAnalysisContentRequest(
        string ScopeKey,
        string DomainName,
        string Title,
        List<Guid> CustomerContentIds,
        string OutlineProviderKey = "openai",
        string VideoProviderKey = "video-a",
        string? AudioProviderKey = "audio-a");
}