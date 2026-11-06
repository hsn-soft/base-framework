using System;
using System.Reflection;
using Microsoft.Extensions.Hosting;

namespace HsnSoft.Base.AspNetCore;

/// <summary>Who this process is: the service, the instance, where it runs and what version. It is made once at the start, kept in the container (<c>AddBaseHosting</c>), shown at <c>GET /</c> and is what a log or a trace can say about the process.</summary>
/// <param name="Name">The service (<c>Service:Name</c>, else the name of the application).</param>
/// <param name="Id">The instance: <c>APP_ID</c>, else <c>POD_NAME</c>, else <c>HOSTNAME</c>, else a new id; two pods of a service have two ids.</param>
/// <param name="Environment">The name of the environment (<c>production</c>, <c>uat</c>, ...).</param>
/// <param name="Version">The version of the build: <c>BUILD_NUMBER</c> when the pipeline sets it, else the version of the assembly.</param>
/// <param name="StartedAt">When the process started (UTC).</param>
public sealed record AppInfo(string Name, string Id, string Environment, string Version, DateTimeOffset StartedAt)
{
    /// <summary>Makes the information from the settings, the variables of the process and the host environment.</summary>
    /// <param name="environment">The host environment.</param>
    /// <param name="serviceName"><c>Service:Name</c> when it is set.</param>
    /// <param name="variable">Reads an environment variable (the process's own by default; tests give theirs).</param>
    public static AppInfo Create(IHostEnvironment environment, string? serviceName = null, Func<string, string?>? variable = null)
    {
        ArgumentNullException.ThrowIfNull(environment);

        variable ??= System.Environment.GetEnvironmentVariable;
        string id = First(variable("APP_ID"), variable("POD_NAME"), variable("HOSTNAME")) ?? Guid.CreateVersion7().ToString("N");
        string version = First(variable("BUILD_NUMBER")) ?? Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0] ?? "0.0.0";
        return new AppInfo(First(serviceName, environment.ApplicationName) ?? "unknown", id, environment.EnvironmentName, version, DateTimeOffset.UtcNow);
    }

    private static string? First(params string?[] values)
    {
        foreach (string? value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }
}
