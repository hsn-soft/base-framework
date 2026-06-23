using Hhs.Shared.Contracts.Cache;
using Hhs.VideoGeneratorService.Application.Contracts;
using Hhs.VideoGeneratorService.Application.Contracts.DashboardDomain;
using Hhs.VideoGeneratorService.Application.Contracts.JobDomain;
using Hhs.VideoGeneratorService.Application.Contracts.Providers;
using Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.Audio.ElevenLabs;
using Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.Storage.Bunny;
using Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.VideoGenerate.Colossyan;
using Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.VideoGenerate.Creatomoate;
using Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.VideoGenerate.Did;
using Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.VideoGenerate.HeyGen;
using Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.VideoGenerate.Yepic;
using Hhs.VideoGeneratorService.Application.Contracts.VideoDomain.Interfaces;
using Hhs.VideoGeneratorService.Application.Providers;
using Hhs.VideoGeneratorService.Application.Services;
using Hhs.VideoGeneratorService.Domain.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hhs.VideoGeneratorService.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddServiceApplicationConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAutoMapper(typeof(ApplicationAutoMapperProfile));

        services.Configure<VideoRequestQuerySettings>(configuration.GetSection(nameof(VideoRequestQuerySettings)));
        services.Configure<VideoGenerationSettings>(configuration.GetSection(nameof(VideoGenerationSettings)));
        services.Configure<ColossyanAiSettings>(configuration.GetSection(nameof(ColossyanAiSettings)));
        services.Configure<YepicAiSettings>(configuration.GetSection(nameof(YepicAiSettings)));
        services.Configure<DidAiSettings>(configuration.GetSection(nameof(DidAiSettings)));
        services.Configure<HeyGenSettings>(configuration.GetSection(nameof(HeyGenSettings)));
        services.Configure<CreatomateSettings>(configuration.GetSection(nameof(CreatomateSettings)));
        services.Configure<BunnyCdnSelfStorageSettings>(configuration.GetSection(nameof(BunnyCdnSelfStorageSettings)));
        services.Configure<BunnyCdnS3StorageSettings>(configuration.GetSection(nameof(BunnyCdnS3StorageSettings)));
        services.Configure<ElevenLabsSettings>(configuration.GetSection(nameof(ElevenLabsSettings)));

        services.AddSingleton<IServicePermissionProvider, ApplicationPermissionProvider>();

        // Must be Scoped or Transient => Cannot consume any scoped service
        services.AddScoped<IColossyanAiVideoGenerationProvider, ColossyanAiVideoGenerationProvider>();
        services.AddScoped<IYepicAiVideoGenerationProvider, YepicAiVideoGenerationProvider>();
        services.AddScoped<IDidAiVideoGenerationProvider, DidAiVideoGenerationProvider>();
        services.AddScoped<IHeyGenVideoGenerationProvider, HeyGenVideoGenerationProvider>();
        services.AddScoped<ICreatomateVideoGenerationProvider, CreatomateVideoGenerationProvider>();
        services.AddScoped<IBunnyCdnSelfVideoStorageProvider, BunnyCdnSelfVideoStorageProvider>();
        services.AddScoped<IBunnyCdnS3VideoStorageProvider, BunnyCdnBlackBlazeVideoStorageProvider>();
        services.AddScoped<IElevenLabsAudioProvider, ElevenLabsAudioProvider>();

        services.AddScoped<IDashboardAppService, DashboardAppService>();
        services.AddScoped<IJobAppService, JobAppService>();
        services.AddScoped<IEventManagerAppService, EventManagerAppService>();
        services.AddScoped<IVideoOperationsAppService, VideoOperationsAppService>();
        return services;
    }
}