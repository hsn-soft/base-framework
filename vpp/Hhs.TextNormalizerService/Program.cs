using Hhs.Shared.Events;
using Hhs.Shared.RabbitMQ;
using Hhs.Shared.Retry;
using Hhs.Shared.Configuration;
using Hhs.TextNormalizerService.Configuration;
using Hhs.TextNormalizerService.Configuration.Providers.Outline;
using Hhs.TextNormalizerService.Handlers;
using Hhs.TextNormalizerService.Infrastructure;
using Hhs.TextNormalizerService.Mongo;
using Hhs.TextNormalizerService.Providers;
using Hhs.TextNormalizerService.Providers.Outline;
using Hhs.TextNormalizerService.Providers.Scraping;
using Hhs.TextNormalizerService.Services;
using Hhs.TextNormalizerService.Workers;

SubscriptionScopeRegistry.Initialize();

var builder = WebApplication.CreateBuilder(args);

// ============================================================================
// 1. INFRASTRUCTURE & MESSAGING CONFIGURATION
// ============================================================================

builder.Services.Configure<MongoOptions>(builder.Configuration.GetSection("MongoDb"));
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IEventBus, RabbitMqEventBus>();

// ============================================================================
// 2. OUTLINE PROVIDER CONFIGURATION
// ============================================================================

var outlineFastProviderSettings = builder.Configuration.GetSection(OutlineFastProviderSettings.SectionName)
    .Get<OutlineFastProviderSettings>() ?? new OutlineFastProviderSettings();
builder.Services.AddSingleton(outlineFastProviderSettings);

var outlineQueueProviderSettings = builder.Configuration.GetSection(OutlineQueueProviderSettings.SectionName)
    .Get<OutlineQueueProviderSettings>() ?? new OutlineQueueProviderSettings();
builder.Services.AddSingleton(outlineQueueProviderSettings);

// Register Outline Provider implementations
builder.Services.AddScoped<IOutlineProvider, OutlineFastProvider>();
builder.Services.AddScoped<IOutlineProvider, OutlineQueueProvider>();
builder.Services.AddScoped<IOutlineProviderResolver, OutlineProviderResolver>();

// ============================================================================
// 3. POLLING & RETRY CONFIGURATION
// ============================================================================

var outlinePollingSettings = builder.Configuration.GetSection(OutlinePollingSettings.SectionName)
    .Get<OutlinePollingSettings>() ?? new OutlinePollingSettings();
builder.Services.AddSingleton(outlinePollingSettings);

var normalizerRetrySettings = builder.Configuration.GetSection(nameof(NormalizerRetrySettings))
    .Get<NormalizerRetrySettings>() ?? new NormalizerRetrySettings();
builder.Services.AddSingleton(normalizerRetrySettings);
builder.Services.AddSingleton(_ => new RetryDelayCalculator(normalizerRetrySettings.DelaySeconds));

// ============================================================================
// 4. DATABASE CONFIGURATION
// ============================================================================

BsonRegisterTools.MongoConfigure();
builder.Services.AddSingleton<NormalizerMongoContext>();

// ============================================================================
// 5. APPLICATION SERVICES & UTILITIES
// ============================================================================

builder.Services.AddScoped<NormalizerInboxStore>();
builder.Services.AddScoped<NormalizerOperationAppService>();
builder.Services.AddScoped<IContentScraper, DummyContentScraper>();

// ============================================================================
// 6. EVENT HANDLERS
// ============================================================================

// Customer Content Handlers
builder.Services.AddScoped<CustomerContentCreatedEtoHandler>();
builder.Services.AddScoped<CustomerContentNormalizeRequestCreatedEtoHandler>();
builder.Services.AddScoped<CustomerContentScrapingStartedEtoHandler>();
builder.Services.AddScoped<CustomerContentScrapingCompletedEtoHandler>();
builder.Services.AddScoped<CustomerContentOutlineStartedEtoHandler>();
builder.Services.AddScoped<CustomerContentOutlineCompletedEtoHandler>();

// Analysis Content Handlers
builder.Services.AddScoped<AnalysisContentCreatedEtoHandler>();
builder.Services.AddScoped<AnalysisContentNormalizeRequestCreatedEtoHandler>();
builder.Services.AddScoped<AnalysisItemScrapingStartedEtoHandler>();
builder.Services.AddScoped<AnalysisItemScrapingCompletedEtoHandler>();
builder.Services.AddScoped<AnalysisItemOutlineStartedEtoHandler>();
builder.Services.AddScoped<AnalysisItemOutlineCompletedEtoHandler>();

// Outline Provider Handlers
builder.Services.AddScoped<OutlineProviderRequestStartedEtoHandler>();
builder.Services.AddScoped<OutlineProviderCompletedEtoHandler>();

// ============================================================================
// 7. BACKGROUND WORKERS / HOSTED SERVICES
// ============================================================================

// Outline Provider Event Workers
builder.Services.AddHostedService<RabbitMqConsumerHostedService<OutlineProviderRequestStartedEto, OutlineProviderRequestStartedEtoHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<OutlineProviderCompletedEto, OutlineProviderCompletedEtoHandler>>();

// Customer Content Event Workers
builder.Services.AddHostedService<RabbitMqConsumerHostedService<CustomerContentCreatedEto, CustomerContentCreatedEtoHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<CustomerContentNormalizeRequestCreatedEto, CustomerContentNormalizeRequestCreatedEtoHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<CustomerContentScrapingStartedEto, CustomerContentScrapingStartedEtoHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<CustomerContentScrapingCompletedEto, CustomerContentScrapingCompletedEtoHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<CustomerContentOutlineStartedEto, CustomerContentOutlineStartedEtoHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<CustomerContentOutlineCompletedEto, CustomerContentOutlineCompletedEtoHandler>>();

// Analysis Content Event Workers
builder.Services.AddHostedService<RabbitMqConsumerHostedService<AnalysisContentCreatedEto, AnalysisContentCreatedEtoHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<AnalysisContentNormalizeRequestCreatedEto, AnalysisContentNormalizeRequestCreatedEtoHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<AnalysisItemScrapingStartedEto, AnalysisItemScrapingStartedEtoHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<AnalysisItemScrapingCompletedEto, AnalysisItemScrapingCompletedEtoHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<AnalysisItemOutlineStartedEto, AnalysisItemOutlineStartedEtoHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<AnalysisItemOutlineCompletedEto, AnalysisItemOutlineCompletedEtoHandler>>();

// ============================================================================
// 8. CUSTOM WORKERS (Polling & Retry Logic)
// ============================================================================

builder.Services.AddScoped<OutlineProviderPollingAppService>();
builder.Services.AddHostedService<OutlineProviderPollingWorker>();

builder.Services.AddScoped<NormalizerRetryAppService>();
builder.Services.AddHostedService<NormalizerRetryWorker>();

// ============================================================================
// 9. BUILD APPLICATION
// ============================================================================

var app = builder.Build();



app.Run();