using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;
using Minio;
using Hhs.MockApi.CdnLocalMinio.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<MinioOptions>(
    builder.Configuration.GetSection("Minio"));

builder.Services.Configure<List<CdnCustomerOptions>>(
    builder.Configuration.GetSection("CdnCustomers"));

builder.Services.AddSingleton<IMinioClient>(sp =>
{
    var opt = builder.Configuration.GetSection("Minio").Get<MinioOptions>()!;
    return new MinioClient()
        .WithEndpoint(opt.Endpoint)
        .WithCredentials(opt.AccessKey, opt.SecretKey)
        .WithSSL(opt.UseSsl)
        .Build();
});

builder.Services.AddSingleton<FileExtensionContentTypeProvider>();
builder.Services.AddControllers();

var app = builder.Build();
app.MapControllers();
app.Run();
