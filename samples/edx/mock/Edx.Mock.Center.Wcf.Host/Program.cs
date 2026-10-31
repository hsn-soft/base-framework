using CoreWCF.Configuration;
using CoreWCF.Description;
using Edx.Mock.Center.MongoDB;
using Edx.Mock.Center.Wcf.Host;
using Edx.Mock.Center.Wcf.Host.Services;
using Edx.Mock.Center.Wcf.Host.Workers;
using Edx.Shared.Efatura.Server;
using Edx.Shared.Efatura.Wire;
using Edx.Shared.Hosting;
using HsnSoft.Base.BlobStoring.Minio;
using HsnSoft.Base.Logging.Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Logging.AddBaseSerilog(builder.Configuration, "Edx.Mock.Center");

CenterOptions center = builder.Configuration.GetSection(CenterOptions.SectionName).Get<CenterOptions>() ?? new CenterOptions();
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = center.MaxReceivedMessageSize + (1024 * 1024);
    options.AllowSynchronousIO = true;
});

// the shell every Edx host has, without a token: the center is open, as the one of GİB is to the integrators that know its address
builder.Services
    .AddEdxHosting(builder.Configuration, "Edx.Mock.Center", authenticateUsers: false)
    .Configure<CenterOptions>(builder.Configuration.GetSection(CenterOptions.SectionName))
    .AddCenterPersistence(builder.Configuration)
    .AddMinioBlobStoring(builder.Configuration)
    .AddSingleton(TimeProvider.System)
    .AddHttpContextAccessor()
    .AddScoped<EnvelopeStorage>()
    .AddScoped<EnvelopeIntake>()
    .AddScoped<EnvelopeAnalyzer>()
    .AddScoped<EnvelopePreparer>()
    .AddScoped<EnvelopeDeliverer>()
    .AddScoped<AnswerPoller>()
    .AddHostedService<AnalyzeWorker>()
    .AddHostedService<PrepareWorker>()
    .AddHostedService<DeliveryWorker>()
    .AddHostedService<AnswerPollWorker>()
    .AddTransient<CenterService>()
    .AddServiceModelServices()
    .AddServiceModelMetadata();

WebApplication app = builder.Build();

await app.Services.SeedParticipantsAsync();

app.UseEdxHosting(authenticateUsers: false);
app.MapControllers();
app.UseServiceModel(services => services
    .AddService<CenterService>()
    .AddServiceEndpoint<CenterService, IEFaturaPortType>(EFaturaServerBinding.Create(useTransportSecurity: false, center.MaxReceivedMessageSize), WireNames.CenterPath));
app.Services.GetRequiredService<ServiceMetadataBehavior>().HttpGetEnabled = true;

await app.RunAsync();
