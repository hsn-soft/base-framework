using Hhs.Shared.Events;
using Hhs.Shared.RabbitMQ;
using Hhs.Shared.Configuration.Providers;
using Hhs.Shared.Retry;
using Hhs.VideoGeneratorService.Configuration;
using Hhs.VideoGeneratorService.Configuration.Providers.Audio;
using Hhs.VideoGeneratorService.Configuration.Providers.Video;
using Hhs.VideoGeneratorService.Configuration.Providers.Cdn;
using Hhs.VideoGeneratorService.Providers.Cdn;
using Hhs.VideoGeneratorService.Entities;
using Hhs.VideoGeneratorService.Handlers;
using Hhs.VideoGeneratorService.Infrastructure;
using Hhs.VideoGeneratorService.Mongo;
using Hhs.VideoGeneratorService.Providers;
using Hhs.VideoGeneratorService.Providers.Audio;
using Hhs.VideoGeneratorService.Providers.FileDownloader;
using Hhs.VideoGeneratorService.Providers.Video;
using Hhs.VideoGeneratorService.Services;
using Hhs.VideoGeneratorService.Workers;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<MongoOptions>(builder.Configuration.GetSection("MongoDb"));
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMq"));

var audioFastProviderSettings = builder.Configuration.GetSection(AudioFastProviderSettings.SectionName)
    .Get<AudioFastProviderSettings>() ?? new AudioFastProviderSettings();
builder.Services.AddSingleton(audioFastProviderSettings);

var audioQueueProviderSettings = builder.Configuration.GetSection(AudioQueueProviderSettings.SectionName)
    .Get<AudioQueueProviderSettings>() ?? new AudioQueueProviderSettings();
builder.Services.AddSingleton(audioQueueProviderSettings);

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

var storageProviderSettings = builder.Configuration.GetSection(StorageProviderSettings.SectionName)
    .Get<StorageProviderSettings>() ?? new StorageProviderSettings();
builder.Services.AddSingleton(storageProviderSettings);

// CDN Provider Configuration (environment-based)
var cdnProviderConfig = builder.Configuration.GetSection(CdnProviderConfiguration.SectionName)
    .Get<CdnProviderConfiguration>() ?? new CdnProviderConfiguration();
builder.Services.AddSingleton(cdnProviderConfig);

// CDN Provider Resolver
builder.Services.AddSingleton<ICdnProviderResolver>(sp =>
{
    var cdnProviders = new Dictionary<string, CdnProviderSettingsBase>(StringComparer.OrdinalIgnoreCase);
    var cdnSection = builder.Configuration.GetSection("Provider:Cdn");

    if (cdnSection.Exists())
    {
        foreach (var child in cdnSection.GetChildren())
        {
            var key = child.Key;
            CdnProviderSettingsBase? settings = null;

            // Try concrete implementations in order
            if (key.Equals("CdnLocalMinio", StringComparison.OrdinalIgnoreCase))
                settings = child.Get<LocalMinioCdnSettings>();
            else if (key.Equals("CdnBunnySelf", StringComparison.OrdinalIgnoreCase))
                settings = child.Get<BunnyCdnSettings>();
            else if (key.Equals("CdnBunnyS3", StringComparison.OrdinalIgnoreCase))
                settings = child.Get<CdnBunnyS3Settings>();
            else if (key.StartsWith("CdnAbc", StringComparison.OrdinalIgnoreCase) ||
                     key.StartsWith("CdnCloudflare", StringComparison.OrdinalIgnoreCase))
                settings = child.Get<CloudflareCdnSettings>();
            else if (key.StartsWith("CdnAzure", StringComparison.OrdinalIgnoreCase))
                settings = child.Get<AzureCdnSettings>();
            else if (key.StartsWith("CdnAws", StringComparison.OrdinalIgnoreCase))
                settings = child.Get<AwsCloudFrontCdnSettings>();

            if (settings != null)
            {
                cdnProviders[key] = settings;
            }
        }
    }

    return new CdnProviderResolver(cdnProviders);
});

// CDN Storage Provider Factory
builder.Services.AddSingleton<CdnProviderFactory>();

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

BsonRegisterTools.MongoConfigure();
builder.Services.AddSingleton<VideoMongoContext>();

builder.Services.AddHttpClient();
builder.Services.AddSingleton<IEventBus, RabbitMqEventBus>();

// Storage Service (CDN + Storage integration)
builder.Services.AddScoped<IStorageService, DummyStorageService>();

builder.Services.AddScoped<VideoGeneratorInboxStore>();
builder.Services.AddScoped<VideoOperationAppService>();

builder.Services.AddScoped<IAudioProvider, AudioQuickProvider>();
builder.Services.AddScoped<IAudioProvider, AudioHQProvider>();
builder.Services.AddScoped<IAudioProviderResolver, AudioProviderResolver>();

builder.Services.AddScoped<IVideoProvider, VideoFastExternalProvider>();
builder.Services.AddScoped<IVideoProvider, VideoFastInternalProvider>();
builder.Services.AddScoped<IVideoProvider, VideoQueueExternalProvider>();
builder.Services.AddScoped<IVideoProvider, VideoQueueInternalProvider>();
builder.Services.AddScoped<IVideoProviderResolver, VideoProviderResolver>();
builder.Services.AddScoped<IFileDownloader, DummyFileDownloader>();
builder.Services.AddScoped<IStorageService, DummyStorageService>();

builder.Services.AddScoped<VideoGenerationApprovedEtoHandler>();
builder.Services.AddScoped<VideoRequestCreatedEtoHandler>();
builder.Services.AddScoped<VideoOperationStartedEtoHandler>();
builder.Services.AddScoped<AudioProviderRequestStartedEtoHandler>();
builder.Services.AddScoped<AudioProviderCompletedEtoHandler>();
builder.Services.AddScoped<AudioFileDownloadStartedEtoHandler>();
builder.Services.AddScoped<AudioFileDownloadCompletedEtoHandler>();
builder.Services.AddScoped<AudioFileUploadStartedEtoHandler>();
builder.Services.AddScoped<AudioFileUploadCompletedEtoHandler>();
builder.Services.AddScoped<VideoProviderRequestStartedEtoHandler>();
builder.Services.AddScoped<VideoProviderCompletedEtoHandler>();
builder.Services.AddScoped<VideoFileDownloadStartedEtoHandler>();
builder.Services.AddScoped<VideoFileUploadStartedEtoHandler>();

builder.Services.AddScoped<AudioProviderPollingStartedEtoHandler>();
builder.Services.AddScoped<VideoProviderPollingStartedEtoHandler>();

builder.Services.AddHostedService<RabbitMqConsumerHostedService<AudioProviderPollingStartedEto, AudioProviderPollingStartedEtoHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<VideoProviderPollingStartedEto, VideoProviderPollingStartedEtoHandler>>();

builder.Services.AddHostedService<RabbitMqConsumerHostedService<VideoGenerationApprovedEto, VideoGenerationApprovedEtoHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<VideoRequestCreatedEto, VideoRequestCreatedEtoHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<VideoOperationStartedEto, VideoOperationStartedEtoHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<AudioProviderRequestStartedEto, AudioProviderRequestStartedEtoHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<AudioProviderCompletedEto, AudioProviderCompletedEtoHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<AudioFileDownloadStartedEto, AudioFileDownloadStartedEtoHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<AudioFileDownloadCompletedEto, AudioFileDownloadCompletedEtoHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<AudioFileUploadStartedEto, AudioFileUploadStartedEtoHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<AudioFileUploadCompletedEto, AudioFileUploadCompletedEtoHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<VideoProviderRequestStartedEto, VideoProviderRequestStartedEtoHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<VideoProviderCompletedEto, VideoProviderCompletedEtoHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<VideoFileDownloadStartedEto, VideoFileDownloadStartedEtoHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<VideoFileUploadStartedEto, VideoFileUploadStartedEtoHandler>>();

builder.Services.AddScoped<AudioProviderPollingAppService>();
builder.Services.AddHostedService<AudioProviderPollingWorker>();

builder.Services.AddScoped<VideoProviderPollingAppService>();
builder.Services.AddHostedService<VideoProviderPollingWorker>();

builder.Services.AddScoped<VideoRetryAppService>();
builder.Services.AddHostedService<VideoRetryWorker>();

var app = builder.Build();

app.MapPost("/admin/audio-requests/{audioRequestId:guid}/upload/complete-manual",
    async (
        Guid audioRequestId,
        ManualAudioUploadInput input,
        VideoOperationAppService appService,
        CancellationToken cancellationToken) =>
    {
        await appService.CompleteAudioUploadManuallyAsync(audioRequestId, input, cancellationToken);
        return Results.Ok();
    });


app.MapPost("/scheduler/audio-polling",
    async (
        VideoMongoContext mongoContext,
        IEventBus eventBus,
        IAudioProviderResolver audioProviderResolver,
        CancellationToken cancellationToken) =>
    {
        var now = DateTime.UtcNow;
        var requests = await mongoContext.AudioRequests
            .Find(x =>
                x.Status == "AUDIO_PROVIDER_POLLING" &&
                x.NextProviderPollAtUtc != null &&
                x.NextProviderPollAtUtc <= now &&
                x.AudioProviderTrackingId != null)
            .Limit(10)
            .ToListAsync(cancellationToken);

        foreach (var request in requests)
        {
            var claimResult = await mongoContext.AudioRequests.UpdateOneAsync(
                x =>
                    x.Id == request.Id &&
                    x.Status == "AUDIO_PROVIDER_POLLING" &&
                    x.NextProviderPollAtUtc != null &&
                    x.NextProviderPollAtUtc <= now &&
                    x.AudioProviderTrackingId != null,
                Builders<AudioRequest>.Update
                    .Set(x => x.NextProviderPollAtUtc, DateTime.UtcNow.AddSeconds(audioPollingSettings.ErrorRescheduleDelaySeconds))
                    .Set(x => x.UpdatedAtUtc, DateTime.UtcNow),
                cancellationToken: cancellationToken);

            if (claimResult.ModifiedCount == 0)
                continue;

            await eventBus.PublishAsync(new AudioProviderPollingStartedEto
            {
                VideoRequestId = request.VideoRequestId,
                AudioRequestId = request.Id,
                ProviderKey = request.AudioProviderKey,
                ProviderTrackId = request.AudioProviderTrackingId
            }, cancellationToken);
        }

        return Results.Ok(new { processed = requests.Count });
    });

app.MapPost("/scheduler/video-polling",
    async (
        VideoMongoContext mongoContext,
        IEventBus eventBus,
        CancellationToken cancellationToken) =>
    {
        var now = DateTime.UtcNow;
        var requests = await mongoContext.VideoRequests
            .Find(x =>
                x.Status == "VIDEO_PROVIDER_POLLING" &&
                x.NextProviderPollAtUtc != null &&
                x.NextProviderPollAtUtc <= now &&
                x.VideoProviderTrackingId != null)
            .Limit(10)
            .ToListAsync(cancellationToken);

        foreach (var request in requests)
        {
            var claimResult = await mongoContext.VideoRequests.UpdateOneAsync(
                x =>
                    x.Id == request.Id &&
                    x.Status == "VIDEO_PROVIDER_POLLING" &&
                    x.NextProviderPollAtUtc != null &&
                    x.NextProviderPollAtUtc <= now &&
                    x.VideoProviderTrackingId != null,
                Builders<VideoRequest>.Update
                    .Set(x => x.NextProviderPollAtUtc, DateTime.UtcNow.AddSeconds(videoPollingSettings.ErrorRescheduleDelaySeconds))
                    .Set(x => x.UpdatedAtUtc, DateTime.UtcNow),
                cancellationToken: cancellationToken);

            if (claimResult.ModifiedCount == 0)
                continue;

            await eventBus.PublishAsync(new VideoProviderPollingStartedEto
            {
                VideoRequestId = request.Id,
                ProviderKey = request.VideoProviderKey,
                ProviderTrackId = request.VideoProviderTrackingId
            }, cancellationToken);
        }

        return Results.Ok(new { processed = requests.Count });
    });

app.Run();