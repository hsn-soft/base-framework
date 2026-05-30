using Hhs.ContentService.Domain.ClientDomain.Consts;
using Hhs.ContentService.Domain.ClientDomain.Entities;
using Hhs.ContentService.Domain.ClientDomain.Exceptions;
using Hhs.ContentService.Domain.ClientDomain.Repositories;
using Hhs.ContentService.Domain.Enums;
using Hhs.ContentService.Domain.Localization;
using Hhs.ContentService.EntityFrameworkCore.Context;
using Hhs.Shared.Localization;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.Validation.Localization;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Hhs.ContentService.EntityFrameworkCore.Repositories;

public sealed class EfCoreClientRepository(IServiceProvider provider, IStringLocalizerFactory stringLocalizerFactory, ContentServiceDbContext dbContext)
    : EfCoreGenericRepository<Client, Guid>(provider, dbContext), IClientRepository
{
    [NotNull] protected IStringLocalizer L { get; } = stringLocalizerFactory.CreateMultiple([typeof(ContentServiceResource), typeof(ValidationResource), typeof(SharedResource)]);

    public async Task<Client> CreateAsync(
        Guid tenantId,
        string domainName,
        List<string> includePathFilters = null,
        List<string> excludePathFilters = null)
        => await CreateAsync(id: Guid.CreateVersion7(),
            tenantId: tenantId,
            domainName: domainName,
            includePathFilters: includePathFilters,
            excludePathFilters: excludePathFilters);

    public async Task<Client> CreateAsync(
        Guid id,
        Guid tenantId,
        string domainName,
        List<string> includePathFilters = null,
        List<string> excludePathFilters = null)
    {
        if (id == Guid.Empty) id = Guid.CreateVersion7();

        // Create draft
        var draft = new Client(
            id: id,
            tenantId: tenantId,
            domainName: domainName
        );

        var filters = new List<ClientPathFilter>();
        if (includePathFilters is { Count: > 0 })
        {
            filters.AddRange(includePathFilters.Select(x => new ClientPathFilter(id: Guid.CreateVersion7(), clientId: id, pathFilterName: x, clientFilterType: ClientFilterTypes.IncludeFilter)).ToList());
        }

        if (excludePathFilters is { Count: > 0 })
        {
            filters.AddRange(excludePathFilters.Select(x => new ClientPathFilter(id: Guid.CreateVersion7(), clientId: id, pathFilterName: x, clientFilterType: ClientFilterTypes.ExcludeFilter)).ToList());
        }

        if (filters is { Count: > 0 })
        {
            draft.PathFilters = filters;
        }

        //Domain Rules
        await ClientDuplicateControlAsync(draft.Id, draft.DomainName);

        _ = await InsertAsync(draft);
        return draft;
    }

    public async Task<Client> UpdateAsync(Guid id,
        string domainName,
        bool isBlocked = false,
        List<string> includePathFilters = null,
        List<string> excludePathFilters = null)
    {
        var oldEntity = await GetSingleOrDefaultAsync(x => x.Id == id, includeEntity: x => x.Include(t => t.PathFilters));
        if (oldEntity == null)
        {
            throw new ClientNotFoundException(L, id.ToString());
        }

        oldEntity.SetDomainName(domainName);
        oldEntity.IsBlocked = isBlocked;

        var filters = new List<ClientPathFilter>();
        if (includePathFilters is { Count: > 0 })
        {
            filters.AddRange(includePathFilters.Select(x => new ClientPathFilter(id: Guid.CreateVersion7(), clientId: id, pathFilterName: x, clientFilterType: ClientFilterTypes.IncludeFilter)).ToList());
        }

        if (excludePathFilters is { Count: > 0 })
        {
            filters.AddRange(excludePathFilters.Select(x => new ClientPathFilter(id: Guid.CreateVersion7(), clientId: id, pathFilterName: x, clientFilterType: ClientFilterTypes.ExcludeFilter)).ToList());
        }

        oldEntity.PathFilters = filters is { Count: > 0 } ? filters : [];

        //Domain Rules
        // Rule01
        // Rule02
        _ = await UpdateAsync(oldEntity);
        return oldEntity;
    }

    public async Task RemoveAsync(Guid id)
    {
        var entity = await GetSingleOrDefaultAsync(x => x.Id == id, includeEntity: x => x.Include(t => t.PathFilters));
        if (entity == null)
        {
            throw new ClientNotFoundException(L, id.ToString());
        }

        // Domain Rule -> Client Dependency Control for Delete
        //  Rule01

        string guidGenerated = Guid.CreateVersion7().ToString("N").ToUpper();
        string uniqueField = guidGenerated + "_" + entity.DomainName;
        if (uniqueField.Length > ClientConsts.DomainNameMaxLength)
        {
            uniqueField = uniqueField[..ClientConsts.DomainNameMaxLength];
        }

        entity.SetDomainName(uniqueField);
        entity.IsDeleted = true;
        entity.PathFilters = [];

        await UpdateAsync(entity);
    }

    private async Task ClientDuplicateControlAsync(Guid clientId, [NotNull] string domainName)
    {
        var old = await GetSingleOrDefaultAsync(x => x.Id == clientId || x.DomainName == domainName);
        if (old != null)
        {
            throw new ClientDuplicateException(L, old.Id.ToString()).WithData(nameof(domainName), domainName);
        }
    }
}