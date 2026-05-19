using System.Globalization;
using System.Net;
using Hhs.AdministrationService.Application.Contracts.PermissionDomain.Dtos;
using Hhs.AdministrationService.Application.Contracts.PermissionDomain.Dtos.Filters;
using Hhs.AdministrationService.Application.Contracts.PermissionDomain.Dtos.Submits;
using Hhs.AdministrationService.Application.Contracts.PermissionDomain.Services;
using Hhs.AdministrationService.Domain.PermissionDomain.Consts;
using Hhs.AdministrationService.Domain.PermissionDomain.Entities;
using Hhs.AdministrationService.Domain.PermissionDomain.Repositories;
using HsnSoft.Base;
using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Domain.Models;
using HsnSoft.Base.Reflection;

namespace Hhs.AdministrationService.Application.Services;

public sealed class PermissionGrantAppService(
    IServiceProvider provider,
    IPermissionGrantRepository permissionGrantRepository
) : ApplicationServiceBase(provider), IPermissionGrantAppService
{
    public async Task<PermissionGrantDto> GetAsync(Guid id)
    {
        if (id == Guid.Empty)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        var item = await permissionGrantRepository.GetSingleOrDefaultAsync<PermissionGrantDto>(x => x.Id == id, Mapper.ConfigurationProvider);
        return item ?? throw new BaseHttpException((int)HttpStatusCode.NotFound);
    }

    public async Task<PagedDataResultDto<PermissionGrantDto>> GetPagedListAsync(GetPermissionGrantsPaged pagedInput)
    {
        if (pagedInput == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        pagedInput.Name = pagedInput.Name?.ToLower(new CultureInfo("en-US"));
        pagedInput.ProviderName = pagedInput.ProviderName?.ToLower(new CultureInfo("en-US"));
        pagedInput.ProviderKey = pagedInput.ProviderKey?.ToLower(new CultureInfo("en-US"));

        var filter = new FilterBuilder<PermissionGrant>()
            .And(!string.IsNullOrWhiteSpace(pagedInput.Name) ? e => e.Name.ToLower(new CultureInfo("en-US")).Contains(pagedInput.Name) : null)
            .And(!string.IsNullOrWhiteSpace(pagedInput.ProviderName) ? e => e.ProviderName.ToLower(new CultureInfo("en-US")).Contains(pagedInput.ProviderName) : null)
            .And(!string.IsNullOrWhiteSpace(pagedInput.ProviderKey) ? e => e.ProviderKey.ToLower(new CultureInfo("en-US")).Contains(pagedInput.ProviderKey) : null)
            .Build();

        var result = await permissionGrantRepository.GetPageListAsync<PermissionGrantDto>(
            options: new PagedQueryOptions<PermissionGrant>
            {
                Filter = filter,
                OrderByDynamic = string.IsNullOrWhiteSpace(pagedInput.SortingText)
                    ? PermissionGrantConsts.GetDefaultSorting()
                    : pagedInput.SortingText,
                PageNumber = pagedInput.PageNumber,
                MaxResultCount = pagedInput.MaxResultCount
            }, Mapper.ConfigurationProvider);

        return new PagedDataResultDto<PermissionGrantDto>(result.TotalCount, pagedInput.PageNumber, pagedInput.MaxResultCount, result.Items);
    }

    public async Task<List<PermissionGrantDto>> GetFilterListAsync(GetPermissionGrantsFilter filterInput)
    {
        if (filterInput == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        filterInput.Name = filterInput.Name?.ToLower(new CultureInfo("en-US"));
        filterInput.ProviderName = filterInput.ProviderName?.ToLower(new CultureInfo("en-US"));
        filterInput.ProviderKey = filterInput.ProviderKey?.ToLower(new CultureInfo("en-US"));

        var filter = new FilterBuilder<PermissionGrant>()
            .And(!string.IsNullOrWhiteSpace(filterInput.Name) ? e => e.Name.ToLower(new CultureInfo("en-US")).Contains(filterInput.Name) : null)
            .And(!string.IsNullOrWhiteSpace(filterInput.ProviderName) ? e => e.ProviderName.ToLower(new CultureInfo("en-US")).Contains(filterInput.ProviderName) : null)
            .And(!string.IsNullOrWhiteSpace(filterInput.ProviderKey) ? e => e.ProviderKey.ToLower(new CultureInfo("en-US")).Contains(filterInput.ProviderKey) : null)
            .Build();

        return await permissionGrantRepository.GetListAsync<PermissionGrantDto>(
            options: new ListQueryOptions<PermissionGrant>
            {
                Filter = filter,
                OrderByDynamic = string.IsNullOrWhiteSpace(filterInput.SortingText)
                    ? PermissionGrantConsts.GetDefaultSorting()
                    : filterInput.SortingText
            }, Mapper.ConfigurationProvider);
    }

    public async Task<List<PermissionGrantSearchDto>> GetSearchListAsync(GetPermissionGrantsSearch searchInput)
    {
        if (searchInput == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        searchInput.SearchText = searchInput.SearchText?.ToLower(new CultureInfo("en-US"));

        var filter = new FilterBuilder<PermissionGrant>()
            .And(!string.IsNullOrWhiteSpace(searchInput.SearchText)
                ? e => e.Name.ToLower(new CultureInfo("en-US")).Contains(searchInput.SearchText)
                       || e.ProviderName.ToLower(new CultureInfo("en-US")).Contains(searchInput.SearchText)
                       || e.ProviderKey.ToLower(new CultureInfo("en-US")).Contains(searchInput.SearchText)
                : null)
            .Build();

        return await permissionGrantRepository.GetListAsync<PermissionGrantSearchDto>(
            options: new ListQueryOptions<PermissionGrant>
            {
                Filter = filter,
                OrderByDynamic = string.IsNullOrWhiteSpace(searchInput.SortingText)
                    ? PermissionGrantConsts.GetDefaultSorting()
                    : searchInput.SortingText,
                MaxResultCount = searchInput.MaxResultCount
            }, Mapper.ConfigurationProvider);
    }

    public async Task<PermissionGrantDto> CreateAsync(PermissionGrantCreateDto input)
    {
        if (input == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        var permissionGrant = await permissionGrantRepository.CreateAsync(
            name: input.Name ?? string.Empty,
            providerName: input.ProviderName ?? string.Empty,
            providerKey: input.ProviderKey ?? string.Empty
        );

        //INTEGRATION EVENT TRIGGER
        //
        //

        return Mapper.Map<PermissionGrant, PermissionGrantDto>(permissionGrant);
    }

    public async Task UpdateAsync(PermissionGrantUpdateDto input)
    {
        if (input == null || input.Id == Guid.Empty)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        await permissionGrantRepository.UpdateAsync(
            id: input.Id,
            name: input.Name ?? string.Empty,
            providerName: input.ProviderName ?? string.Empty,
            providerKey: input.ProviderKey ?? string.Empty
        );

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

        await permissionGrantRepository.RemoveAsync(id);

        //INTEGRATION EVENT TRIGGER
        //
        //
    }
}