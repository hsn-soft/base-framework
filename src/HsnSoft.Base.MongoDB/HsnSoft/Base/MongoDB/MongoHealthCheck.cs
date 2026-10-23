using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HsnSoft.Base.MongoDB;

internal sealed class MongoHealthCheck(IMongoDatabase database) : IHealthCheck
{
    private readonly IMongoDatabase _database = database;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        await _database.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1), cancellationToken: cancellationToken).ConfigureAwait(false);
        return HealthCheckResult.Healthy();
    }
}
