using System.Globalization;
using Hhs.AdministrationService.Domain.Localization;
using Hhs.AdministrationService.Domain.PermissionDomain.Entities;
using Hhs.AdministrationService.Domain.PermissionDomain.Exceptions;
using Hhs.AdministrationService.Domain.PermissionDomain.Repositories;
using Hhs.AdministrationService.EntityFrameworkCore.Context;
using Hhs.Shared.Localization;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.Reflection;
using HsnSoft.Base.Validation.Localization;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Hhs.AdministrationService.EntityFrameworkCore.Repositories;

public sealed class EfCorePermissionGrantRepository(
    IServiceProvider serviceProvider,
    IStringLocalizerFactory stringLocalizerFactory,
    AdministrationServiceDbContext dbContext
) : EfCoreGenericRepository<PermissionGrant, Guid>(serviceProvider, dbContext), IPermissionGrantRepository
{
    [NotNull]
    private IStringLocalizer L { get; } = stringLocalizerFactory.CreateMultiple([
        typeof(AdministrationServiceResource),
        typeof(ValidationResource),
        typeof(SharedResource)
    ]);

    public async Task<List<PermissionGrant>> GetSessionPermissionsAsync(string clientKey = null, string[] roleKeys = null, string userKey = null, CancellationToken cancellationToken = default)
    {
        var result = new List<PermissionGrant>();

        if (roleKeys is { Length: > 0 })
        {
            var queryR = GetQueryable()
                .Where(e => e.ProviderName.Equals("R") && roleKeys.Contains(e.ProviderKey));

            result.AddRange(await queryR.ToListAsync(cancellationToken));
        }
        else if (!string.IsNullOrWhiteSpace(clientKey))
        {
            var queryC = GetQueryable()
                .Where(e => e.ProviderName.Equals("C") && e.ProviderKey.Equals(clientKey));

            result.AddRange(await queryC.ToListAsync(cancellationToken));
        }

        if (!string.IsNullOrWhiteSpace(userKey))
        {
            var queryU = GetQueryable()
                .Where(e => e.ProviderName.Equals("U") && e.ProviderKey.Equals(userKey));

            result.AddRange(await queryU.ToListAsync(cancellationToken));
        }

        return result;
    }

    public async Task<PermissionGrant> CreateAsync(string name, string providerName, string providerKey)
        => await CreateAsync(id: Guid.NewGuid(),
            name: name,
            providerName: providerName,
            providerKey: providerKey);

    public async Task<PermissionGrant> CreateAsync(Guid id, string name, string providerName, string providerKey)
    {
        if (id == Guid.Empty) id = Guid.NewGuid();

        // Create draft
        var draft = new PermissionGrant(
            id: id,
            name: name,
            providerName: providerName,
            providerKey: providerKey
        );

        //Domain Rules
        if (await ExistsAsync(x => x.Name == draft.Name && x.ProviderName == draft.ProviderName && x.ProviderKey == draft.ProviderKey))
        {
            throw new PermissionGrantDuplicateException(L, draft.Name, draft.ProviderName, draft.ProviderKey);
        }

        _ = await InsertAsync(draft);
        return draft;
    }

    public async Task<PermissionGrant> UpdateAsync(Guid id, string name, string providerName, string providerKey)
    {
        var oldPermissionGrant = await GetByIdOrDefaultAsync(id);
        if (oldPermissionGrant == null)
        {
            throw new PermissionGrantNotFoundException(L, id.ToString());
        }

        oldPermissionGrant.SetName(name);
        oldPermissionGrant.SetProviderName(providerName);
        oldPermissionGrant.SetProviderKey(providerKey);

        //Domain Rule -> PermissionGrant must be unique
        if (await ExistsAsync(x => x.Id != oldPermissionGrant.Id && x.Name == name && x.ProviderName == providerName && x.ProviderKey == providerKey))
        {
            throw new PermissionGrantDuplicateException(L, name, providerName, providerKey);
        }

        _ = await UpdateAsync(oldPermissionGrant);
        return oldPermissionGrant;
    }

    public async Task SetManyAsync(List<string> names, string providerName, string providerKey)
    {
        if (string.IsNullOrWhiteSpace(providerName) && string.IsNullOrWhiteSpace(providerKey))
        {
            throw new PermissionGrantFilterException(L);
        }

        var cleaningFilter = new FilterBuilder<PermissionGrant>()
            .And(!string.IsNullOrWhiteSpace(providerName) ? e => e.ProviderName.ToLower(new CultureInfo("en-US")).Contains(providerName) : null)
            .And(!string.IsNullOrWhiteSpace(providerKey) ? e => e.ProviderKey.ToLower(new CultureInfo("en-US")).Contains(providerKey) : null)
            .Build();

        // Clear Permissions
        await DeleteManyAsync(cleaningFilter);

        if (names.Count > 0)
        {
            // Create new Permission list
            var permissionGrants = names.Select(x => new PermissionGrant(
                Guid.NewGuid(),
                name: x,
                providerName,
                providerKey)
            ).ToList();

            await InsertManyAsync(permissionGrants);
        }
    }

    public async Task RemoveAsync(Guid id)
    {
        if (await ExistsAsync(x => x.Id == id))
        {
            throw new PermissionGrantNotFoundException(L, id.ToString());
        }

        // Hard delete
        await DeleteByIdAsync(id);
    }
}