using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.EventBus.RabbitMQ;

public static class BaseEventBusRabbitMqServiceCollectionExtensions
{
    public static IServiceCollection AddBaseEventBusRabbitMqServiceCollection(this IServiceCollection services)
    {
        // public override void ConfigureServices(ServiceConfigurationContext context)
        // {
        //     var configuration = context.Services.GetConfiguration();
        //
        //     Configure<BaseRabbitMqEventBusOptions>(configuration.GetSection("RabbitMQ:EventBus"));
        // }
        //
        // public override void OnApplicationInitialization(ApplicationInitializationContext context)
        // {
        //     context
        //         .ServiceProvider
        //         .GetRequiredService<RabbitMqDistributedEventBus>()
        //         .Initialize();
        // }


        // services.AddSingleton(typeof(IGuidGenerator), typeof(SimpleGuidGenerator));

        return services;
    }
}