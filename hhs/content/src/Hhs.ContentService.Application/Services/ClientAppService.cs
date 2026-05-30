using System.Net;
using Hhs.ContentService.Application.Contracts.ClientDomain.Dtos;
using Hhs.ContentService.Application.Contracts.ClientDomain.Dtos.Filters;
using Hhs.ContentService.Application.Contracts.ClientDomain.Dtos.Submits;
using Hhs.ContentService.Application.Contracts.ClientDomain.Interfaces;
using Hhs.ContentService.Domain.ClientDomain.Consts;
using Hhs.ContentService.Domain.ClientDomain.Entities;
using Hhs.ContentService.Domain.ClientDomain.Repositories;
using Hhs.Shared.Helper.Utils;
using HsnSoft.Base;
using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Domain.Models;
using HsnSoft.Base.Logging.Abstracts;
using HsnSoft.Base.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace Hhs.ContentService.Application.Services;

public sealed class ClientAppService : ApplicationServiceBase, IClientAppService
{
    private readonly IAppConsoleLogger _logger;
    private readonly IClientRepository _clientRepository;

    public ClientAppService(IServiceProvider provider,
        IClientRepository clientRepository
    ) : base(provider)
    {
        _logger = provider.GetRequiredService<IAppConsoleLogger>();
        _clientRepository = clientRepository;
    }

    public async Task<ClientDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        var item = await _clientRepository.GetSingleOrDefaultAsync<ClientDto>(
            predicate: x => x.Id == id,
            includeEntity: q => q.Include(x => x.PathFilters),
            configuration: Mapper.ConfigurationProvider, cancellationToken: cancellationToken);
        if (item == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.NotFound);
        }

        return item;
    }

    public async Task<PagedDataResultDto<ClientDto>> GetPagedListAsync(GetClientsPaged pagedInput, CancellationToken cancellationToken = default)
    {
        if (pagedInput == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        pagedInput.SearchText = StringOperations.Minimize(StringOperations.ReplaceInvalidChars(pagedInput.SearchText));
        pagedInput.DomainName = StringOperations.Minimize(StringOperations.ReplaceInvalidChars(pagedInput.DomainName));

        var filter = new FilterBuilder<Client>()
            .And(!string.IsNullOrWhiteSpace(pagedInput.SearchText) ? e => e.DomainName.Contains(pagedInput.SearchText) : null)
            .And(pagedInput.TenantId.HasValue ? e => e.TenantId == pagedInput.TenantId.Value : null)
            .And(!string.IsNullOrWhiteSpace(pagedInput.DomainName) ? e => e.DomainName.Contains(pagedInput.DomainName) : null)
            .And(pagedInput.IsBlocked.HasValue ? e => e.IsBlocked == pagedInput.IsBlocked.Value : null)
            .Build();

        var result = await _clientRepository.GetPageListAsync<ClientDto>(
            options: new PagedQueryOptions<Client>
            {
                Filter = filter,
                IncludeEntity = q => q.Include(x => x.PathFilters),
                OrderByDynamic = string.IsNullOrWhiteSpace(pagedInput.SortingText)
                    ? ClientConsts.GetDefaultSorting()
                    : pagedInput.SortingText,
                PageNumber = pagedInput.PageNumber,
                MaxResultCount = pagedInput.MaxResultCount
            }, Mapper.ConfigurationProvider, cancellationToken: cancellationToken);

        return new PagedDataResultDto<ClientDto>(result.TotalCount, pagedInput.PageNumber, pagedInput.MaxResultCount, result.Items);
    }

    public async Task<List<ClientDto>> GetFilterListAsync(GetClientsFilter filterInput, CancellationToken cancellationToken = default)
    {
        if (filterInput == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        filterInput.DomainName = StringOperations.Minimize(StringOperations.ReplaceInvalidChars(filterInput.DomainName));

        var filter = new FilterBuilder<Client>()
            .And(filterInput.TenantId.HasValue ? e => e.TenantId == filterInput.TenantId.Value : null)
            .And(!string.IsNullOrWhiteSpace(filterInput.DomainName) ? e => e.DomainName.Contains(filterInput.DomainName) : null)
            .And(filterInput.IsBlocked.HasValue ? e => e.IsBlocked == filterInput.IsBlocked.Value : null)
            .Build();

        return await _clientRepository.GetListAsync<ClientDto>(
            options: new ListQueryOptions<Client>
            {
                Filter = filter,
                IncludeEntity = q => q.Include(x => x.PathFilters),
                OrderByDynamic = string.IsNullOrWhiteSpace(filterInput.SortingText)
                    ? ClientConsts.GetDefaultSorting()
                    : filterInput.SortingText,
                MaxResultCount = filterInput.MaxResultCount
            }, Mapper.ConfigurationProvider, cancellationToken: cancellationToken);
    }

    public async Task<List<ClientSearchDto>> GetSearchListAsync(GetClientsSearch searchInput, CancellationToken cancellationToken = default)
    {
        if (searchInput == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        searchInput.SearchText = StringOperations.Minimize(StringOperations.ReplaceInvalidChars(searchInput.SearchText));

        var filter = new FilterBuilder<Client>()
            .And(!string.IsNullOrWhiteSpace(searchInput.SearchText) ? e => e.DomainName.Contains(searchInput.SearchText) : null)
            .Build();

        return await _clientRepository.GetListAsync<ClientSearchDto>(
            options: new ListQueryOptions<Client>
            {
                Filter = filter,
                OrderByDynamic = string.IsNullOrWhiteSpace(searchInput.SortingText)
                    ? ClientConsts.GetDefaultSorting()
                    : searchInput.SortingText,
                MaxResultCount = searchInput.MaxResultCount
            }, Mapper.ConfigurationProvider, cancellationToken: cancellationToken);
    }

    public async Task<ClientDto> CreateAsync(ClientCreateDto input)
    {
        if (input == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        var placed = await _clientRepository.CreateAsync(
            tenantId: input.TenantId ?? Guid.Empty,
            domainName: input.DomainName ?? string.Empty,
            includePathFilters: input.IncludePathFilters,
            excludePathFilters: input.ExcludePathFilters
        );

        _logger.LogDebug("{EntityTypeName} created [{EntityTypeId}]", nameof(Client), placed.Id);

        //INTEGRATION EVENT TRIGGER
        //
        //

        return Mapper.Map<Client, ClientDto>(placed);
    }

    public async Task UpdateAsync(ClientUpdateDto input)
    {
        if (input?.Id == null || input.Id == Guid.Empty)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        var updated = await _clientRepository.UpdateAsync(
            id: input.Id.Value,
            domainName: input.DomainName ?? string.Empty,
            isBlocked: input.IsBlocked,
            includePathFilters: input.IncludePathFilters,
            excludePathFilters: input.ExcludePathFilters
        );

        _logger.LogDebug("{EntityTypeName} updated [{EntityTypeId}]", nameof(Client), updated.Id);

        //INTEGRATION EVENT TRIGGER
        //
        //
    }

    public async Task DeleteAsync(Guid id)
    {
        if (id == Guid.Empty)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        await _clientRepository.RemoveAsync(id);

        _logger.LogDebug("{EntityTypeName} removed [{EntityTypeId}]", nameof(Client), id);

        //INTEGRATION EVENT TRIGGER
        //
        //
    }
}