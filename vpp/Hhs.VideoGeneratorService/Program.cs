using Hhs.Shared.Events;
using Hhs.Shared.RabbitMQ;
using Hhs.VideoGeneratorService.Handlers;
using Hhs.VideoGeneratorService.Infrastructure;
using Hhs.VideoGeneratorService.Mongo;
using Hhs.VideoGeneratorService.Providers;
using Hhs.VideoGeneratorService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<MongoOptions>(builder.Configuration.GetSection("MongoDb"));
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMq"));

BsonRegisterTools.MongoConfigure();
builder.Services.AddSingleton<VideoMongoContext>();
builder.Services.AddSingleton<IEventBus, RabbitMqEventBus>();

builder.Services.AddScoped<VideoGeneratorInboxStore>();
builder.Services.AddScoped<VideoOperationAppService>();

builder.Services.AddScoped<IAudioProvider, DummyAudioProvider>();
builder.Services.AddScoped<IVideoProvider, DummyVideoProvider>();
builder.Services.AddScoped<IFileDownloader, DummyFileDownloader>();
builder.Services.AddScoped<IStorageService, DummyStorageService>();

builder.Services.AddScoped<VideoGenerationApprovedEventHandler>();
builder.Services.AddScoped<VideoRequestCreatedEventHandler>();
builder.Services.AddScoped<VideoOperationStartedEventHandler>();
builder.Services.AddScoped<AudioProviderRequestStartedEventHandler>();
builder.Services.AddScoped<AudioProviderCompletedEventHandler>();
builder.Services.AddScoped<AudioFileDownloadStartedEventHandler>();
builder.Services.AddScoped<AudioFileUploadStartedEventHandler>();
builder.Services.AddScoped<AudioFileUploadCompletedEventHandler>();
builder.Services.AddScoped<VideoProviderRequestStartedEventHandler>();
builder.Services.AddScoped<VideoProviderCompletedEventHandler>();
builder.Services.AddScoped<VideoFileDownloadStartedEventHandler>();
builder.Services.AddScoped<VideoFileUploadStartedEventHandler>();

builder.Services.AddHostedService<RabbitMqConsumerHostedService<VideoGenerationApprovedEvent, VideoGenerationApprovedEventHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<VideoRequestCreatedEvent, VideoRequestCreatedEventHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<VideoOperationStartedEvent, VideoOperationStartedEventHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<AudioProviderRequestStartedEvent, AudioProviderRequestStartedEventHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<AudioProviderCompletedEvent, AudioProviderCompletedEventHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<AudioFileDownloadStartedEvent, AudioFileDownloadStartedEventHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<AudioFileUploadStartedEvent, AudioFileUploadStartedEventHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<AudioFileUploadCompletedEvent, AudioFileUploadCompletedEventHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<VideoProviderRequestStartedEvent, VideoProviderRequestStartedEventHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<VideoProviderCompletedEvent, VideoProviderCompletedEventHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<VideoFileDownloadStartedEvent, VideoFileDownloadStartedEventHandler>>();
builder.Services.AddHostedService<RabbitMqConsumerHostedService<VideoFileUploadStartedEvent, VideoFileUploadStartedEventHandler>>();

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

app.Run();