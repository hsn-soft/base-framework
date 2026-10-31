using CoreWCF.Configuration;
using CoreWCF.Description;
using Edx.Mock.Center.MongoDB;
using Edx.Mock.Center.Wcf.Host;
using Edx.Mock.Center.Wcf.Host.Services;
using Edx.Mock.Center.Wcf.Host.Workers;
using Edx.Shared.Efatura.Server;
using Edx.Shared.Efatura.Wire;
using Edx.Shared.Hosting;
using HsnSoft.Base.AspNetCore;
using HsnSoft.Base.BlobStoring.Minio;
using HsnSoft.Base.Logging.Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.AddBaseHosting();   // the .env file, the limits of Kestrel, the proxies and the time to stop: the section Hosting
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

// a failure to start (a setting that is missing, a database that cannot be prepared) is logged as critical and ends the process with the exit code 1
return await builder.RunBaseAsync(async app =>
{
    await app.Services.SeedParticipantsAsync();

    app.UseEdxHosting(authenticateUsers: false);
    app.MapControllers();
    app.UseServiceModel(services => services
        .AddService<CenterService>()
        .AddServiceEndpoint<CenterService, IEFaturaPortType>(EFaturaServerBinding.Create(useTransportSecurity: false, center.MaxReceivedMessageSize), WireNames.CenterPath));
    app.Services.GetRequiredService<ServiceMetadataBehavior>().HttpGetEnabled = true;
});
