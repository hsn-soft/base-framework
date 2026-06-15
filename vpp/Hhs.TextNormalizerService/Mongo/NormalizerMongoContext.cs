using Hhs.TextNormalizerService.Entities;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Hhs.TextNormalizerService.Mongo;

public sealed class NormalizerMongoContext
{
    private readonly IMongoDatabase _database;

    public NormalizerMongoContext(IOptions<MongoOptions> options)
    {
        var client = new MongoClient(options.Value.ConnectionString);
        _database = client.GetDatabase(options.Value.DatabaseName);
    }

    public IMongoCollection<CustomerContentNormalizedRequest> CustomerRequests =>
        _database.GetCollection<CustomerContentNormalizedRequest>("customer_content_normalized_requests");

    public IMongoCollection<AnalysisContentNormalizedRequest> AnalysisRequests =>
        _database.GetCollection<AnalysisContentNormalizedRequest>("analysis_content_normalized_requests");

    public IMongoCollection<NormalizerInboxMessage> InboxMessages =>
        _database.GetCollection<NormalizerInboxMessage>("inbox_messages");
}