using Abc.AuthServer.Host.Security;
using Abc.AuthServer.Host.Services;
using Abc.IdentityService.EntityFrameworkCore;
using Abc.Shared.Helper;
using Abc.Shared.Hosting.Microservices;
using Abc.Shared.Localization;
using HsnSoft.Base.AspNetCore;
using HsnSoft.Base.Caching.StackExchangeRedis;
using HsnSoft.Base.Localization;
using HsnSoft.Base.Logging.Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Logging.AddBaseSerilog(builder.Configuration, "Abc.AuthServer");

builder.Services
    // the texts in the languages of the system: the framework's and the shared ones (the text of a wrong password)
    .AddBaseLocalization(builder.Configuration, options => options.AddAbcSharedTexts())
    .AddBaseRedis(builder.Configuration)
    .AddAbcMicroservice(builder.Configuration, "Abc.AuthServer API")
    .AddIdentityPersistence(builder.Configuration, AbcTenants.System, AbcTenants.Acme)
    .AddIdentityHistoryPersistence(builder.Configuration)
    .AddScoped<UserAuthenticator>()
    .AddSingleton<TokenService>();

WebApplication app = builder.Build();

// the private key is loaded now: a missing key stops the host here, not at the first login
_ = app.Services.GetRequiredService<TokenService>();

app.UseAbcMicroservice();
app.UseBaseLocalization();   // after the caller is known (the hosting reads the token): the language is the one of the token, else of the header
app.MapControllers();

await app.RunAsync();
