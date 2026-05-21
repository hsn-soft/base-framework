using Hhs.IdentityService.Application.Contracts.AppRoleDomain.Dtos;
using Hhs.IdentityService.Application.Contracts.AppRoleDomain.Dtos.Filters;
using Hhs.IdentityService.Application.Contracts.AppRoleDomain.Dtos.Submits;
using Hhs.IdentityService.Application.Contracts.AppRoleDomain.Services;
using Hhs.IdentityService.Domain.AuthDomain.Repositories;
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


    public Task<AppRoleDto> GetAsync(Guid id) => throw new NotImplementedException();

    public Task<PagedDataResultDto<AppRoleDto>> GetPagedListAsync(GetAppRolesPaged pagedInput) => throw new NotImplementedException();

    public Task<List<AppRoleDto>> GetFilterListAsync(GetAppRolesFilter filterInput) => throw new NotImplementedException();

    public Task<List<AppRoleDto>> GetSearchListAsync(GetAppRolesSearch searchInput) => throw new NotImplementedException();

    public Task<AppRoleDto> CreateAsync(AppRoleCreateDto input) => throw new NotImplementedException();

    public Task UpdateAsync(AppRoleUpdateDto input) => throw new NotImplementedException();

    public Task DeleteAsync(Guid id) => throw new NotImplementedException();
}