using CoreWCF.Configuration;
using CoreWCF.Description;
using Edx.Integration.GibInvoice.Application;
using Edx.Integration.GibInvoice.Application.Options;
using Edx.Integration.GibInvoice.MongoDB;
using Edx.Integration.GibInvoice.Wcf.Host;
using Edx.Shared.Efatura.Server;
using Edx.Shared.Hosting;
using HsnSoft.Base.AspNetCore;
using HsnSoft.Base.BlobStoring.Minio;
using HsnSoft.Base.Logging.Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.AddBaseHosting();   // the .env file, the limits of Kestrel, the proxies and the time to stop: the section Hosting
builder.Logging.AddBaseSerilog(builder.Configuration, "Edx.Integration.GibInvoice.Wcf");

GibCenterOptions center = builder.Configuration.GetSection(GibCenterOptions.SectionName).Get<GibCenterOptions>() ?? new GibCenterOptions();

// CoreWCF reads its messages synchronously; the biggest message is the setting of the center
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = center.MaxMessageSize + (1024 * 1024);
    options.AllowSynchronousIO = true;
});

// the shell every Edx host has, without a token: the center calls the post box as an integrator is called, by its address
builder.Services
    .AddEdxHosting(builder.Configuration, "Edx.Integration.GibInvoice WCF", authenticateUsers: false)
    .AddIntegrationPersistence(builder.Configuration)
    .AddMinioBlobStoring(builder.Configuration)
    .AddIntegrationPostBox(builder.Configuration)
    .AddHttpContextAccessor()
    .AddTransient<PostBoxService>()
    .AddServiceModelServices()
    .AddServiceModelMetadata();

// a failure to start (a setting that is missing, a database that cannot be prepared) is logged as critical and ends the process with the exit code 1
return await builder.RunBaseAsync(app =>
{
    app.UseEdxHosting(authenticateUsers: false);
    app.UseServiceModel(services => services
        .AddService<PostBoxService>()
        .AddServiceEndpoint<PostBoxService, IEFaturaPortType>(EFaturaServerBinding.Create(useTransportSecurity: false, center.MaxMessageSize), "/EFatura/"));
    app.Services.GetRequiredService<ServiceMetadataBehavior>().HttpGetEnabled = true;
});
