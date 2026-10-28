using Edx.Integration.GibInvoice.Domain.DirectoryDomain.Entities;
using Edx.Integration.GibInvoice.Domain.DirectoryDomain.Repositories;
using HsnSoft.Base.MongoDB;
using MongoDB.Driver;

namespace Edx.Integration.GibInvoice.MongoDB;

/// <summary>The list of the registered users in MongoDB: what <see cref="MongoRepository{TEntity, TKey}"/> does for any entity, and the replacement of the whole list.</summary>
public sealed class MongoExchangeUserRepository(IMongoDatabase database, MongoEntityConcepts concepts, string collectionName)
    : MongoRepository<ExchangeUser, Guid>(database, concepts, collectionName), IExchangeUserRepository
{
    public Task<ExchangeUser?> FindByRegisterNumberAsync(string registerNumber, CancellationToken cancellationToken = default)
        => FindAsync(user => user.RegisterNumber == registerNumber, cancellationToken);

    public async Task ReplaceAllAsync(IReadOnlyCollection<ExchangeUser> users, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(users);

        // the new list is written first, so a reader is never left with an empty one: users that are gone are taken out after it
        HashSet<string> numbers = [.. users.Select(user => user.RegisterNumber)];
        foreach (ExchangeUser user in users)
        {
            ExchangeUser? existing = await FindByRegisterNumberAsync(user.RegisterNumber, cancellationToken).ConfigureAwait(false);
            if (existing is null)
            {
                await AddAsync(user, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                existing.Refresh(user.Title, user.PostBoxAliases, user.RefreshedAt);
                await UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
            }
        }

        await Collection.DeleteManyAsync(Builders<ExchangeUser>.Filter.Nin(item => item.RegisterNumber, numbers), cancellationToken).ConfigureAwait(false);
    }
}
