using Hhs.Shared.Events;
using Hhs.Shared.RabbitMQ;
using Hhs.Shared.Configuration;
using Hhs.Shared.Retry;
using Hhs.VideoGeneratorService.Configuration;
using Hhs.VideoGeneratorService.Workers;
using Hhs.VideoGeneratorService.Entities;
using Hhs.VideoGeneratorService.Handlers;
using Hhs.VideoGeneratorService.Infrastructure;
using Hhs.VideoGeneratorService.Mongo;
using Hhs.VideoGeneratorService.Providers;
using Hhs.VideoGeneratorService.Services;
using Hhs.VideoGeneratorService.Workers;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<MongoOptions>(builder.Configuration.GetSection("MongoDb"));
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMq"));
builder.Services.Configure<ProviderEndpointsOptions>(builder.Configuration.GetSection("ProviderEndpoints"));

var audioPollingSettings = builder.Configuration.GetSection(AudioPollingSettings.SectionName)
    .Get<AudioPollingSettings>() ?? new AudioPollingSettings();
builder.Services.AddSingleton(audioPollingSettings);

var videoPollingSettings = builder.Configuration.GetSection(VideoPollingSettings.SectionName)
    .Get<VideoPollingSettings>() ?? new VideoPollingSettings();
builder.Services.AddSingleton(videoPollingSettings);

var videoRetrySettings = builder.Configuration.GetSection(VideoRetrySettings.SectionName)
    .Get<VideoRetrySettings>() ?? new VideoRetrySettings();
builder.Services.AddSingleton(videoRetrySettings);
builder.Services.AddSingleton(_ => new RetryDelayCalculator(videoRetrySettings.DelaySeconds));

BsonRegisterTools.MongoConfigure();
builder.Services.AddSingleton<VideoMongoContext>();

builder.Services.AddHttpClient();
builder.Services.AddSingleton<IEventBus, RabbitMqEventBus>();

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
                x.AudioProviderTrackId != null)
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
                    x.AudioProviderTrackId != null,
                Builders<AudioRequest>.Update
                    .Set(x => x.NextProviderPollAtUtc, DateTime.UtcNow.AddSeconds(5))
                    .Set(x => x.UpdatedAtUtc, DateTime.UtcNow),
                cancellationToken: cancellationToken);

            if (claimResult.ModifiedCount == 0)
                continue;

            await eventBus.PublishAsync(new AudioProviderPollingStartedEto
            {
                VideoRequestId = request.VideoRequestId,
                AudioRequestId = request.Id,
                ProviderKey = request.AudioProviderKey,
                ProviderTrackId = request.AudioProviderTrackId
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
                x.VideoProviderTrackId != null)
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
                    x.VideoProviderTrackId != null,
                Builders<VideoRequest>.Update
                    .Set(x => x.NextProviderPollAtUtc, DateTime.UtcNow.AddSeconds(5))
                    .Set(x => x.UpdatedAtUtc, DateTime.UtcNow),
                cancellationToken: cancellationToken);

            if (claimResult.ModifiedCount == 0)
                continue;

            await eventBus.PublishAsync(new VideoProviderPollingStartedEto
            {
                VideoRequestId = request.Id,
                ProviderKey = request.VideoProviderKey,
                ProviderTrackId = request.VideoProviderTrackId
            }, cancellationToken);
        }

        return Results.Ok(new { processed = requests.Count });
    });

app.Run();