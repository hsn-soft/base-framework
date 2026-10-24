using HsnSoft.Base.AspNetCore;
using HsnSoft.Base.Logging;
using HsnSoft.Base.Logging.Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Logging.AddBaseSerilog(builder.Configuration, "Abc.IdentityService");

builder.Services
    .AddBaseLogging()
    .AddBaseRequestLogging(builder.Configuration)
    .AddBaseWebApi()
    .AddBaseSwagger("Abc.IdentityService API", builder.Configuration)
    .AddBaseHealthChecks();

WebApplication app = builder.Build();

app.UseBaseRequestLogging();   // outside the exception handler: the response log has the status the client gets
app.UseBaseExceptionHandling();
app.UseBaseSwagger();
app.MapBaseHealth();
app.MapControllers();

await app.RunAsync();
