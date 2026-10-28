using CoreWCF;
using CoreWCF.Channels;
using CoreWCF.Configuration;
using CoreWCF.Description;
using Edx.IntakeService.Application.Storage;

namespace Edx.IntakeService.Wcf.Host;

/// <summary>The WCF endpoint of the intake service: an entry point and nothing more, the application service it calls does not know WCF (the framework has no package for it: ADR-0079).</summary>
public static class IntakeWcfRegister
{
    // a message is the file in base64, a third bigger, and the envelope around it
    private const long EnvelopeAllowanceBytes = 65_536;

    public static IServiceCollection AddIntakeWcf(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHttpContextAccessor();

        // CoreWCF makes the service from the container: it has to be there
        services.AddTransient<IntakeWcfService>();
        services.AddServiceModelServices();
        services.AddServiceModelMetadata();
        return services;
    }

    /// <summary>Maps the WCF endpoint at <see cref="IntakeWcfNames.Address"/> with its WSDL (<c>?wsdl</c>) for the customers who generate a client from it.</summary>
    public static WebApplication UseIntakeWcf(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        long messageLimit = checked((long)((IntakeOptions.MaxUploadBytesOf(app.Configuration) * 4 / 3) + EnvelopeAllowanceBytes));
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
}
