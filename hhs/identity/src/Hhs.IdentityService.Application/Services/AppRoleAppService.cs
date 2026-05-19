using System.Net;
using Hhs.IdentityService.Application.Contracts.AppRoleDomain.Dtos;
using Hhs.IdentityService.Application.Contracts.AppRoleDomain.Dtos.Filters;
using Hhs.IdentityService.Application.Contracts.AppRoleDomain.Dtos.Submits;
using Hhs.IdentityService.Application.Contracts.AppRoleDomain.Services;
using Hhs.IdentityService.Domain.AppRoleDomain.Entities;
using Hhs.IdentityService.Domain.AppRoleDomain.Repositories;
using HsnSoft.Base;
using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Logging.Abstracts;
using Microsoft.Extensions.DependencyInjection;

namespace Hhs.IdentityService.Application.Services;

public sealed class AppRoleAppService : ApplicationServiceBase, IAppRoleAppService
{
    private readonly IAppConsoleLogger _logger;
    private readonly IAppRoleRepository _appRoleRepository;

    public AppRoleAppService(IServiceProvider provider,
        IAppRoleRepository appRoleRepository
    ) : base(provider)
    {
        _logger = provider.GetRequiredService<IAppConsoleLogger>();

        _appRoleRepository = appRoleRepository;
    }

    public async Task<AppRoleDto> GetAsync(Guid id)
    {
        if (id == Guid.Empty)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        var item = await _appRoleRepository.FindWithIdAsync(id);
        if (item == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.NotFound);
        }

        return Mapper.Map<AppRole, AppRoleDto>(item);
    }

    public async Task<PagedDataResultDto<AppRoleDto>> GetPagedListAsync(GetAppRolesPaged pagedInput)
    {
        if (pagedInput == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        long totalCount = await _appRoleRepository.GetCountWithFiltersAsync(pagedInput.TenantId,
            pagedInput.Name, pagedInput.IsDefault, pagedInput.IsStatic, pagedInput.IsPublic);

        var items = await _appRoleRepository.GetPagedListWithFiltersAsync(pagedInput.TenantId,
            pagedInput.Name, pagedInput.IsDefault, pagedInput.IsStatic, pagedInput.IsPublic,
            pagedInput.SortingText, pagedInput.MaxResultCount, pagedInput.PageNumber);

        if (items == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.RequestTimeout);
        }

        return new PagedDataResultDto<AppRoleDto>(totalCount, pagedInput.PageNumber, pagedInput.MaxResultCount, Mapper.Map<List<AppRole>, List<AppRoleDto>>(items));
    }

    public async Task<List<AppRoleDto>> GetFilterListAsync(GetAppRolesFilter filterInput)
    {
        if (filterInput == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        var items = await _appRoleRepository.GetFilterListAsync(filterInput.TenantId,
            filterInput.Name, filterInput.IsDefault, filterInput.IsStatic, filterInput.IsPublic,
            filterInput.SortingText);

        if (items == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.RequestTimeout);
        }

        return Mapper.Map<List<AppRole>, List<AppRoleDto>>(items);
    }

    public async Task<List<AppRoleDto>> GetSearchListAsync(GetAppRolesSearch searchInput)
    {
        if (searchInput == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        var items = await _appRoleRepository.GetSearchListAsync(searchInput.TenantId,
            searchInput.SearchText, searchInput.SortingText, searchInput.MaxResultCount);

        if (items == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.RequestTimeout);
        }

        return Mapper.Map<List<AppRole>, List<AppRoleDto>>(items);
    }

    public async Task<AppRoleDto> CreateAsync(AppRoleCreateDto input)
    {
        if (input == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        var appRole = await _appRoleRepository.CreateAsync(
            tenantId: input.TenantId ?? Guid.Empty,
            tenantDomain: input.TenantDomain,
            name: input.Name ?? string.Empty,
            isDefault: input.IsDefault,
            isStatic: input.IsStatic,
            isPublic: input.IsPublic
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
            isStatic: input.IsStatic,
            isPublic: input.IsPublic
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