using System.Net;
using Hhs.IdentityService.Application.Contracts.AppUserDomain.Dtos;
using Hhs.IdentityService.Application.Contracts.AppUserDomain.Dtos.Filters;
using Hhs.IdentityService.Application.Contracts.AppUserDomain.Dtos.Submits;
using Hhs.IdentityService.Application.Contracts.AppUserDomain.Services;
using Hhs.IdentityService.Domain.AuthDomain.Consts;
using Hhs.IdentityService.Domain.AuthDomain.Entities;
using Hhs.IdentityService.Domain.AuthDomain.Repositories;
using Hhs.Shared.Helper.Utils;
using HsnSoft.Base;
using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Domain.Models;
using HsnSoft.Base.Logging.Abstracts;
using HsnSoft.Base.Reflection;
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


    public async Task<AppUserDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        var item = await _appUserRepository.GetSingleOrDefaultAsync(x => x.Id == id, cancellationToken: cancellationToken);
        if (item == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.NotFound);
        }

        return Mapper.Map<AppUser, AppUserDto>(item);
    }

    public async Task<PagedDataResultDto<AppUserDto>> GetPagedListAsync(GetAppUsersPaged pagedInput, CancellationToken cancellationToken = default)
    {
        if (pagedInput == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        pagedInput.UserName = StringOperations.Normalize(pagedInput.UserName);
        pagedInput.Email = StringOperations.Normalize(pagedInput.Email);

        var filter = new FilterBuilder<AppUser>()
            .And(pagedInput.TenantId.HasValue ? e => e.TenantId == pagedInput.TenantId.Value : null)
            .And(!string.IsNullOrWhiteSpace(pagedInput.UserName) ? e => e.NormalizedUserName.Contains(pagedInput.UserName) : null)
            .And(!string.IsNullOrWhiteSpace(pagedInput.Email) ? e => e.NormalizedEmail.Contains(pagedInput.Email) : null)
            .And(!string.IsNullOrWhiteSpace(pagedInput.PhoneNumber) ? e => e.PhoneNumber!=null && e.PhoneNumber.Contains(pagedInput.PhoneNumber) : null)
            .And(pagedInput.EmailConfirmed.HasValue ? e => e.EmailConfirmed == pagedInput.EmailConfirmed.Value : null)
            .And(pagedInput.PhoneNumberConfirmed.HasValue ? e => e.PhoneNumberConfirmed == pagedInput.PhoneNumberConfirmed.Value : null)
            .Build();

        var result = await _appUserRepository.GetPageListAsync<AppUserDto>(
            options: new PagedQueryOptions<AppUser>
            {
                Filter = filter,
                OrderByDynamic = string.IsNullOrWhiteSpace(pagedInput.SortingText)
                    ? AppUserConsts.GetDefaultSorting()
                    : pagedInput.SortingText,
                PageNumber = pagedInput.PageNumber,
                MaxResultCount = pagedInput.MaxResultCount
            }, Mapper.ConfigurationProvider, cancellationToken: cancellationToken);

        return new PagedDataResultDto<AppUserDto>(result.TotalCount, pagedInput.PageNumber, pagedInput.MaxResultCount, result.Items);
    }

    public async Task<List<AppUserDto>> GetFilterListAsync(GetAppUsersFilter filterInput, CancellationToken cancellationToken = default)
    {
        if (filterInput == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        filterInput.UserName = StringOperations.Normalize(filterInput.UserName);
        filterInput.Email = StringOperations.Normalize(filterInput.Email);

        var filter = new FilterBuilder<AppUser>()
            .And(filterInput.TenantId.HasValue ? e => e.TenantId == filterInput.TenantId.Value : null)
            .And(!string.IsNullOrWhiteSpace(filterInput.UserName) ? e => e.NormalizedUserName.Contains(filterInput.UserName) : null)
            .And(!string.IsNullOrWhiteSpace(filterInput.Email) ? e => e.NormalizedEmail.Contains(filterInput.Email) : null)
            .And(!string.IsNullOrWhiteSpace(filterInput.PhoneNumber) ? e => e.PhoneNumber!=null && e.PhoneNumber.Contains(filterInput.PhoneNumber) : null)
            .And(filterInput.EmailConfirmed.HasValue ? e => e.EmailConfirmed == filterInput.EmailConfirmed.Value : null)
            .And(filterInput.PhoneNumberConfirmed.HasValue ? e => e.PhoneNumberConfirmed == filterInput.PhoneNumberConfirmed.Value : null)
            .Build();

        return await _appUserRepository.GetListAsync<AppUserDto>(
            options: new ListQueryOptions<AppUser>
            {
                Filter = filter,
                OrderByDynamic = string.IsNullOrWhiteSpace(filterInput.SortingText)
                    ? AppUserConsts.GetDefaultSorting()
                    : filterInput.SortingText,
                MaxResultCount = filterInput.MaxResultCount
            }, Mapper.ConfigurationProvider, cancellationToken: cancellationToken);
    }

    public async Task<List<AppUserDto>> GetSearchListAsync(GetAppUsersSearch searchInput, CancellationToken cancellationToken = default)
    {
        if (searchInput == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        searchInput.SearchText = StringOperations.Normalize(searchInput.SearchText);

        var filter = new FilterBuilder<AppUser>()
            .And(!string.IsNullOrWhiteSpace(searchInput.SearchText)
                ? e => e.NormalizedUserName.Contains(searchInput.SearchText) ||  e.NormalizedEmail.Contains(searchInput.SearchText)
                : null)
            .Build();

        return await _appUserRepository.GetListAsync<AppUserDto>(
            options: new ListQueryOptions<AppUser>
            {
                Filter = filter,
                OrderByDynamic = string.IsNullOrWhiteSpace(searchInput.SortingText)
                    ? AppUserConsts.GetDefaultSorting()
                    : searchInput.SortingText,
                MaxResultCount = searchInput.MaxResultCount
            }, Mapper.ConfigurationProvider, cancellationToken: cancellationToken);
    }

    public Task<AppUserDto> CreateAsync(AppUserCreateDto input) => throw new NotImplementedException();

    public Task UpdateAsync(AppUserUpdateDto input) => throw new NotImplementedException();

    public Task DeleteAsync(Guid id) => throw new NotImplementedException();
}