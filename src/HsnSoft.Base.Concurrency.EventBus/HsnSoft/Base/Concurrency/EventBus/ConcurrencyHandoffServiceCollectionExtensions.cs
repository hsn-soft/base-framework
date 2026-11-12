using System;
using HsnSoft.Base.EventBus;
using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.Concurrency.EventBus;

public static class ConcurrencyHandoffServiceCollectionExtensions
{
    /// <summary>
    /// Lets a place of a concurrency limit travel with an event: the event an endpoint with <c>[ConcurrencyLimit(HandOff = true)]</c> publishes carries the place (<see cref="ConcurrencyHandoffHeaderProvider"/>), and the service that handles it keeps it alive and gives it back
    /// (<see cref="ConcurrencyHandoffObserver"/>). Registers the two; the limiter (<c>AddRedisConcurrencyLimiter</c>, or <c>AddInMemoryConcurrencyLimiter</c>) and the event bus are registered by the service. The service that publishes and the service that handles may be the same, as a job of the scheduler is.
    /// </summary>
    public static IServiceCollection AddConcurrencyHandoff(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddEventHeaderProvider<ConcurrencyHandoffHeaderProvider>();
        services.AddEventHandlingObserver<ConcurrencyHandoffObserver>();
        return services;
    }
}
