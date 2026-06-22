using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;
using Hhs.MockApi.CdnAbc.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<StorageApiOptions>(
    builder.Configuration.GetSection("StorageApi"));

builder.Services.Configure<List<CdnCustomerOptions>>(
    builder.Configuration.GetSection("CdnCustomers"));

builder.Services.AddSingleton<FileExtensionContentTypeProvider>();
builder.Services.AddHttpClient();
builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();
app.Run();

// ============================================================================
// OPTIONS CLASSES
// ============================================================================

public sealed class StorageApiOptions
{
    public string BaseUrl { get; set; } = default!;
    public string ApiKey { get; set; } = default!;
}
