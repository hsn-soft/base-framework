using System.Text.Json;
using System.Text.Json.Serialization;
using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.AspNetCore;
using HsnSoft.Base.AspNetCore.Responses;
using HsnSoft.Base.Communication;
using HsnSoft.Base.Sample.Api.Hosting.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HsnSoft.Base.Sample.Api.Hosting.Extensions;

/// <summary>
/// The microservice-level hosting composition. In a real solution this lives in the shared repository
/// (e.g. Hhs.Shared.Hosting.Microservices) and is consumed by every microservice as a NuGet package.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSampleMicroserviceHosting(this IServiceCollection services, Type apiAssemblyMarker)
    {
        // Upper bound for paging requests
        LimitedDataRequestDto.MaxMaxResultCount = 100;

        services.AddBaseAspNetCoreContextCollection();

        // Unified response + global exception handling
        services.AddSingleton<IApiExceptionMapper, ApiExceptionMapper>();
        services.AddSingleton<IApiResponseWriter, ApiResponseWriter>();
        services.AddSingleton<IStatusMessageProvider, StatusMessageProvider>();
        services.AddExceptionHandler<GlobalApiExceptionHandler>();
        services.AddProblemDetails();

        services.AddControllers(options =>
            {
                options.Filters.Add<UnifiedApiResponseFilter>();
                options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
            })
            .AddApplicationPart(apiAssemblyMarker.Assembly)
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
                var environment = context.HttpContext.RequestServices.GetRequiredService<IHostEnvironment>();
                var messages = new List<string> { "Validation failed." };

                if (!environment.IsProduction())
                {
                    messages.AddRange(context.ModelState
                        .Where(x => x.Value?.Errors.Count > 0)
                        .SelectMany(x => x.Value!.Errors.Select(e => $"{x.Key}: {(string.IsNullOrWhiteSpace(e.ErrorMessage) ? "Invalid value." : e.ErrorMessage)}")));
                }

                return new BadRequestObjectResult(new BaseResponse
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    StatusMessages = messages.Distinct().ToList(),
                    TraceId = context.HttpContext.TraceIdentifier,
                    ErrorCode = "validation_error"
                });
            };
        });

        return services;
    }
}
