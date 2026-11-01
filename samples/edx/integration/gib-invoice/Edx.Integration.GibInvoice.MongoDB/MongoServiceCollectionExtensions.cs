using Edx.Integration.GibInvoice.Domain.DirectoryDomain.Entities;
using Edx.Integration.GibInvoice.Domain.DirectoryDomain.Repositories;
using Edx.Integration.GibInvoice.Domain.EnvelopeDomain.Consts;
using Edx.Integration.GibInvoice.Domain.EnvelopeDomain.Entities;
using Edx.Integration.GibInvoice.Domain.EnvelopeDomain.Repositories;
using Edx.Integration.GibInvoice.MongoDB.Configurations;
using HsnSoft.Base.Data;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.MongoDB;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Edx.Integration.GibInvoice.MongoDB;

public static class MongoServiceCollectionExtensions
{
    /// <summary>
    /// Registers what the integration service needs of MongoDB: the connection (<c>AddBaseMongo</c>, <c>ConnectionStrings:Mongo-IntegrationService</c>, and its readiness check), the tenants of the documents, the mapping and the repositories.
    /// </summary>
    public static IServiceCollection AddIntegrationPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddBaseMongo(configuration, MongoIntegrationDbProperties.ConnectionStringName);
        services.AddBaseMultiTenancyMongo();

        IntegrationClassMaps.Register();
        services.AddScoped<IOutboundEnvelopeRepository>(provider => new MongoOutboundEnvelopeRepository(
            provider.GetRequiredService<IMongoDatabase>(), provider.GetRequiredService<MongoEntityConcepts>(), MongoIntegrationDbProperties.CollectionPrefix + EnvelopeConsts.OutboundCollectionName));
        services.AddScoped<IRepository<OutboundEnvelope, Guid>>(provider => provider.GetRequiredService<IOutboundEnvelopeRepository>());
        services.AddScoped<IInboundEnvelopeRepository>(provider => new MongoInboundEnvelopeRepository(
            provider.GetRequiredService<IMongoDatabase>(), provider.GetRequiredService<MongoEntityConcepts>(), MongoIntegrationDbProperties.CollectionPrefix + EnvelopeConsts.InboundCollectionName));
        services.AddScoped<IRepository<InboundEnvelope, Guid>>(provider => provider.GetRequiredService<IInboundEnvelopeRepository>());
        services.AddScoped<IExchangeUserRepository>(provider => new MongoExchangeUserRepository(
            provider.GetRequiredService<IMongoDatabase>(), provider.GetRequiredService<MongoEntityConcepts>(), MongoIntegrationDbProperties.CollectionPrefix + MongoIntegrationDbProperties.ExchangeUserCollectionName));
        services.AddScoped<IRepository<ExchangeUser, Guid>>(provider => provider.GetRequiredService<IExchangeUserRepository>());
        services.AddDatabaseStartupStep(MongoIntegrationDbProperties.ConnectionStringName, "Integration collections", EnsureIntegrationCollections);
        return services;
    }

    /// <summary>
    /// Makes the indexes: an envelope is known by its uuid, once (unique, in each direction), the workers find theirs by state, the invoices that were handed over by the record of the invoice service, and a user of the list by its register number (unique).
    /// MongoDB creates the database and the collections when the first document is written.
    /// </summary>
    internal static async Task EnsureIntegrationCollections(IServiceProvider services, CancellationToken cancellationToken)
    {
        IMongoDatabase database = services.GetRequiredService<IMongoDatabase>();

        await database.GetCollection<OutboundEnvelope>(MongoIntegrationDbProperties.CollectionPrefix + EnvelopeConsts.OutboundCollectionName).Indexes.CreateManyAsync(
            [
                new CreateIndexModel<OutboundEnvelope>(Builders<OutboundEnvelope>.IndexKeys.Ascending(envelope => envelope.EnvelopeId), new CreateIndexOptions { Unique = true }),
                new CreateIndexModel<OutboundEnvelope>(Builders<OutboundEnvelope>.IndexKeys.Ascending(envelope => envelope.State).Ascending(envelope => envelope.NextSendAt)),
                new CreateIndexModel<OutboundEnvelope>(Builders<OutboundEnvelope>.IndexKeys.Ascending(envelope => envelope.TenantId).Descending(envelope => envelope.CreatedAt)),
                new CreateIndexModel<OutboundEnvelope>(Builders<OutboundEnvelope>.IndexKeys.Ascending(envelope => envelope.InvoiceRecordId)),
            ],
            cancellationToken).ConfigureAwait(false);

        await database.GetCollection<InboundEnvelope>(MongoIntegrationDbProperties.CollectionPrefix + EnvelopeConsts.InboundCollectionName).Indexes.CreateManyAsync(
            [
                new CreateIndexModel<InboundEnvelope>(Builders<InboundEnvelope>.IndexKeys.Ascending(envelope => envelope.EnvelopeId), new CreateIndexOptions { Unique = true }),
                new CreateIndexModel<InboundEnvelope>(Builders<InboundEnvelope>.IndexKeys.Ascending(envelope => envelope.State).Ascending(envelope => envelope.ReceivedAt)),
                new CreateIndexModel<InboundEnvelope>(Builders<InboundEnvelope>.IndexKeys.Ascending(envelope => envelope.TenantId).Descending(envelope => envelope.ReceivedAt)),
                new CreateIndexModel<InboundEnvelope>(Builders<InboundEnvelope>.IndexKeys.Ascending(envelope => envelope.InvoiceUuid)),
            ],
            cancellationToken).ConfigureAwait(false);

        await database.GetCollection<ExchangeUser>(MongoIntegrationDbProperties.CollectionPrefix + MongoIntegrationDbProperties.ExchangeUserCollectionName).Indexes.CreateOneAsync(
            new CreateIndexModel<ExchangeUser>(Builders<ExchangeUser>.IndexKeys.Ascending(user => user.RegisterNumber), new CreateIndexOptions { Unique = true }),
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}
