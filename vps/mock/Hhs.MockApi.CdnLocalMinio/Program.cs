using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
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

// Ensure bucket exists
EnsureBucketExists(app.Services).Wait();

app.MapControllers();
app.Run();

async Task EnsureBucketExists(IServiceProvider services)
{
    var minioClient = services.GetRequiredService<IMinioClient>();
    var minioOptions = services.GetRequiredService<IOptions<MinioOptions>>().Value;

    var bucketExistsArgs = new BucketExistsArgs().WithBucket(minioOptions.BucketName);
    var isBucketExist = await minioClient.BucketExistsAsync(bucketExistsArgs);
    if (!isBucketExist)
    {
        var makeBucketArgs = new MakeBucketArgs().WithBucket(minioOptions.BucketName);
        await minioClient.MakeBucketAsync(makeBucketArgs);
        Console.WriteLine($"✅ Bucket '{minioOptions.BucketName}' created");
    }
    else
    {
        Console.WriteLine($"✅ Bucket '{minioOptions.BucketName}' exists");
    }
}
