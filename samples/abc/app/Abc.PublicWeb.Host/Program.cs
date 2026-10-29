using Abc.PublicWeb.Host.Services;
using HsnSoft.Base.AspNetCore;
using HsnSoft.Base.Logging;
using HsnSoft.Base.Logging.Serilog;
using Microsoft.AspNetCore.Mvc;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Logging.AddBaseSerilog(builder.Configuration, "Abc.PublicWeb");

builder.Services
    .AddBaseLogging()
    .AddBaseRequestLogging(builder.Configuration)
    .AddBaseHealthChecks()
    .AddHttpContextAccessor();
builder.Services.AddControllersWithViews(options => options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));

// every call to the public gateway carries the correlation id of the page being served
builder.Services.AddTransient<GatewayRequestHandler>();
builder.Services
    .AddHttpClient<GatewayClient>(client => client.BaseAddress = new Uri(builder.Configuration["Gateway:BaseUrl"] ?? throw new InvalidOperationException("Gateway:BaseUrl is required.")))
    .AddHttpMessageHandler<GatewayRequestHandler>();

WebApplication app = builder.Build();

app.UseBaseRequestLogging();   // outside the exception page: the response log has the status the client gets
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/home/error");
}

app.UseRouting();
app.MapBaseHealth();
app.MapDefaultControllerRoute();

await app.RunAsync();
