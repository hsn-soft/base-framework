using Hhs.Shared.Contracts.Cache;
using Hhs.Shared.Helper.Retry;
using Hhs.VideoGeneratorService.Application.Infrastructure;
using Hhs.VideoGeneratorService.Application.Providers;
using Hhs.VideoGeneratorService.Application.Providers.Audio;
using Hhs.VideoGeneratorService.Application.Providers.Cdn;
using Hhs.VideoGeneratorService.Application.Providers.FileDownloader;
using Hhs.VideoGeneratorService.Application.Providers.Video;
using Hhs.VideoGeneratorService.Application.Services;
using Hhs.VideoGeneratorService.Domain.Configuration;
using Hhs.VideoGeneratorService.Domain.Configuration.Providers.Audio;
using Hhs.VideoGeneratorService.Domain.Configuration.Providers.Cdn;
using Hhs.VideoGeneratorService.Domain.Configuration.Providers.Video;
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
        services.AddSingleton<IServicePermissionProvider, ApplicationPermissionProvider>();

        // Must be Scoped or Transient => Cannot consume any scoped service
        services.AddScoped<ApplicationEventInboxMessageManager>();
        services.AddScoped<VideoOperationAppService>();
        services.AddScoped<IRemoteFileDownloader, RemoteFileDownloader>();

        // ============================================================================
        // 2. AUDIO PROVIDER CONFIGURATION
        // ============================================================================

        var audioFastProviderSettings = configuration.GetSection(AudioFastProviderSettings.SectionName)
            .Get<AudioFastProviderSettings>() ?? new AudioFastProviderSettings();
        services.AddSingleton(audioFastProviderSettings);

        var audioQueueProviderSettings = configuration.GetSection(AudioQueueProviderSettings.SectionName)
            .Get<AudioQueueProviderSettings>() ?? new AudioQueueProviderSettings();
        services.AddSingleton(audioQueueProviderSettings);

        // Register Audio Provider implementations
        services.AddScoped<IAudioProvider, AudioQuickProvider>();
        services.AddScoped<IAudioProvider, AudioHQProvider>();
        services.AddScoped<IAudioProviderResolver, AudioProviderResolver>();

        // ============================================================================
        // 3. VIDEO PROVIDER CONFIGURATION
        // ============================================================================

        var videoQueueExternalProviderSettings = configuration.GetSection(VideoQueueExternalProviderSettings.SectionName)
            .Get<VideoQueueExternalProviderSettings>() ?? new VideoQueueExternalProviderSettings();
        services.AddSingleton(videoQueueExternalProviderSettings);

        var videoQueueInternalProviderSettings = configuration.GetSection(VideoQueueInternalProviderSettings.SectionName)
            .Get<VideoQueueInternalProviderSettings>() ?? new VideoQueueInternalProviderSettings();
        services.AddSingleton(videoQueueInternalProviderSettings);

        // Register Video Provider implementations
        services.AddScoped<IVideoProvider, VideoQueueExternalProvider>();
        services.AddScoped<IVideoProvider, VideoQueueInternalProvider>();
        services.AddScoped<IVideoProviderResolver, VideoProviderResolver>();

        // ============================================================================
        // 4. CDN PROVIDER CONFIGURATION
        // ============================================================================

        // System CDN selection (which CDN to use based on environment)
        var systemCdnSettings = configuration.GetSection("SystemCdn")
            .Get<SystemCdnSettings>() ?? new SystemCdnSettings();
        services.AddSingleton(systemCdnSettings);

        // Load CDN provider settings from configuration
        var cdnLocalMinioSettings = configuration.GetSection("Provider:Cdn:CdnLocalMinio")
            .Get<CdnLocalMinioSettings>() ?? new CdnLocalMinioSettings();
        services.AddSingleton(cdnLocalMinioSettings);

        var cdnBunnySelfSettings = configuration.GetSection("Provider:Cdn:CdnBunnySelf")
            .Get<CdnBunnySelfSettings>() ?? new CdnBunnySelfSettings();
        services.AddSingleton(cdnBunnySelfSettings);

        var cdnBunnyS3Settings = configuration.GetSection("Provider:Cdn:CdnBunnyS3")
            .Get<CdnBunnyS3Settings>() ?? new CdnBunnyS3Settings();
        services.AddSingleton(cdnBunnyS3Settings);


        // Register CDN Provider implementations
        services.AddScoped<ICdnProvider, CdnLocalMinioProvider>();
        services.AddScoped<ICdnProvider, CdnBunnySelfProvider>();
        services.AddScoped<ICdnProvider, CdnBunnyS3Provider>();
        services.AddScoped<ICdnProviderResolver, CdnProviderResolver>();

        // ============================================================================
        // 5. POLLING & RETRY CONFIGURATION
        // ============================================================================

        var audioPollingSettings = configuration.GetSection(AudioPollingSettings.SectionName)
            .Get<AudioPollingSettings>() ?? new AudioPollingSettings();
        services.AddSingleton(audioPollingSettings);

        var videoPollingSettings = configuration.GetSection(VideoPollingSettings.SectionName)
            .Get<VideoPollingSettings>() ?? new VideoPollingSettings();
        services.AddSingleton(videoPollingSettings);

        var videoRetrySettings = configuration.GetSection(nameof(VideoRetrySettings))
            .Get<VideoRetrySettings>() ?? new VideoRetrySettings();
        services.AddSingleton(videoRetrySettings);
        services.AddSingleton(_ => new RetryDelayCalculator(videoRetrySettings.DelaySeconds));

        return services;
    }
}