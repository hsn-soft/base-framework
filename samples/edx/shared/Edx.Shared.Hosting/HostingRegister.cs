using HsnSoft.Base.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Edx.Shared.Hosting;

/// <summary>What every Edx host has, whatever it is (a web host, an agent): the logging. Everything that is of a web host is in <c>Edx.Shared.Hosting.Http</c>.</summary>
public static class HostingRegister
{
    /// <summary>The masker of sensitive data and the console logger; the logger itself (<c>AddBaseSerilog</c>) is added by the host as it chooses its sinks.</summary>
    public static IServiceCollection AddEdxLogging(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services.AddBaseLogging();
    }
}
