using Hhs.IdentityService.Application.Contracts.AppUserDomain.Dtos;
using Hhs.IdentityService.Application.Contracts.AppUserDomain.Dtos.Filters;
using Hhs.IdentityService.Application.Contracts.AppUserDomain.Dtos.Submits;
using Hhs.IdentityService.Application.Contracts.AppUserDomain.Services;
using Hhs.IdentityService.Domain.AuthDomain.Repositories;
using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Logging.Abstracts;
using Microsoft.Extensions.DependencyInjection;

namespace Hhs.IdentityService.Application.Services;

public sealed class AppUserAppService : ApplicationServiceBase, IAppUserAppService
{
    private readonly IAppConsoleLogger _logger;
    private readonly IAppUserRepository _appUserRepository;

    public AppUserAppService(IServiceProvider provider,
        IAppUserRepository appUserRepository
    ) : base(provider)
    {
        _logger = provider.GetRequiredService<IAppConsoleLogger>();

        _appUserRepository = appUserRepository;
    }


    public Task<AppUserDto> GetAsync(Guid id) => throw new NotImplementedException();

    public Task<PagedDataResultDto<AppUserDto>> GetPagedListAsync(GetAppUsersPaged pagedInput, CancellationToken cancellationToken = default) => throw new NotImplementedException();

    public Task<List<AppUserDto>> GetFilterListAsync(GetAppUsersFilter filterInput, CancellationToken cancellationToken = default) => throw new NotImplementedException();

    public Task<List<AppUserDto>> GetSearchListAsync(GetAppUsersSearch searchInput) => throw new NotImplementedException();

    public Task<AppUserDto> CreateAsync(AppUserCreateDto input) => throw new NotImplementedException();

    public Task UpdateAsync(AppUserUpdateDto input) => throw new NotImplementedException();

    public Task DeleteAsync(Guid id) => throw new NotImplementedException();
}