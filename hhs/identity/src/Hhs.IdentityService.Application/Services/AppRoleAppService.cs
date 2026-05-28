using System.Net;
using Hhs.IdentityService.Application.Contracts.AppRoleDomain.Dtos;
using Hhs.IdentityService.Application.Contracts.AppRoleDomain.Dtos.Filters;
using Hhs.IdentityService.Application.Contracts.AppRoleDomain.Dtos.Submits;
using Hhs.IdentityService.Application.Contracts.AppRoleDomain.Services;
using Hhs.IdentityService.Domain.AuthDomain.Consts;
using Hhs.IdentityService.Domain.AuthDomain.Entities;
using Hhs.IdentityService.Domain.AuthDomain.Repositories;
using Hhs.IdentityService.Domain.TenantDomain.Exceptions;
using Hhs.IdentityService.Domain.TenantDomain.Repositories;
using Hhs.Shared.Helper.Utils;
using HsnSoft.Base;
using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Domain.Models;
using HsnSoft.Base.Logging.Abstracts;
using HsnSoft.Base.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Hhs.IdentityService.Application.Services;

public sealed class AppRoleAppService : ApplicationServiceBase, IAppRoleAppService
{
    private readonly IAppConsoleLogger _logger;
    private readonly IAppRoleRepository _appRoleRepository;
    private readonly ITenantRepository _tenantRepository;

    public AppRoleAppService(IServiceProvider provider,
        IAppRoleRepository appRoleRepository, ITenantRepository tenantRepository) : base(provider)
    {
        _logger = provider.GetRequiredService<IAppConsoleLogger>();

        _appRoleRepository = appRoleRepository;
        _tenantRepository = tenantRepository;
    }

    public async Task<AppRoleDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        var item = await _appRoleRepository.GetSingleOrDefaultAsync(x => x.Id == id, cancellationToken: cancellationToken);
        if (item == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.NotFound);
        }

        return Mapper.Map<AppRole, AppRoleDto>(item);
    }

    public async Task<PagedDataResultDto<AppRoleDto>> GetPagedListAsync(GetAppRolesPaged pagedInput, CancellationToken cancellationToken = default)
    {
        if (pagedInput == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        pagedInput.SearchText = StringOperations.Normalize(pagedInput.SearchText);
        pagedInput.Name = StringOperations.Normalize(pagedInput.Name);

        var filter = new FilterBuilder<AppRole>()
            .And(!string.IsNullOrWhiteSpace(pagedInput.SearchText) ? e => e.NormalizedName.Contains(pagedInput.SearchText) : null)
            .And(pagedInput.TenantId.HasValue ? e => e.TenantId == pagedInput.TenantId.Value : null)
            .And(!string.IsNullOrWhiteSpace(pagedInput.Name) ? e => e.NormalizedName.Contains(pagedInput.Name) : null)
            .And(pagedInput.IsDefault.HasValue ? e => e.IsDefault == pagedInput.IsDefault.Value : null)
            .And(pagedInput.IsStatic.HasValue ? e => e.IsStatic == pagedInput.IsStatic.Value : null)
            .Build();

        var result = await _appRoleRepository.GetPageListAsync<AppRoleDto>(
            options: new PagedQueryOptions<AppRole>
            {
                Filter = filter,
                IncludeEntity = q => q.Include(x => x.Tenant),
                OrderByDynamic = string.IsNullOrWhiteSpace(pagedInput.SortingText)
                    ? AppRoleConsts.GetDefaultSorting()
                    : pagedInput.SortingText,
                PageNumber = pagedInput.PageNumber,
                MaxResultCount = pagedInput.MaxResultCount
            }, Mapper.ConfigurationProvider, cancellationToken: cancellationToken);

        return new PagedDataResultDto<AppRoleDto>(result.TotalCount, pagedInput.PageNumber, pagedInput.MaxResultCount, result.Items);
    }

    public async Task<List<AppRoleDto>> GetFilterListAsync(GetAppRolesFilter filterInput, CancellationToken cancellationToken = default)
    {
        if (filterInput == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        filterInput.Name = StringOperations.Normalize(filterInput.Name);

        var filter = new FilterBuilder<AppRole>()
            .And(filterInput.TenantId.HasValue ? e => e.TenantId == filterInput.TenantId.Value : null)
            .And(!string.IsNullOrWhiteSpace(filterInput.Name) ? e => e.NormalizedName.Contains(filterInput.Name) : null)
            .And(filterInput.IsDefault.HasValue ? e => e.IsDefault == filterInput.IsDefault.Value : null)
            .And(filterInput.IsStatic.HasValue ? e => e.IsStatic == filterInput.IsStatic.Value : null)
            .Build();

        return await _appRoleRepository.GetListAsync<AppRoleDto>(
            options: new ListQueryOptions<AppRole>
            {
                Filter = filter,
                IncludeEntity = q => q.Include(x => x.Tenant),
                OrderByDynamic = string.IsNullOrWhiteSpace(filterInput.SortingText)
                    ? AppRoleConsts.GetDefaultSorting()
                    : filterInput.SortingText,
                MaxResultCount = filterInput.MaxResultCount
            }, Mapper.ConfigurationProvider, cancellationToken: cancellationToken);
    }

    public async Task<List<AppRoleSearchDto>> GetSearchListAsync(GetAppRolesSearch searchInput, CancellationToken cancellationToken = default)
    {
        if (searchInput == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        searchInput.SearchText = StringOperations.Normalize(searchInput.SearchText);

        var filter = new FilterBuilder<AppRole>()
            .And(!string.IsNullOrWhiteSpace(searchInput.SearchText) ? e => e.NormalizedName.Contains(searchInput.SearchText) : null)
            .Build();

        return await _appRoleRepository.GetListAsync<AppRoleSearchDto>(
            options: new ListQueryOptions<AppRole>
            {
                Filter = filter,
                IncludeEntity = q => q.Include(x => x.Tenant),
                OrderByDynamic = string.IsNullOrWhiteSpace(searchInput.SortingText)
                    ? AppRoleConsts.GetDefaultSorting()
                    : searchInput.SortingText,
                MaxResultCount = searchInput.MaxResultCount
            }, Mapper.ConfigurationProvider, cancellationToken: cancellationToken);
    }

    public async Task<AppRoleDto> CreateAsync(AppRoleCreateDto input)
    {
        if (input == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        if (!await _tenantRepository.ExistsAsync(x => x.Id == input.TenantId))
        {
            throw new TenantNotFoundException(L, input.TenantId.ToString());
        }

        var appRole = await _appRoleRepository.CreateAsync(
            tenantId: input.TenantId ?? Guid.Empty,
            name: input.Name ?? string.Empty,
            isDefault: input.IsDefault,
            isStatic: input.IsStatic
        );

        //INTEGRATION EVENT TRIGGER
        //
        //

        return Mapper.Map<AppRole, AppRoleDto>(appRole);
    }

    public async Task UpdateAsync(AppRoleUpdateDto input)
    {
        if (input == null || input.Id == Guid.Empty)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        await _appRoleRepository.UpdateAsync(
            id: input.Id,
            name: input.Name ?? string.Empty,
            isDefault: input.IsDefault,
            isStatic: input.IsStatic
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

        await _appRoleRepository.DeleteAsync(id);

        //INTEGRATION EVENT TRIGGER
        // TODO: Create ROLE_DELETED event for AdminService PermissionGrant synch service !!!
        //
    }
}