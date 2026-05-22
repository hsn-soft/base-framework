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
using HsnSoft.Base.Communication;
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
                    options.Filters.Add<UnifiedApiResponseFilter>(); // FILTER 03 : controller operation end -> action result filter, don't use in gateway
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
                options.InvalidModelStateResponseFactory = context => // FILTER 02 : before controller start -> model validation filter
                {
                    var env = context.HttpContext.RequestServices.GetRequiredService<IHostEnvironment>();

                    var messages = new List<string> { "Validation failed." };

                    if (!env.IsProduction())
                    {
                        messages.AddRange(
                            context.ModelState
                                .Where(x => x.Value?.Errors.Count > 0)
                                .SelectMany(x => x.Value!.Errors.Select(e =>
                                {
                                    var message = string.IsNullOrWhiteSpace(e.ErrorMessage)
                                        ? "Invalid value."
                                        : e.ErrorMessage;

                                    return $"{x.Key}: {message}";
                                })));
                    }

                    var response = new BaseResponse
                    {
                        StatusCode = StatusCodes.Status400BadRequest,
                        StatusMessages = messages.Distinct().ToList(),
                        TraceId = context.HttpContext.TraceIdentifier,
                        ErrorCode = "validation_error"
                    };

                    return new BadRequestObjectResult(response);
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