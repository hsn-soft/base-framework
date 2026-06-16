using Hhs.Shared.Events;
using Hhs.Shared.RabbitMQ;
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

BsonRegisterTools.MongoConfigure();
builder.Services.AddSingleton<VideoMongoContext>();

builder.Services.AddHttpClient();
builder.Services.AddSingleton<IEventBus, RabbitMqEventBus>();

builder.Services.AddScoped<VideoGeneratorInboxStore>();
builder.Services.AddScoped<VideoOperationAppService>();

builder.Services.AddScoped<IAudioProvider, AudioQuickProvider>();
builder.Services.AddScoped<IAudioProvider, AudioHQProvider>();
builder.Services.AddScoped<IAudioProviderResolver, AudioProviderResolver>();

builder.Services.AddScoped<IVideoProvider, VideoFastProvider>();
builder.Services.AddScoped<IVideoProvider, VideoSyncProvider>();
builder.Services.AddScoped<IVideoProvider, VideoCloudProvider>();
builder.Services.AddScoped<IVideoProvider, VideoProProvider>();
builder.Services.AddScoped<IVideoProviderResolver, VideoProviderResolver>();
builder.Services.AddScoped<IFileDownloader, DummyFileDownloader>();
builder.Services.AddScoped<IStorageService, DummyStorageService>();

builder.Services.AddScoped<VideoGenerationApprovedEventHandler>();
builder.Services.AddScoped<VideoRequestCreatedEventHandler>();
builder.Services.AddScoped<VideoOperationStartedEventHandler>();
builder.Services.AddScoped<AudioProviderRequestStartedEventHandler>();
builder.Services.AddScoped<AudioProviderCompletedEventHandler>();
builder.Services.AddScoped<AudioFileDownloadStartedEventHandler>();
builder.Services.AddScoped<AudioFileDownloadCompletedEventHandler>();
builder.Services.AddScoped<AudioFileUploadStartedEventHandler>();
builder.Services.AddScoped<AudioFileUploadCompletedEventHandler>();
builder.Services.AddScoped<VideoProviderRequestStartedEventHandler>();
builder.Services.AddScoped<VideoProviderCompletedEventHandler>();
builder.Services.AddScoped<VideoFileDownloadStartedEventHandler>();
builder.Services.AddScoped<VideoFileUploadStartedEventHandler>();

builder.Services.AddScoped<AudioProviderPollingStartedEventHandler>();
builder.Services.AddScoped<VideoProviderPollingStartedEventHandler>();

builder.Services.AddHostedService<RabbitMqConsumerHostedService<AudioProviderPollingStartedEvent, AudioProviderPollingStartedEventHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<VideoProviderPollingStartedEvent, VideoProviderPollingStartedEventHandler>>();

builder.Services.AddHostedService<RabbitMqConsumerHostedService<VideoGenerationApprovedEvent, VideoGenerationApprovedEventHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<VideoRequestCreatedEvent, VideoRequestCreatedEventHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<VideoOperationStartedEvent, VideoOperationStartedEventHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<AudioProviderRequestStartedEvent, AudioProviderRequestStartedEventHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<AudioProviderCompletedEvent, AudioProviderCompletedEventHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<AudioFileDownloadStartedEvent, AudioFileDownloadStartedEventHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<AudioFileDownloadCompletedEvent, AudioFileDownloadCompletedEventHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<AudioFileUploadStartedEvent, AudioFileUploadStartedEventHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<AudioFileUploadCompletedEvent, AudioFileUploadCompletedEventHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<VideoProviderRequestStartedEvent, VideoProviderRequestStartedEventHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<VideoProviderCompletedEvent, VideoProviderCompletedEventHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<VideoFileDownloadStartedEvent, VideoFileDownloadStartedEventHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<VideoFileUploadStartedEvent, VideoFileUploadStartedEventHandler>>();

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

            await eventBus.PublishAsync(new AudioProviderPollingStartedEvent
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

            await eventBus.PublishAsync(new VideoProviderPollingStartedEvent
            {
                VideoRequestId = request.Id,
                ProviderKey = request.VideoProviderKey,
                ProviderTrackId = request.VideoProviderTrackId
            }, cancellationToken);
        }

        return Results.Ok(new { processed = requests.Count });
    });

app.Run();