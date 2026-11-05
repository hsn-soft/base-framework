using HsnSoft.Base.AspNetCore;
using HsnSoft.Base.Authentication;
using HsnSoft.Base.Caching.StackExchangeRedis;
using HsnSoft.Base.Localization;
using HsnSoft.Base.Logging.Serilog;
using Hsx.AuthServer.Host.Security;
using Hsx.AuthServer.Host.Services;
using Hsx.IdentityService.EntityFrameworkCore;
using Hsx.Shared.Hosting.Http;
using Hsx.Shared.Hosting.Microservices;
using Hsx.Shared.Localization;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.AddBaseHosting();   // the .env file, the port, the limits of Kestrel, the proxies and the time to stop: the section Hosting
builder.Logging.AddBaseSerilog(builder.Configuration, "Hsx.AuthServer");

builder.Services.Configure<AuthServerOptions>(builder.Configuration.GetSection(AuthServerOptions.SectionName));
builder.Services
    // the texts in the languages of the system: the framework's and the shared ones (the text of a wrong password)
    .AddBaseLocalization(builder.Configuration, options => options.AddHsxSharedTexts())
    .AddBaseRedis(builder.Configuration)
    .AddHsxMicroservice(builder.Configuration, "Hsx.AuthServer API")
    .AddBaseHealthChecks(builder.Configuration)   // /health/live, /startup and /ready: leave it out and the service has none
    .AddHsxAuthentication(builder.Configuration)
    .AddHsxAuthorization()
    .AddIdentityPersistence(builder.Configuration)
    .AddScoped<UserAuthenticator>()
    .AddSingleton<TokenService>();

// a failure to start (a setting that is missing, a database that cannot be prepared) is logged as critical and ends the process with the exit code 1
return await builder.RunBaseAsync(app =>
{
    // the private key is loaded now: a missing key stops the host here, not at the first login
    _ = app.Services.GetRequiredService<TokenService>();

    app.UseHsxMicroservice();
    app.MapBaseHealth();
    app.UseBaseAuthentication();
    app.UseBaseAuthorization();
    app.UseBaseLocalization();   // after the caller is known (the hosting reads the token): the language is the one of the token, else of the header
    app.MapControllers();
});
