using Edx.Integration.GibInvoice.Application.Contracts.EnvelopeDomain.Services;
using Edx.Integration.GibInvoice.Application.Options;
using Edx.Integration.GibInvoice.Application.Services;
using HsnSoft.Base.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Edx.Integration.GibInvoice.Application;

public static class ApplicationServiceCollectionExtensions
{
    /// <summary>
    /// Registers the application services of the integration service: the center it talks to (<see cref="GibCenterOptions"/>, section <c>Center</c>), where it keeps its files (<see cref="IntegrationStorageOptions"/>, section <c>Storage</c>), the outbound and the inbound work and the
    /// reads of the envelopes. The repositories come from the persistence project.
    /// </summary>
    public static IServiceCollection AddIntegrationApplication(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<GibCenterOptions>(configuration.GetSection(GibCenterOptions.SectionName));
        services.Configure<IntegrationStorageOptions>(configuration.GetSection(IntegrationStorageOptions.SectionName));

        // the most a client may ask for in a list (the Paging section)
        services.AddBasePaging(configuration);

        services.AddSingleton(TimeProvider.System);
        services.AddScoped<EnvelopeStorage>();
        services.AddScoped<UserDirectory>();
        services.AddScoped<OutboundEnvelopeMaker>();
        services.AddScoped<OutboundStatusService>();
        services.AddScoped<OutboundSender>();
        services.AddScoped<OutboundStatusPoller>();
        services.AddScoped<InboundStatusPoller>();
        services.AddScoped<InboundReceiver>();
        services.AddScoped<InboundProcessor>();
        services.AddScoped<IEnvelopeAppService, EnvelopeAppService>();
        return services;
    }

    /// <summary>
    /// Registers only what the WCF host of the post box needs: where the files are kept and the receiver that queues what the center delivers (it needs no event bus: the checks, the events and the answers are the work of the other host).
    /// </summary>
    public static IServiceCollection AddIntegrationPostBox(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<IntegrationStorageOptions>(configuration.GetSection(IntegrationStorageOptions.SectionName));
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<EnvelopeStorage>();
        services.AddScoped<InboundReceiver>();
        return services;
    }
}
