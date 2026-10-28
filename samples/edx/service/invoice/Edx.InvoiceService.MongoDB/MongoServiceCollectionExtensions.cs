using Edx.InvoiceService.Domain.InvoiceDomain.Consts;
using Edx.InvoiceService.Domain.InvoiceDomain.Entities;
using Edx.InvoiceService.Domain.InvoiceDomain.Repositories;
using Edx.InvoiceService.Domain.InvoiceReportDomain.Consts;
using Edx.InvoiceService.Domain.InvoiceReportDomain.Entities;
using Edx.InvoiceService.MongoDB.Configurations;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.MongoDB;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Edx.InvoiceService.MongoDB;

public static class MongoServiceCollectionExtensions
{
    /// <summary>
    /// Registers what the invoice service needs of MongoDB: the connection (<c>AddBaseMongo</c>, <c>ConnectionStrings:Mongo-InvoiceService</c>, and its readiness check), the tenants of the documents, the mapping and the repository.
    /// </summary>
    public static IServiceCollection AddInvoicePersistence(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddBaseMongo(configuration, MongoInvoiceDbProperties.ConnectionStringName);
        services.AddBaseMultiTenancyMongo();

        InvoiceClassMap.Register();
        InvoiceReportClassMap.Register();
        services.AddScoped<IInvoiceRepository>(provider => new MongoInvoiceRepository(
            provider.GetRequiredService<IMongoDatabase>(), provider.GetRequiredService<MongoEntityConcepts>(), MongoInvoiceDbProperties.CollectionPrefix + InvoiceConsts.CollectionName));
        services.AddScoped<IRepository<Invoice, Guid>>(provider => provider.GetRequiredService<IInvoiceRepository>());
        services.AddScoped<IRepository<InvoiceReport, Guid>>(provider => new MongoRepository<InvoiceReport, Guid>(
            provider.GetRequiredService<IMongoDatabase>(), provider.GetRequiredService<MongoEntityConcepts>(), MongoInvoiceDbProperties.CollectionPrefix + InvoiceReportConsts.CollectionName));
        return services;
    }
}
