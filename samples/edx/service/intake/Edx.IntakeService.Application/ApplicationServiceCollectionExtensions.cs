using Edx.IntakeService.Application.Contracts.IntakeDomain.Services;
using Edx.IntakeService.Application.Services;
using Edx.IntakeService.Application.Storage;
using Edx.IntakeService.Application.Uploads;
using HsnSoft.Base.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Edx.IntakeService.Application;

public static class ApplicationServiceCollectionExtensions
{
    /// <summary>
    /// Registers the application services of the intake service. The repositories come from the persistence project, the blob store (<c>IBlobStore</c>) and the event bus from the host. Where the files are kept is the
    /// <c>Storage</c> section (<see cref="StorageOptions"/>), what is accepted the <c>Intake</c> section (<see cref="IntakeOptions"/>).
    /// </summary>
    public static IServiceCollection AddIntakeApplication(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));
        services.Configure<IntakeOptions>(configuration.GetSection(IntakeOptions.SectionName));

        // the most a client may ask for in a list (the Paging section)
        services.AddBasePaging(configuration);

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<UploadInspector>();
        services.AddScoped<StorageLayout>();
        services.AddScoped<IIntakeAppService, IntakeAppService>();
        services.AddScoped<QueueUpdater>();
        return services;
    }
}
