using Def.Signer.Host.Endpoints;
using Def.Signer.Host.Events;
using Def.Signer.Host.Handlers;
using Def.Signer.Host.Persistence;
using Def.Signer.Host.Security;
using HsnSoft.Base.AspNetCore;
using HsnSoft.Base.BlobStoring.Minio;
using HsnSoft.Base.Caching.StackExchangeRedis;
using HsnSoft.Base.EntityFrameworkCore;
using HsnSoft.Base.EntityFrameworkCore.SqlServer;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.EventBus.Kafka;
using HsnSoft.Base.Logging;
using HsnSoft.Base.Logging.Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Logging.AddBaseSerilog(builder.Configuration, "Def.Signer");

// One host, so it picks its pieces itself: Redis (API keys), SQL Server (metadata), MinIO (documents), Kafka (the queue).
builder.Services
    .AddBaseLogging()
    .AddBaseRequestLogging(builder.Configuration)
    .AddBaseWebApi()
    .AddBaseOpenApi()
    .AddBaseHealthChecks()
    .AddBaseRedis(builder.Configuration)
    .AddApiKeyAuthentication()
    .AddBaseSqlServer<SignerDbContext>(builder.Configuration)
    .AddMinioBlobStoring(builder.Configuration)
    .AddKafkaEventBus(builder.Configuration)
    .AddEventHandler<SignRequested, SignRequestedHandler>()
    .Configure<SignerOptions>(builder.Configuration.GetSection("Signer"));

WebApplication app = builder.Build();

await app.Services.EnsureDatabaseCreatedAsync<SignerDbContext>();
await app.Services.SeedApiKeysAsync(app.Configuration);

app.UseBaseRequestLogging();   // outside the exception handler: the response log has the status the client gets
app.UseBaseExceptionHandling();
app.MapBaseOpenApi();
app.UseAuthentication();
app.UseAuthorization();
app.MapBaseHealth();
app.MapDocumentEndpoints();

await app.RunAsync();
