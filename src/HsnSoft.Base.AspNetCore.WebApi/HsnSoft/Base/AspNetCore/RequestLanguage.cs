using System;
using System.Linq;
using System.Threading.Tasks;
using HsnSoft.Base.Localization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace HsnSoft.Base.AspNetCore;

/// <summary>Where the language of a request is read from. Section <c>Localization</c>.</summary>
public sealed class RequestLanguageOptions
{
    public const string SectionName = "Localization";

    /// <summary>The claim of the token that holds the language of the user. Default <c>locale</c>.</summary>
    public string LanguageClaim { get; set; } = "locale";
}

/// <summary>Decides in which language a request is answered.</summary>
public interface IRequestLanguageResolver
{
    /// <summary>
    /// The language claim of the token when the user has one that is supported; else the first supported language of the <c>Accept-Language</c> header (the highest quality first);
    /// else the default language.
    /// </summary>
    string Resolve(HttpContext context);
}

public sealed class RequestLanguageResolver(IServiceProvider services, IOptions<RequestLanguageOptions> options) : IRequestLanguageResolver
{
    public string Resolve(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // asked for here, not at the construction: a host without localization has this registered too and never calls it
        ILanguageAccessor languages = services.GetRequiredService<ILanguageAccessor>();

        if (languages.Normalize(context.User.FindFirst(options.Value.LanguageClaim)?.Value) is { } fromToken)
        {
            return fromToken;
        }

        RequestHeaders headers = context.Request.GetTypedHeaders();
        foreach (StringWithQualityHeaderValue preference in headers.AcceptLanguage.OrderByDescending(language => language.Quality ?? 1.0))
        {
            if (preference.Quality is 0)
            {
                continue;
            }

            if (languages.Normalize(preference.Value.Value) is { } fromHeader)
            {
                return fromHeader;
            }
        }

        return languages.DefaultLanguage;
    }
}

/// <summary>Sets the language of a request for the rest of the pipeline and tells it in <c>Content-Language</c>.</summary>
public sealed class RequestLanguageMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IRequestLanguageResolver resolver, ILanguageAccessor languages)
    {
        ArgumentNullException.ThrowIfNull(context);

        string language = resolver.Resolve(context);
        context.Response.Headers.ContentLanguage = language;
        using (languages.Use(language))
        {
            await next(context).ConfigureAwait(false);
        }
    }
}

internal static class RequestLanguageScope
{
    /// <summary>
    /// Answers in the language of the request while the scope lasts. The handlers of the outer layers (the exception handler, the status pages) use it: the language the middleware set
    /// does not reach them, as it was set inside. Nothing when the localization is not registered.
    /// </summary>
    public static IDisposable? Begin(HttpContext http)
    {
        // a context built by hand may have no services
        IServiceProvider? services = http.RequestServices;
        ILanguageAccessor? languages = services?.GetService<ILanguageAccessor>();
        return languages?.Use(services!.GetRequiredService<IRequestLanguageResolver>().Resolve(http));
    }
}

public static class RequestLanguageExtensions
{
    /// <summary>
    /// Answers each request in its language: the language claim of the token, else the <c>Accept-Language</c> header, else the default language. Put it after the authentication, so
    /// the claim is there. It does nothing when <c>AddBaseLocalization</c> was not called.
    /// </summary>
    public static IApplicationBuilder UseBaseLocalization(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.ApplicationServices.GetService<ILanguageAccessor>() is null ? app : app.UseMiddleware<RequestLanguageMiddleware>();
    }
}
