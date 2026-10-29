using Edx.InvoiceService.Domain.InvoiceDomain.Consts;
using Edx.InvoiceService.Domain.InvoiceDomain.Entities;
using Edx.InvoiceService.Domain.InvoiceDomain.Repositories;
using Edx.InvoiceService.Domain.InvoiceReportDomain.Consts;
using Edx.InvoiceService.Domain.InvoiceReportDomain.Entities;
using Edx.InvoiceService.Domain.PurchaseInvoiceDomain.Consts;
using Edx.InvoiceService.Domain.PurchaseInvoiceDomain.Entities;
using Edx.InvoiceService.Domain.PurchaseInvoiceDomain.Repositories;
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

        SalesInvoiceClassMap.Register();
        InvoiceReportClassMap.Register();
        PurchaseInvoiceClassMap.Register();
        services.AddScoped<ISalesInvoiceRepository>(provider => new MongoSalesInvoiceRepository(
            provider.GetRequiredService<IMongoDatabase>(), provider.GetRequiredService<MongoEntityConcepts>(), MongoInvoiceDbProperties.CollectionPrefix + InvoiceConsts.CollectionName));
        services.AddScoped<IRepository<SalesInvoice, Guid>>(provider => provider.GetRequiredService<ISalesInvoiceRepository>());
        services.AddScoped<IPurchaseInvoiceRepository>(provider => new MongoPurchaseInvoiceRepository(
            provider.GetRequiredService<IMongoDatabase>(), provider.GetRequiredService<MongoEntityConcepts>(), MongoInvoiceDbProperties.CollectionPrefix + PurchaseInvoiceConsts.CollectionName));
        services.AddScoped<IRepository<PurchaseInvoice, Guid>>(provider => provider.GetRequiredService<IPurchaseInvoiceRepository>());
        services.AddScoped<IRepository<InvoiceReport, Guid>>(provider => new MongoRepository<InvoiceReport, Guid>(
            provider.GetRequiredService<IMongoDatabase>(), provider.GetRequiredService<MongoEntityConcepts>(), MongoInvoiceDbProperties.CollectionPrefix + InvoiceReportConsts.CollectionName));
        return services;
    }
}
