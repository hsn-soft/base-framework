using CoreWCF;
using CoreWCF.Channels;
using CoreWCF.Configuration;
using CoreWCF.Description;
using Edx.IntakeService.Application.Storage;
using Edx.IntakeService.Http.Host.GrpcApi;
using Edx.IntakeService.Http.Host.WcfApi;

namespace Edx.IntakeService.Http.Host;

/// <summary>
/// The protocols the intake service speaks besides REST: gRPC and WCF. Both are entry points of this host and nothing more: the application service they call does not know them, and the rest of the system never sees them
/// (the framework has no package for either: ADR-0079).
/// </summary>
public static class IntakeProtocolsRegister
{
    // a message is the file in base64, a third bigger, and the envelope around it
    private const long EnvelopeAllowanceBytes = 65_536;

    public static IServiceCollection AddIntakeProtocols(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddGrpc(options =>
        {
            options.Interceptors.Add<GrpcExceptionInterceptor>();

            // the size of one message; an archive is sent in chunks, so a message is small
            options.MaxReceiveMessageSize = 4 * 1024 * 1024;
        });
        services.AddHttpContextAccessor();

        // CoreWCF makes the service from the container: it has to be there
        services.AddTransient<IntakeWcfService>();
        services.AddServiceModelServices();
        services.AddServiceModelMetadata();
        return services;
    }

    /// <summary>Maps the gRPC service, and the WCF endpoint at <see cref="IntakeWcfNames.Address"/> with its WSDL (<c>?wsdl</c>) for the customers who generate a client from it.</summary>
    public static WebApplication UseIntakeProtocols(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGrpcService<IntakeGrpcService>();

        long messageLimit = checked((long)((MaxUploadBytes(app.Configuration) * 4 / 3) + EnvelopeAllowanceBytes));
        BasicHttpBinding binding = new(BasicHttpSecurityMode.None)
        {
            MaxReceivedMessageSize = messageLimit,
            MaxBufferSize = (int)Math.Min(messageLimit, int.MaxValue),
        };
        binding.ReaderQuotas.MaxArrayLength = int.MaxValue;
        binding.ReaderQuotas.MaxStringContentLength = int.MaxValue;

        // an unknown failure tells a client nothing of its cause (the service turns it into a fault with a trace id); Wcf:IncludeExceptionDetails shows it, for a developer who looks at what WCF itself refused
        bool details = app.Configuration.GetValue<bool>("Wcf:IncludeExceptionDetails");
        app.UseServiceModel(builder => builder
            .AddService<IntakeWcfService>()
            .AddServiceEndpoint<IntakeWcfService, IIntakeWcfService>(binding, IntakeWcfNames.Address)
            .ConfigureServiceHostBase<IntakeWcfService>(host => host.Description.Behaviors.Find<ServiceDebugBehavior>()!.IncludeExceptionDetailInFaults = details));
        app.Services.GetRequiredService<ServiceMetadataBehavior>().HttpGetEnabled = true;
        return app;
    }

    /// <summary>The most a file may weigh (<c>Intake:MaxUploadBytes</c>): what Kestrel, the form, WCF and the application service all allow.</summary>
    public static long MaxUploadBytes(IConfiguration configuration)
        => configuration.GetValue($"{IntakeOptions.SectionName}:{nameof(IntakeOptions.MaxUploadBytes)}", new IntakeOptions().MaxUploadBytes);
}
