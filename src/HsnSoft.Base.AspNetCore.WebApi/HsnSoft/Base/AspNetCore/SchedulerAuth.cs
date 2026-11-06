using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.AspNetCore;

/// <summary>The settings of the endpoints a scheduler calls (section <c>Scheduler</c>).</summary>
public sealed class SchedulerOptions
{
    public const string SectionName = "Scheduler";

    /// <summary>
    /// The key the scheduler sends in the header <see cref="SchedulerHeaders.ApiKey"/>. It is a secret: it is not in a file of the repository, it comes from the environment (<c>Scheduler__ApiKey</c>), a <c>.env</c> file or the secret of the
    /// platform, and the service does not start without it (<see cref="SchedulerAuthServiceCollectionExtensions.AddBaseSchedulerAuth"/>).
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
}

/// <summary>The headers a scheduler sends with a job.</summary>
public static class SchedulerHeaders
{
    /// <summary>The key of the scheduler (<see cref="SchedulerOptions.ApiKey"/>).</summary>
    public const string ApiKey = "SchedulerApiKey";

    /// <summary>The id of the run of the job; the scheduler also sends it as the correlation id, so the logs of the job and of what it caused are one chain.</summary>
    public const string JobId = "SchedulerJobId";

    /// <summary>The name of the job.</summary>
    public const string JobName = "SchedulerJobName";
}

/// <summary>
/// An endpoint that the scheduler of the system calls (a job: <c>POST .../jobs/...</c>). The caller is not a user, so it has no token: it proves itself with the key of the scheduler (<see cref="SchedulerOptions.ApiKey"/>) in the header
/// <see cref="SchedulerHeaders.ApiKey"/>, compared in constant time; a missing or wrong key is a 401. Put it on a controller that is <c>[AllowAnonymous]</c> (the key is the authentication of the job), and
/// call <see cref="SchedulerAuthServiceCollectionExtensions.AddBaseSchedulerAuth"/> in the host.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class SchedulerAuthAttribute() : TypeFilterAttribute(typeof(SchedulerAuthFilter));

internal sealed class SchedulerAuthFilter(IOptions<SchedulerOptions> options) : IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        byte[] expected = Encoding.UTF8.GetBytes(options.Value.ApiKey);
        string sent = context.HttpContext.Request.Headers[SchedulerHeaders.ApiKey].ToString();
        if (expected.Length == 0 || !CryptographicOperations.FixedTimeEquals(expected, Encoding.UTF8.GetBytes(sent)))
        {
            throw new BaseHttpException(401, "The scheduler key is missing or wrong.").WithKey("scheduler_key_invalid");
        }
    }
}

public static class SchedulerAuthServiceCollectionExtensions
{
    /// <summary>
    /// Lets a service have endpoints for the scheduler (<see cref="SchedulerAuthAttribute"/>): reads <c>Scheduler:ApiKey</c> and <b>requires</b> it, so the service does not start without a key (a job endpoint with no key would
    /// accept nobody, or, worse, everybody).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Holds the section <c>Scheduler</c> (<see cref="SchedulerOptions"/>).</param>
    public static IServiceCollection AddBaseSchedulerAuth(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<SchedulerOptions>(configuration.GetSection(SchedulerOptions.SectionName));
        return services.RequireBaseSettings($"{SchedulerOptions.SectionName}:{nameof(SchedulerOptions.ApiKey)}");
    }
}
