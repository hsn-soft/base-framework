using HsnSoft.Base.AspNetCore;
using HsnSoft.Base.Authentication;
using HsnSoft.Base.Caching.StackExchangeRedis;
using HsnSoft.Base.Localization;
using HsnSoft.Base.Logging.Serilog;
using Hsx.AuthServer.Contracts.AuthDomain.Services;
using Hsx.AuthServer.Security;
using Hsx.AuthServer.Security.Clients;
using Hsx.AuthServer.Security.RateLimiting;
using Hsx.AuthServer.Services;
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
    .AddHttpContextAccessor()   // the application service reads the address, the agent and the refresh cookie of the request, so a controller has no logic
    .AddAuthRateLimiting(builder.Configuration)   // the requests of one address in a minute: the sign-in is the strictest (the section Auth:RateLimits); this host is open to the internet
    .AddSingleton(TimeProvider.System)
    .AddSingleton<ClientRegistry>()   // the applications that may ask for a token (the section Auth:Clients), kept in memory
    .AddServiceEfCoreDatabaseConfiguration(builder.Configuration)   // the repositories of the identity database; the host that owns it (the identity service, SeedStartupConnections) prepares it, this one does not
    .AddScoped<UserAuthenticator>()
    .AddScoped<IAuthAppService, AuthAppService>()
    .AddSingleton<TokenService>();

// a failure to start (a setting that is missing, a database that cannot be prepared) is logged as critical and ends the process with the exit code 1
return await builder.RunBaseAsync(app =>
{
    // the private key is loaded now: a missing key stops the host here, not at the first login
    _ = app.Services.GetRequiredService<TokenService>();
    // the clients are checked now too: a setting that cannot be right stops the host here, not at the first sign-in
    _ = app.Services.GetRequiredService<ClientRegistry>();

    app.UseHsxMicroservice();
    app.UseBaseSecurityHeaders();   // nosniff, no framing, no referrer, and HSTS on https (this host is open to browsers)
    app.UseBaseCors();   // only the origins of Hosting:Cors may call: none when none is listed
    app.UseRateLimiter();   // after the proxies (the address of the caller) and before the authentication
    app.MapBaseHealth();
    app.UseBaseAuthentication();
    app.UseBaseAuthorization();
    app.UseBaseLocalization();   // after the caller is known (the hosting reads the token): the language is the one of the token, else of the header
    app.MapControllers();
});
