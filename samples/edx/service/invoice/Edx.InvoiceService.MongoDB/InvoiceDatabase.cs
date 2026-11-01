using Edx.InvoiceService.Domain.InvoiceDomain.Consts;
using Edx.InvoiceService.Domain.InvoiceDomain.Entities;
using Edx.InvoiceService.Domain.InvoiceReportDomain.Consts;
using Edx.InvoiceService.Domain.InvoiceReportDomain.Entities;
using Edx.InvoiceService.Domain.PurchaseInvoiceDomain.Consts;
using Edx.InvoiceService.Domain.PurchaseInvoiceDomain.Entities;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Edx.InvoiceService.MongoDB;

public static class InvoiceDatabase
{
    /// <summary>Makes the indexes: the uuid and the number of the live record of an invoice are unique in a tenant, the records are looked up by tenant, newest first. MongoDB creates the database and the collections when the first document is written.</summary>
    internal static async Task EnsureInvoiceCollections(IServiceProvider services, CancellationToken cancellationToken)
    {
        IMongoDatabase database = services.GetRequiredService<IMongoDatabase>();

        // At most one record of an invoice is live (being processed, a draft, approved or signed), by its uuid and by its number, in a tenant; a failed or a rejected record, and a deleted one, are not in these indexes, so they stay
        // and the same uuid and the same number can come again. The store decides when two arrive at the same time (the one that loses gets a duplicate key).
        FilterDefinition<SalesInvoice> live = Builders<SalesInvoice>.Filter.And(Builders<SalesInvoice>.Filter.Eq(invoice => invoice.IsLive, true), Builders<SalesInvoice>.Filter.Eq(invoice => invoice.IsDeleted, false));
        await database.GetCollection<SalesInvoice>(MongoInvoiceDbProperties.CollectionPrefix + InvoiceConsts.CollectionName).Indexes.CreateManyAsync(
            [
                new CreateIndexModel<SalesInvoice>(
                    Builders<SalesInvoice>.IndexKeys.Ascending(invoice => invoice.TenantId).Ascending(invoice => invoice.InvoiceId),
                    new CreateIndexOptions<SalesInvoice> { Unique = true, Name = "uq_live_invoice_id", PartialFilterExpression = live }),
                new CreateIndexModel<SalesInvoice>(
                    Builders<SalesInvoice>.IndexKeys.Ascending(invoice => invoice.TenantId).Ascending(invoice => invoice.NormalizedNumber),
                    new CreateIndexOptions<SalesInvoice> { Unique = true, Name = "uq_live_invoice_number", PartialFilterExpression = live }),
                new CreateIndexModel<SalesInvoice>(Builders<SalesInvoice>.IndexKeys.Ascending(invoice => invoice.TenantId).Descending(invoice => invoice.StatusChangedAt)),
                new CreateIndexModel<SalesInvoice>(Builders<SalesInvoice>.IndexKeys.Ascending(invoice => invoice.TenantId).Ascending(invoice => invoice.InvoiceId).Descending(invoice => invoice.StatusChangedAt)),
            ],
            cancellationToken).ConfigureAwait(false);

        // a purchase invoice is kept once by its uuid in a tenant; they are listed by tenant, newest first
        await database.GetCollection<PurchaseInvoice>(MongoInvoiceDbProperties.CollectionPrefix + PurchaseInvoiceConsts.CollectionName).Indexes.CreateManyAsync(
            [
                new CreateIndexModel<PurchaseInvoice>(Builders<PurchaseInvoice>.IndexKeys.Ascending(invoice => invoice.TenantId).Ascending(invoice => invoice.InvoiceUuid), new CreateIndexOptions { Unique = true }),
                new CreateIndexModel<PurchaseInvoice>(Builders<PurchaseInvoice>.IndexKeys.Ascending(invoice => invoice.TenantId).Descending(invoice => invoice.ReceivedAt)),
                new CreateIndexModel<PurchaseInvoice>(Builders<PurchaseInvoice>.IndexKeys.Ascending(invoice => invoice.TenantId).Ascending(invoice => invoice.EnvelopeId)),
            ],
            cancellationToken).ConfigureAwait(false);

        // the reports are listed by tenant, newest first
        await database.GetCollection<InvoiceReport>(MongoInvoiceDbProperties.CollectionPrefix + InvoiceReportConsts.CollectionName).Indexes.CreateOneAsync(
            new CreateIndexModel<InvoiceReport>(Builders<InvoiceReport>.IndexKeys.Ascending(report => report.TenantId).Descending(report => report.GeneratedAt)),
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}
