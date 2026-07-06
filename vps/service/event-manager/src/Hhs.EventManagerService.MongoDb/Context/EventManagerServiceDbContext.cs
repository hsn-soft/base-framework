using Hhs.EventManagerService.Domain.EventDomain.Entities;
using Hhs.Shared.Helper.EventInbox;
using HsnSoft.Base.MongoDB;
using HsnSoft.Base.MongoDB.Context;
using Microsoft.Extensions.Configuration;

namespace Hhs.EventManagerService.MongoDb.Context;

public sealed class EventManagerServiceDbContext(IServiceProvider provider, IConfiguration configuration) : BaseMongoDbContext(configuration.GetConnectionString(MongoDbProperties.ConnectionStringName), provider)
{
    public ITrackingMongoCollection<FailedIntegrationEvent> FailedIntegrationEvents => GetCollection<FailedIntegrationEvent>();
    public ITrackingMongoCollection<EventInboxMessage> EventInboxMessages => GetCollection<EventInboxMessage>();
}