using System.Reflection;
using Hhs.Shared.Hosting.Middlewares;
using Hhs.Shared.Localization;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.Reflection;
using HsnSoft.Base.Validation.Localization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Localization;

namespace Hhs.Shared.Hosting.Extensions;

public static class ApplicationBuilderExtensions
{
    extension(WebApplication app)
    {
        public void ConfigureLocalizedModelValidator(Type serviceResourceType)
        {
            EnumHelper.Configure(app.Services.GetService<IStringLocalizerFactory>(), serviceResourceType);
            LocalizedModelValidator.Configure(app.Services.GetService<IStringLocalizerFactory>(), [
                serviceResourceType,
                typeof(ValidationResource)
            ]);
        }

        public void UseDefaultCorsSettings(string corsName) { app.UseCors(corsName); }

        public void UseSearchEngineAgent() => app.UseMiddleware<SearchEngineAgentMiddleware>();

        public void UseEventBus(Assembly assembly, Dictionary<string, ushort> eventFetchCounts = null)
        {
            var refType = typeof(IIntegrationEventHandler);
            var eventHandlerTypes = assembly.GetTypes()
                .Where(p => refType.IsAssignableFrom(p) && p is { IsInterface: false, IsAbstract: false }).ToList();

            if (eventHandlerTypes is not { Count: > 0 }) return;

            var eventBus = app.Services.GetRequiredService<IEventBus>();

            foreach (var eventHandlerType in eventHandlerTypes)
            {
                var eventType = eventHandlerType.GetInterfaces().First(x => x.IsGenericType).GenericTypeArguments[0];

                ushort fetchCount = 0;
                if (eventFetchCounts is { Count: > 0 })
                {
                    if (eventFetchCounts.TryGetValue(eventType.Name, out ushort eventFetchCount))
                    {
                        fetchCount = eventFetchCount;
                    }
                }

                eventBus.Subscribe(eventType, eventHandlerType, fetchCount < 1 ? (ushort)1 : fetchCount);
            }
        }

        public void UseHostingHealthChecks()
        {
            // app.UseHealthChecks("/StartupCheck", new HealthCheckOptions { Predicate = _ => true });
            // app.UseHealthChecks("/LivenessCheck", new HealthCheckOptions { Predicate = _ => true });
            // app.UseHealthChecks("/ReadinessCheck", new HealthCheckOptions { Predicate = _ => true });

            app.UseHealthChecks("/StartupCheck", new HealthCheckOptions { Predicate = r => r.Tags.Contains("dependencies"), ResponseWriter = CustomHealthCheckResponse });
            app.UseHealthChecks("/LivenessCheck", new HealthCheckOptions { Predicate = r => r.Name.Equals("self-check") });
            app.UseHealthChecks("/ReadinessCheck", new HealthCheckOptions
            {
                Predicate = r => r.Tags.Contains("dependencies"),
                //ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                ResponseWriter = CustomHealthCheckResponse
            });
        }

        private static async Task CustomHealthCheckResponse(HttpContext context, HealthReport report)
        {
            context.Response.ContentType = "application/json";
            string result = System.Text.Json.JsonSerializer.Serialize(
                new { status = report.Status.ToString(), checks = report.Entries.Select(e => new { name = e.Key, status = e.Value.Status.ToString(), exception = e.Value.Exception?.Message, duration = e.Value.Duration }) });

            await context.Response.WriteAsync(result);
        }
    }
}