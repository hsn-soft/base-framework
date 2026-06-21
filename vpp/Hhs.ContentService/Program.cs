using Hhs.ContentService;
using Hhs.ContentService.Data;
using Hhs.ContentService.Handlers;
using Hhs.ContentService.Infrastructure;
using Hhs.ContentService.Services;
using Hhs.Shared.Events;
using Hhs.Shared.RabbitMQ;
using Hhs.Shared.Configuration;
using Microsoft.EntityFrameworkCore;

SubscriptionScopeRegistry.Initialize();

var builder = WebApplication.CreateBuilder(args);

// ============================================================================
// 1. INFRASTRUCTURE & MESSAGING CONFIGURATION
// ============================================================================

builder.Services.AddDbContext<ContentDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));
builder.Services.AddSingleton<IEventBus, RabbitMqEventBus>();
builder.Services.AddHttpClient();

// ============================================================================
// 2. APPLICATION SERVICES
// ============================================================================

builder.Services.AddScoped<ContentInboxStore>();
builder.Services.AddScoped<ContentOperationAppService>();

// ============================================================================
// 3. EVENT HANDLERS
// ============================================================================

builder.Services.AddScoped<NormalizerResultPublishedEtoHandler>();
builder.Services.AddScoped<VideoGenerationResultPublishedEtoHandler>();
builder.Services.AddScoped<StepFailedEtoHandler>();

// ============================================================================
// 4. BACKGROUND WORKERS / HOSTED SERVICES
// ============================================================================

builder.Services.AddHostedService<RabbitMqConsumerHostedService<StepFailedEto, StepFailedEtoHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<NormalizerResultPublishedEto, NormalizerResultPublishedEtoHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<VideoGenerationResultPublishedEto, VideoGenerationResultPublishedEtoHandler>>();

// ============================================================================
// 5. DATABASE INITIALIZATION
// ============================================================================

using (var scope = builder.Services.BuildServiceProvider().CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ContentDbContext>();
    await db.Database.EnsureCreatedAsync();
}

// ============================================================================
// 6. BUILD APPLICATION
// ============================================================================

var app = builder.Build();

// ============================================================================
// 7. API ENDPOINTS
// ============================================================================

// Customer Content Endpoints
app.MapPost("/customer-contents", async (
    CreateCustomerContentRequest request,
    ContentOperationAppService appService,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    string correlationId = httpContext.Request.Headers["X-Correlation-Id"].ToString();
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
        status = content is { NormalizeStatus: StatusNames.Completed, VideoStatus: StatusNames.Completed } ? StatusNames.Completed : StatusNames.Processing,
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

// Analysis Content Endpoints
app.MapPost("/analysis-contents", async (
    CreateAnalysisContentRequest request,
    ContentOperationAppService appService,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    string correlationId = httpContext.Request.Headers["X-Correlation-Id"].ToString();
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
        status = content is { NormalizeStatus: StatusNames.Completed, VideoStatus: StatusNames.Completed } ? StatusNames.Completed : StatusNames.Processing,
        normalizeStatus = content.NormalizeStatus,
        videoStatus = content.VideoStatus,
        title = content.Title,
        customerContentIds = content.Items.OrderBy(x => x.SortOrder).Select(x => x.CustomerContentId).ToList(),
        createdAt = content.CreatedAtUtc,
        updatedAt = content.UpdatedAtUtc
    });
});

// ============================================================================
// 8. RUN APPLICATION
// ============================================================================

app.Run();