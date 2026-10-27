#pragma warning disable CA1848, CA1873 // the tests call the logger the way a service does
using System.Text;
using HsnSoft.Base.BlobStoring;
using HsnSoft.Base.BlobStoring.Azure;
using HsnSoft.Base.EntityFrameworkCore;
using HsnSoft.Base.EntityFrameworkCore.MySql;
using HsnSoft.Base.IntegrationTests.Data;
using HsnSoft.Base.Logging;
using HsnSoft.Base.Logging.NLog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Testcontainers.Azurite;
using Testcontainers.MySql;

namespace HsnSoft.Base.IntegrationTests;

/// <summary>
/// A service that makes other choices than the Abc sample does (NLog instead of Serilog, Azure Blob Storage instead of MinIO, MySQL instead of PostgreSQL), composed from the framework packages alone: it logs, stores a row and a blob, and
/// answers its readiness, with the real services behind it. The packages of the other choices are not part of it (see the dependency graph in ADR-0004).
/// </summary>
public sealed class AlternativeStackTests : IAsyncLifetime
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "hsn-stack-" + Guid.NewGuid().ToString("N"));
    private readonly AzuriteContainer _azurite = new AzuriteBuilder("mcr.microsoft.com/azure-storage/azurite:3.35.0").WithCommand("--skipApiVersionCheck").Build();
    private readonly MySqlContainer _mysql = new MySqlBuilder("mysql:8.4").Build();

    public async Task InitializeAsync() => await Task.WhenAll(_azurite.StartAsync(), _mysql.StartAsync());

    public async Task DisposeAsync()
    {
        await _azurite.DisposeAsync();
        await _mysql.DisposeAsync();
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Fact]
    public async Task ARowAndABlobAreStored_TheLogsAreInTheFile_AndTheServiceIsReady()
    {
        string logFile = Path.Combine(_directory, "logs.txt");
        IConfiguration configuration = Support.Configuration(
            ("ConnectionStrings:MySql", _mysql.GetConnectionString()),
            ("AzureBlob:ConnectionString", _azurite.GetConnectionString()),
            ("PersistentLogger:FilePath", logFile),
            ("PersistentLogger:AppNamespace", "Acme"));

        Microsoft.Extensions.Hosting.HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        builder.Logging.AddBaseNLog(configuration, "Acme.Orders");
        builder.Services
            .AddBaseLogging()
            .AddBaseMySql<ItemContext>(configuration)
            .AddAzureBlobStoring(configuration);
        using IHost host = builder.Build();
        await host.StartAsync();
        await host.Services.EnsureDatabaseCreatedAsync<ItemContext>();

        await using (AsyncServiceScope scope = host.Services.CreateAsyncScope())
        {
            ILogger logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Acme.Orders.Checkout");
            ItemContext context = scope.ServiceProvider.GetRequiredService<ItemContext>();
            context.Items.Add(new Item { Name = "lamp" });
            await context.SaveChangesAsync();
            await scope.ServiceProvider.GetRequiredService<IBlobStore>().SaveAsync("invoices", "lamp.txt", new MemoryStream(Encoding.UTF8.GetBytes("1 lamp")), "text/plain");
            logger.LogInformation("Order {Item} was stored", "lamp");
        }

        await using (AsyncServiceScope scope = host.Services.CreateAsyncScope())
        {
            Assert.Equal("lamp", await scope.ServiceProvider.GetRequiredService<ItemContext>().Items.Select(item => item.Name).SingleAsync());
            await using Stream? blob = await scope.ServiceProvider.GetRequiredService<IBlobStore>().OpenReadAsync("invoices", "lamp.txt");
            Assert.Equal("1 lamp", await new StreamReader(blob!).ReadToEndAsync());
        }

        Assert.Equal(HealthStatus.Healthy, (await Support.ReadyReportAsync(host.Services)).Status);
        await host.StopAsync();
        host.Dispose();

        Assert.Contains("Acme.Orders [Acme.Orders.Checkout] | Order lamp was stored", await File.ReadAllTextAsync(logFile), StringComparison.Ordinal);
    }
}
