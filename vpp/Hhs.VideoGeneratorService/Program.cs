using Hhs.Shared.Events;
using Hhs.Shared.RabbitMQ;
using Hhs.Shared.Configuration;
using Hhs.Shared.Retry;
using Hhs.VideoGeneratorService.Configuration;
using Hhs.VideoGeneratorService.Configuration.Providers.Audio;
using Hhs.VideoGeneratorService.Configuration.Providers.Video;
using Hhs.VideoGeneratorService.Configuration.Providers.Cdn;
using Hhs.VideoGeneratorService.Providers.Cdn;
using Hhs.VideoGeneratorService.Handlers;
using Hhs.VideoGeneratorService.Infrastructure;
using Hhs.VideoGeneratorService.Mongo;
using Hhs.VideoGeneratorService.Providers;
using Hhs.VideoGeneratorService.Providers.Audio;
using Hhs.VideoGeneratorService.Providers.FileDownloader;
using Hhs.VideoGeneratorService.Providers.Video;
using Hhs.VideoGeneratorService.Services;

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
// 2. AUDIO PROVIDER CONFIGURATION
// ============================================================================

var audioFastProviderSettings = builder.Configuration.GetSection(AudioFastProviderSettings.SectionName)
    .Get<AudioFastProviderSettings>() ?? new AudioFastProviderSettings();
builder.Services.AddSingleton(audioFastProviderSettings);

var audioQueueProviderSettings = builder.Configuration.GetSection(AudioQueueProviderSettings.SectionName)
    .Get<AudioQueueProviderSettings>() ?? new AudioQueueProviderSettings();
builder.Services.AddSingleton(audioQueueProviderSettings);

// Register Audio Provider implementations
builder.Services.AddScoped<IAudioProvider, AudioQuickProvider>();
builder.Services.AddScoped<IAudioProvider, AudioHQProvider>();
builder.Services.AddScoped<IAudioProviderResolver, AudioProviderResolver>();

// ============================================================================
// 3. VIDEO PROVIDER CONFIGURATION
// ============================================================================

var videoFastExternalProviderSettings = builder.Configuration.GetSection(VideoFastExternalProviderSettings.SectionName)
    .Get<VideoFastExternalProviderSettings>() ?? new VideoFastExternalProviderSettings();
builder.Services.AddSingleton(videoFastExternalProviderSettings);

var videoFastInternalProviderSettings = builder.Configuration.GetSection(VideoFastInternalProviderSettings.SectionName)
    .Get<VideoFastInternalProviderSettings>() ?? new VideoFastInternalProviderSettings();
builder.Services.AddSingleton(videoFastInternalProviderSettings);

var videoQueueExternalProviderSettings = builder.Configuration.GetSection(VideoQueueExternalProviderSettings.SectionName)
    .Get<VideoQueueExternalProviderSettings>() ?? new VideoQueueExternalProviderSettings();
builder.Services.AddSingleton(videoQueueExternalProviderSettings);

var videoQueueInternalProviderSettings = builder.Configuration.GetSection(VideoQueueInternalProviderSettings.SectionName)
    .Get<VideoQueueInternalProviderSettings>() ?? new VideoQueueInternalProviderSettings();
builder.Services.AddSingleton(videoQueueInternalProviderSettings);

// Register Video Provider implementations
builder.Services.AddScoped<IVideoProvider, VideoFastExternalProvider>();
builder.Services.AddScoped<IVideoProvider, VideoFastInternalProvider>();
builder.Services.AddScoped<IVideoProvider, VideoQueueExternalProvider>();
builder.Services.AddScoped<IVideoProvider, VideoQueueInternalProvider>();
builder.Services.AddScoped<IVideoProviderResolver, VideoProviderResolver>();

// ============================================================================
// 4. CDN PROVIDER CONFIGURATION
// ============================================================================

// System CDN selection (which CDN to use based on environment)
var systemCdnSettings = builder.Configuration.GetSection("SystemCdn")
    .Get<SystemCdnSettings>() ?? new SystemCdnSettings();
builder.Services.AddSingleton(systemCdnSettings);

// Load CDN provider settings from configuration
var cdnLocalMinioSettings = builder.Configuration.GetSection("Provider:Cdn:CdnLocalMinio")
    .Get<CdnLocalMinioSettings>() ?? new CdnLocalMinioSettings();
builder.Services.AddSingleton(cdnLocalMinioSettings);

var cdnBunnySelfSettings = builder.Configuration.GetSection("Provider:Cdn:CdnBunnySelf")
    .Get<CdnBunnySelfSettings>() ?? new CdnBunnySelfSettings();
builder.Services.AddSingleton(cdnBunnySelfSettings);

var cdnBunnyS3Settings = builder.Configuration.GetSection("Provider:Cdn:CdnBunnyS3")
    .Get<CdnBunnyS3Settings>() ?? new CdnBunnyS3Settings();
builder.Services.AddSingleton(cdnBunnyS3Settings);

var cdnAbcSettings = builder.Configuration.GetSection("Provider:Cdn:CdnAbcCloudFront")
    .Get<CdnAbcCloudFrontSettings>() ?? new CdnAbcCloudFrontSettings();
builder.Services.AddSingleton(cdnAbcSettings);

// Register CDN Provider implementations
builder.Services.AddScoped<ICdnProvider, CdnLocalMinioProvider>();
builder.Services.AddScoped<ICdnProvider, CdnBunnySelfProvider>();
builder.Services.AddScoped<ICdnProvider, CdnBunnyS3Provider>();
builder.Services.AddScoped<ICdnProvider, CdnAbcCloudFrontProvider>();
builder.Services.AddScoped<ICdnProviderResolver, CdnProviderResolver>();

// ============================================================================
// 5. POLLING & RETRY CONFIGURATION
// ============================================================================

var audioPollingSettings = builder.Configuration.GetSection(AudioPollingSettings.SectionName)
    .Get<AudioPollingSettings>() ?? new AudioPollingSettings();
builder.Services.AddSingleton(audioPollingSettings);

var videoPollingSettings = builder.Configuration.GetSection(VideoPollingSettings.SectionName)
    .Get<VideoPollingSettings>() ?? new VideoPollingSettings();
builder.Services.AddSingleton(videoPollingSettings);

var videoRetrySettings = builder.Configuration.GetSection(nameof(VideoRetrySettings))
    .Get<VideoRetrySettings>() ?? new VideoRetrySettings();
builder.Services.AddSingleton(videoRetrySettings);
builder.Services.AddSingleton(_ => new RetryDelayCalculator(videoRetrySettings.DelaySeconds));

// ============================================================================
// 6. DATABASE CONFIGURATION
// ============================================================================

BsonRegisterTools.MongoConfigure();
builder.Services.AddSingleton<VideoMongoContext>();

// ============================================================================
// 7. APPLICATION SERVICES
// ============================================================================

builder.Services.AddScoped<VideoGeneratorInboxStore>();
builder.Services.AddScoped<VideoOperationAppService>();
builder.Services.AddScoped<IFileDownloader, DummyFileDownloader>();
builder.Services.AddScoped<IStorageService, DummyStorageService>();

// ============================================================================
// 8. EVENT HANDLERS
// ============================================================================
// Order matters: handlers are triggered by events from event bus

// Video Generation Handlers
builder.Services.AddScoped<VideoGenerationApprovedEtoHandler>();
builder.Services.AddScoped<VideoRequestCreatedEtoHandler>();
builder.Services.AddScoped<VideoOperationStartedEtoHandler>();

// Audio Processing Handlers
builder.Services.AddScoped<AudioProviderRequestStartedEtoHandler>();
builder.Services.AddScoped<AudioProviderCompletedEtoHandler>();
builder.Services.AddScoped<AudioFileDownloadStartedEtoHandler>();
builder.Services.AddScoped<AudioFileDownloadCompletedEtoHandler>();
builder.Services.AddScoped<AudioFileUploadCompletedEtoHandler>();

// Video Processing Handlers
builder.Services.AddScoped<VideoProviderRequestStartedEtoHandler>();
builder.Services.AddScoped<VideoProviderCompletedEtoHandler>();
builder.Services.AddScoped<VideoFileDownloadStartedEtoHandler>();
builder.Services.AddScoped<VideoFileDownloadCompletedEtoHandler>();
builder.Services.AddScoped<VideoFileUploadCompletedEtoHandler>();

// Polling Handlers (periodically check status)
builder.Services.AddScoped<AudioProviderPollingStartedEtoHandler>();
builder.Services.AddScoped<VideoProviderPollingStartedEtoHandler>();

// ============================================================================
// 9. BACKGROUND WORKERS / HOSTED SERVICES
// ============================================================================
// These are long-running services that listen to RabbitMQ queues

// Audio Processing Workers
builder.Services.AddHostedService<RabbitMqConsumerHostedService<AudioProviderPollingStartedEto, AudioProviderPollingStartedEtoHandler>>();

// Video Processing Workers
builder.Services.AddHostedService<RabbitMqConsumerHostedService<VideoProviderPollingStartedEto, VideoProviderPollingStartedEtoHandler>>();

// Video Generation Event Handlers
builder.Services.AddHostedService<RabbitMqConsumerHostedService<VideoGenerationApprovedEto, VideoGenerationApprovedEtoHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<VideoRequestCreatedEto, VideoRequestCreatedEtoHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<VideoOperationStartedEto, VideoOperationStartedEtoHandler>>();

// Audio Event Handlers
builder.Services.AddHostedService<RabbitMqConsumerHostedService<AudioProviderRequestStartedEto, AudioProviderRequestStartedEtoHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<AudioProviderCompletedEto, AudioProviderCompletedEtoHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<AudioFileDownloadStartedEto, AudioFileDownloadStartedEtoHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<AudioFileDownloadCompletedEto, AudioFileDownloadCompletedEtoHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<AudioFileUploadCompletedEto, AudioFileUploadCompletedEtoHandler>>();

// Video Event Handlers
builder.Services.AddHostedService<RabbitMqConsumerHostedService<VideoProviderRequestStartedEto, VideoProviderRequestStartedEtoHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<VideoProviderCompletedEto, VideoProviderCompletedEtoHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<VideoFileDownloadStartedEto, VideoFileDownloadStartedEtoHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<VideoFileDownloadCompletedEto, VideoFileDownloadCompletedEtoHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<VideoFileUploadCompletedEto, VideoFileUploadCompletedEtoHandler>>();

// ============================================================================
// 10. BUILD & RUN APPLICATION
// ============================================================================

var app = builder.Build();



app.Run();
