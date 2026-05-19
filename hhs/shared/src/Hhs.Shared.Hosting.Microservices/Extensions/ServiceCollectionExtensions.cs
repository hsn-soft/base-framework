using System.Text.Json;
using System.Text.Json.Serialization;
using Hhs.Shared.Contracts.Cache;
using Hhs.Shared.Hosting.Extensions;
using Hhs.Shared.Hosting.Microservices.Cache;
using Hhs.Shared.Hosting.Microservices.Middlewares;
using Hhs.Shared.Hosting.Microservices.Workers;
using Hhs.Shared.Hosting.Workers;
using HsnSoft.Base;
using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.AspNetCore;
using HsnSoft.Base.AspNetCore.Hosting.Loader;
using HsnSoft.Base.AspNetCore.Responses;
using HsnSoft.Base.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Hhs.Shared.Hosting.Microservices.Extensions;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddMicroserviceHosting(IConfiguration configuration, Type type)
        {
            Microsoft.IdentityModel.Logging.IdentityModelEventSource.ShowPII = true;

            // Set filter limit value
            LimitedDataRequestDto.MaxMaxResultCount = 100;

            services.AddCommonAspNetCoreHosting(configuration);
            services.Configure<MicroserviceHostingSettings>(configuration.GetSection("HostingSettings"));

            services.AddBaseAspNetCoreContextCollection();

            services.AddControllers(options =>
                {
                    options.Filters.Add<UnifiedApiResponseFilter>();
                    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
                })
                .AddApplicationPart(type.Assembly)
                .AddJsonOptions(options =>
                {
                    var json = options.JsonSerializerOptions;

                    json.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                    json.PropertyNameCaseInsensitive = true;
                    json.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                    json.Converters.Add(new JsonStringEnumConverter());
                });

            services.Configure<ApiBehaviorOptions>(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var writer = context.HttpContext.RequestServices.GetRequiredService<IApiResponseWriter>();

                    var env = context.HttpContext.RequestServices.GetRequiredService<IHostEnvironment>();

                    var messages = new List<string> { "Validation failed." };

                    if (!env.IsProduction())
                    {
                        messages.AddRange(
                            context.ModelState
                                .Where(x => x.Value?.Errors.Count > 0)
                                .SelectMany(x => x.Value!.Errors.Select(e => $"{x.Key}: {e.ErrorMessage}")));
                    }

                    writer.WriteErrorAsync(
                        context.HttpContext,
                        StatusCodes.Status400BadRequest,
                        messages,
                        "validation_error"
                    ).GetAwaiter().GetResult();

                    return new EmptyResult();
                };
            });

            // Service permission store worker
            services.AddSingleton<IServicePermissionProvider, DefaultServicePermissionProvider>();
            services.AddHostingRedis(configuration);
            services.AddTransient<ICachePermissionGrantRepository, CachePermissionGrantRepository>();
            services.AddHostedService<SynchServicePermissionStoreBackgroundService>();

            // Loader functionality
            services.AddTransient<IBasicLoader, AppBasicLoader>();
            services.AddTransient<IBasicDataSeeder, DefaultBasicDataSeeder>();
            services.AddHostedService<LoaderHostedService>();

            return services;
        }

        public IServiceCollection AddMicroserviceUserTenantChecker()
        {
            services.AddBaseDataServiceCollection();
            services.AddScoped<UserTenantCheckerMiddleware>();

            return services;
        }
    }
}