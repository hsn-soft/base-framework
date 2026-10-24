using Abc.IdentityService.Application;
using Abc.IdentityService.EntityFrameworkCore;
using Abc.IdentityService.EntityFrameworkCore.Context;
using HsnSoft.Base.AspNetCore;
using HsnSoft.Base.Authentication;
using HsnSoft.Base.EntityFrameworkCore;
using HsnSoft.Base.EntityFrameworkCore.PostgreSql;
using HsnSoft.Base.Logging;
using HsnSoft.Base.Logging.Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Logging.AddBaseSerilog(builder.Configuration, "Abc.IdentityService");

builder.Services
    .AddBaseLogging()
    .AddBaseRequestLogging(builder.Configuration)
    .AddBaseWebApi()
    .AddBaseJwtAuthentication(builder.Configuration)
    .AddBaseSwaggerWithBearer("Abc.IdentityService API", "Enter a valid token", builder.Configuration)
    .AddBaseHealthChecks()
    .AddBasePostgres<IdentityDbContext>(builder.Configuration)
    .AddIdentityPersistence()
    .AddIdentityApplication();

WebApplication app = builder.Build();

await app.Services.EnsureDatabaseCreatedAsync<IdentityDbContext>();

app.UseBaseRequestLogging();   // outside the exception handler: the response log has the status the client gets
app.UseBaseExceptionHandling();
app.UseBaseSwagger();
app.UseBaseAuthentication();
app.UseBaseAuthorization();
app.MapBaseHealth();
app.MapControllers();

await app.RunAsync();
