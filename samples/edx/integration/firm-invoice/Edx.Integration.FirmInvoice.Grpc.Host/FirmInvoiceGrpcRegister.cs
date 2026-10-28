namespace Edx.Integration.FirmInvoice.Grpc.Host;

/// <summary>The gRPC service of the firm invoice integration: an entry point and nothing more, the application service it calls does not know gRPC (the framework has no package for it: ADR-0079).</summary>
public static class FirmInvoiceGrpcRegister
{
    public static IServiceCollection AddFirmInvoiceGrpc(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddGrpc(options =>
        {
            options.Interceptors.Add<GrpcExceptionInterceptor>();

            // the size of one message; an archive is sent in chunks, so a message is small
            options.MaxReceiveMessageSize = 4 * 1024 * 1024;
        });
        return services;
    }

    public static WebApplication MapFirmInvoiceGrpc(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGrpcService<FirmInvoiceGrpcService>();
        return app;
    }
}
