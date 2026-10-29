using System;
using HsnSoft.Base.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HsnSoft.Base.Application;

/// <summary>
/// What every application service has at hand: <see cref="Logger"/>, and <see cref="CurrentUser"/>, found from the services the first time they are used (the same as the base controller
/// of a web api). A service derives its own base from this one and adds what is its own (a localizer); the application service passes the <see cref="IServiceProvider"/> it is given up the chain, and asks for
/// everything else in its constructor.
/// </summary>
/// <param name="services">The services of the application (the dependency injection gives it to any constructor that asks).</param>
public abstract class ApplicationService(IServiceProvider services)
{
    /// <summary>The services this one was made from, for what a derived base finds the same way (<c>ServiceProvider.GetRequiredService&lt;IStringLocalizer&gt;()</c>).</summary>
    protected IServiceProvider ServiceProvider { get; } = services ?? throw new ArgumentNullException(nameof(services));

    /// <summary>
    /// Writes the application logs of the service (<c>Logger.LogInformation("Order {OrderId} was made", id)</c>): they are the <c>AppLog</c>s of the application, under the category of the class that writes them,
    /// with the <c>CorrelationId</c> of the request.
    /// </summary>
    protected ILogger Logger => field ??= ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(GetType());

    /// <summary>Who is making the request: its id and user name. Without a request (a worker) there is no user.</summary>
    protected ICurrentUser CurrentUser => field ??= ServiceProvider.GetRequiredService<ICurrentUser>();
}
