using Edx.InvoiceService.Domain.InvoiceDomain.Consts;
using Edx.InvoiceService.Domain.InvoiceDomain.Entities;
using Edx.InvoiceService.Domain.InvoiceReportDomain.Consts;
using Edx.InvoiceService.Domain.InvoiceReportDomain.Entities;
using HsnSoft.Base.Hosting;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Edx.InvoiceService.MongoDB;

public static class InvoiceDatabase
{
    /// <summary>Makes the indexes: the uuid and the number of the live record of an invoice are unique in a tenant, the records are looked up by tenant, newest first. MongoDB creates the database and the collections when the first document is written.</summary>
    public static Task EnsureInvoiceDatabaseAsync(this IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.RunStartupStepAsync("Invoice collections", async cancellationToken =>
        {
            IMongoDatabase database = services.GetRequiredService<IMongoDatabase>();

            // At most one record of an invoice is live (being processed, a draft, approved or signed), by its uuid and by its number, in a tenant; a failed or a rejected record, and a deleted one, are not in these indexes, so they stay
            // and the same uuid and the same number can come again. The store decides when two arrive at the same time (the one that loses gets a duplicate key).
            FilterDefinition<Invoice> live = Builders<Invoice>.Filter.And(Builders<Invoice>.Filter.Eq(invoice => invoice.IsLive, true), Builders<Invoice>.Filter.Eq(invoice => invoice.IsDeleted, false));
            await database.GetCollection<Invoice>(MongoInvoiceDbProperties.CollectionPrefix + InvoiceConsts.CollectionName).Indexes.CreateManyAsync(
                [
                    new CreateIndexModel<Invoice>(
                        Builders<Invoice>.IndexKeys.Ascending(invoice => invoice.TenantId).Ascending(invoice => invoice.InvoiceId),
                        new CreateIndexOptions<Invoice> { Unique = true, Name = "uq_live_invoice_id", PartialFilterExpression = live }),
                    new CreateIndexModel<Invoice>(
                        Builders<Invoice>.IndexKeys.Ascending(invoice => invoice.TenantId).Ascending(invoice => invoice.NormalizedNumber),
                        new CreateIndexOptions<Invoice> { Unique = true, Name = "uq_live_invoice_number", PartialFilterExpression = live }),
                    new CreateIndexModel<Invoice>(Builders<Invoice>.IndexKeys.Ascending(invoice => invoice.TenantId).Descending(invoice => invoice.StatusChangedAt)),
                    new CreateIndexModel<Invoice>(Builders<Invoice>.IndexKeys.Ascending(invoice => invoice.TenantId).Ascending(invoice => invoice.InvoiceId).Descending(invoice => invoice.StatusChangedAt)),
                ],
                cancellationToken).ConfigureAwait(false);

            // the reports are listed by tenant, newest first
            await database.GetCollection<InvoiceReport>(MongoInvoiceDbProperties.CollectionPrefix + InvoiceReportConsts.CollectionName).Indexes.CreateOneAsync(
                new CreateIndexModel<InvoiceReport>(Builders<InvoiceReport>.IndexKeys.Ascending(report => report.TenantId).Descending(report => report.GeneratedAt)),
                cancellationToken: cancellationToken).ConfigureAwait(false);
        });
    }
}
