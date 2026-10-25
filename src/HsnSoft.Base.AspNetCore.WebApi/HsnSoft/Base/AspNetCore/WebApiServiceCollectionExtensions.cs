using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using HsnSoft.Base.AspNetCore.Responses;
using HsnSoft.Base.Communication;
using HsnSoft.Base.Localization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.AspNetCore;

public static class WebApiServiceCollectionExtensions
{
    /// <summary>
    /// What every web api of a system repeats: controllers, every answer in the one <see cref="BaseResponse"/> type (results, validation
    /// errors, exceptions, bodyless statuses), the <see cref="GlobalExceptionHandler"/>, and JSON as camel case without nulls and with
    /// enums as text. Map the controllers with <c>app.MapControllers()</c>. Register your own <see cref="IStatusMessageProvider"/> or
    /// <see cref="IApiExceptionMapper"/> before this to change the texts or add exceptions.
    /// </summary>
    public static IServiceCollection AddBaseWebApi(this IServiceCollection services, IConfiguration? configuration = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        OptionsBuilder<ApiErrorOptions> errors = services.AddOptions<ApiErrorOptions>();
        if (configuration is not null)
        {
            errors.Bind(configuration.GetSection(ApiErrorOptions.SectionName));
        }

        services.AddOptions<RequestLanguageOptions>().Configure(options => configuration?.GetSection(RequestLanguageOptions.SectionName).Bind(options));
        services.TryAddSingleton<IRequestLanguageResolver, RequestLanguageResolver>();
        services.TryAddSingleton<IStatusMessageProvider, StatusMessageProvider>();
        services.TryAddSingleton<IApiResponseWriter, ApiResponseWriter>();
        services.TryAddSingleton<IApiExceptionMapper, ApiExceptionMapper>();
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options => ConfigureJson(options.SerializerOptions));
        services.AddControllers(options =>
            {
                options.Filters.Add<UnifiedApiResponseFilter>();
                options.Filters.Add<FileResultLogFilter>();

                // a missing string is the code's business, as in the services in production; the model says what is required
                options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
            })
            .AddJsonOptions(options => ConfigureJson(options.JsonSerializerOptions));
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = InvalidModelState;

            // a bare status (NotFound(), StatusCode(409)) is a BaseResponse like everything else, not a ProblemDetails
            options.SuppressMapClientErrors = true;
        });
        return services;
    }

    /// <summary>
    /// Turns unhandled exceptions into <see cref="BaseResponse"/> answers and gives bodyless statuses (404, 405, ...) a body, except for
    /// files and other binary answers.
    /// </summary>
    public static IApplicationBuilder UseBaseExceptionHandling(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseExceptionHandler();
        app.UseStatusCodePages(WriteStatusBody);
        return app;
    }

    private static void ConfigureJson(JsonSerializerOptions json)
    {
        json.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        json.PropertyNameCaseInsensitive = true;
        json.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        json.Converters.Add(new JsonStringEnumConverter());
    }

    private static BadRequestObjectResult InvalidModelState(ActionContext context)
    {
        IHostEnvironment environment = context.HttpContext.RequestServices.GetRequiredService<IHostEnvironment>();
        System.Collections.Generic.List<string> messages = [context.HttpContext.RequestServices.GetService<IMessageLocalizer>()?.Find("Validation:Failed") ?? "Validation failed."];

        // which field is wrong helps the developer; where the service runs for real the client is not told
        if (!context.HttpContext.RequestServices.GetRequiredService<IOptions<ApiErrorOptions>>().Value.HidesDetails(environment))
        {
            messages.AddRange(context.ModelState
                .Where(entry => entry.Value?.Errors.Count > 0)
                .SelectMany(entry => entry.Value!.Errors.Select(error => $"{entry.Key}: {(string.IsNullOrWhiteSpace(error.ErrorMessage) ? "Invalid value." : error.ErrorMessage)}")));
        }

        return new BadRequestObjectResult(new BaseResponse
        {
            StatusCode = StatusCodes.Status400BadRequest,
            StatusMessages = [.. messages.Distinct()],
            TraceId = ResponseTrace.Of(context.HttpContext),
            ErrorCode = "validation_error",
        });
    }

    // the server only asks for a body when the answer has none and no content type: a file or a stream already has one
    private static async Task WriteStatusBody(StatusCodeContext context)
    {
        HttpContext http = context.HttpContext;
        int status = http.Response.StatusCode;
        using IDisposable? language = RequestLanguageScope.Begin(http);
        string message = http.RequestServices.GetRequiredService<IStatusMessageProvider>().GetMessage(status);
        await http.RequestServices.GetRequiredService<IApiResponseWriter>().WriteErrorAsync(http, status, [message], $"http_{status}").ConfigureAwait(false);
    }
}
