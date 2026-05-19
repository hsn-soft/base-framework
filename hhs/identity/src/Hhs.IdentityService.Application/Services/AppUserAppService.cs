using System.Globalization;
using System.Net;
using Hhs.IdentityService.Application.Contracts.AppUserDomain.Dtos;
using Hhs.IdentityService.Application.Contracts.AppUserDomain.Dtos.Filters;
using Hhs.IdentityService.Application.Contracts.AppUserDomain.Dtos.Submits;
using Hhs.IdentityService.Application.Contracts.AppUserDomain.Services;
using Hhs.IdentityService.Domain.AppUserDomain.Consts;
using Hhs.IdentityService.Domain.AppUserDomain.Entities;
using Hhs.IdentityService.Domain.AppUserDomain.Repositories;
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

    public async Task<AppUserDto> GetAsync(Guid id)
    {
        if (id == Guid.Empty)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        var item = await _appUserRepository.FindWithIdAsync(id);
        if (item == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.NotFound);
        }

        var resultItem = Mapper.Map<AppUser, AppUserDto>(item);
        resultItem.Roles = await _appUserRepository.GetUserRolesAsync(item);
        resultItem.Roles ??= new List<string>();

        return resultItem;
    }

    public async Task<PagedDataResultDto<AppUserDto>> GetPagedListAsync(GetAppUsersPaged pagedInput, CancellationToken cancellationToken = default)
    {
        if (pagedInput == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        pagedInput.SearchText = pagedInput.SearchText?.ToLower(new CultureInfo("en-US"));
        pagedInput.UserName = pagedInput.UserName?.ToLower(new CultureInfo("en-US"));
        pagedInput.Email = pagedInput.Email?.ToLower(new CultureInfo("en-US"));
        pagedInput.PhoneNumber = pagedInput.PhoneNumber?.ToLower(new CultureInfo("en-US"));
        pagedInput.Name = pagedInput.Name?.ToLower(new CultureInfo("en-US"));
        pagedInput.Surname = pagedInput.Surname?.ToLower(new CultureInfo("en-US"));

        var filter = new FilterBuilder<AppUser>()
            .And(!string.IsNullOrWhiteSpace(pagedInput.SearchText)
                ? e => e.UserName.ToLower().Contains(pagedInput.SearchText)
                       || e.Email.ToLower().Contains(pagedInput.SearchText)
                       || e.Name.ToLower().Contains(pagedInput.SearchText)
                       || e.Surname.ToLower().Contains(pagedInput.SearchText)
                       || e.PhoneNumber.Contains(pagedInput.SearchText)
                : null)
            .And(!string.IsNullOrWhiteSpace(pagedInput.UserName) ? e => e.UserName.ToLower().Contains(pagedInput.UserName) : null)
            .And(!string.IsNullOrWhiteSpace(pagedInput.Email) ? e => e.Email.ToLower().Contains(pagedInput.Email) : null)
            .And(pagedInput.EmailConfirmed.HasValue ? e => e.EmailConfirmed == pagedInput.EmailConfirmed.Value : null)
            .And(!string.IsNullOrWhiteSpace(pagedInput.PhoneNumber) ? e => e.PhoneNumber.ToLower().Contains(pagedInput.PhoneNumber) : null)
            .And(pagedInput.PhoneNumberConfirmed.HasValue ? e => e.PhoneNumberConfirmed == pagedInput.PhoneNumberConfirmed.Value : null)
            .And(!string.IsNullOrWhiteSpace(pagedInput.Name) ? e => e.Name.ToLower().Contains(pagedInput.Name) : null)
            .And(!string.IsNullOrWhiteSpace(pagedInput.Surname) ? e => e.Surname.ToLower().Contains(pagedInput.Surname) : null)
            .Build();

        var result = await _appUserRepository.GetPageListAsync(
            // extra filters
            tenantId: pagedInput.TenantId,
            roleIds: pagedInput.Roles is { Count: > 0 } ? pagedInput.Roles.Select(x => x.RoleId).ToList() : null,

            // standard filter
            options: new PagedQueryOptions<AppUser>
            {
                Filter = filter,
                OrderByDynamic = string.IsNullOrWhiteSpace(pagedInput.SortingText)
                    ? AppUserConsts.GetDefaultSorting()
                    : pagedInput.SortingText,
                PageNumber = pagedInput.PageNumber,
                MaxResultCount = pagedInput.MaxResultCount
            },
            cancellationToken: cancellationToken);

        var resultItems = new List<AppUserDto>();
        foreach (var item in result.Items)
        {
            var resultItem = Mapper.Map<AppUser, AppUserDto>(item);
            resultItem.Roles = await _appUserRepository.GetUserRolesAsync(item);
            resultItem.Roles ??= new List<string>();

            resultItem.UserName = (!string.IsNullOrWhiteSpace(resultItem.UserName) && resultItem.UserName.Contains('#'))
                ? resultItem.UserName.Split("#")[0]
                : resultItem.UserName;

            resultItem.Email = (!string.IsNullOrWhiteSpace(resultItem.Email) && resultItem.Email.Contains('#'))
                ? resultItem.Email.Split("#")[0]
                : resultItem.Email;

            if (string.IsNullOrWhiteSpace(resultItem.AvatarSuffixUrl))
            {
                resultItem.AvatarSuffixUrl = "/images/no-image.webp";
            }

            resultItems.Add(resultItem);
        }

        return new PagedDataResultDto<AppUserDto>(result.TotalCount, pagedInput.PageNumber, pagedInput.MaxResultCount, resultItems);
    }

    public async Task<List<AppUserDto>> GetFilterListAsync(GetAppUsersFilter filterInput, CancellationToken cancellationToken = default)
    {
        if (filterInput == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        filterInput.UserName = filterInput.UserName?.ToLower(new CultureInfo("en-US"));
        filterInput.Email = filterInput.Email?.ToLower(new CultureInfo("en-US"));
        filterInput.PhoneNumber = filterInput.PhoneNumber?.ToLower(new CultureInfo("en-US"));
        filterInput.Name = filterInput.Name?.ToLower(new CultureInfo("en-US"));
        filterInput.Surname = filterInput.Surname?.ToLower(new CultureInfo("en-US"));

        var filter = new FilterBuilder<AppUser>()
            .And(!string.IsNullOrWhiteSpace(filterInput.UserName) ? e => e.UserName.ToLower().Contains(filterInput.UserName) : null)
            .And(!string.IsNullOrWhiteSpace(filterInput.Email) ? e => e.Email.ToLower().Contains(filterInput.Email) : null)
            .And(filterInput.EmailConfirmed.HasValue ? e => e.EmailConfirmed == filterInput.EmailConfirmed.Value : null)
            .And(!string.IsNullOrWhiteSpace(filterInput.PhoneNumber) ? e => e.PhoneNumber.ToLower().Contains(filterInput.PhoneNumber) : null)
            .And(filterInput.PhoneNumberConfirmed.HasValue ? e => e.PhoneNumberConfirmed == filterInput.PhoneNumberConfirmed.Value : null)
            .And(!string.IsNullOrWhiteSpace(filterInput.Name) ? e => e.Name.ToLower().Contains(filterInput.Name) : null)
            .And(!string.IsNullOrWhiteSpace(filterInput.Surname) ? e => e.Surname.ToLower().Contains(filterInput.Surname) : null)
            .Build();

        var result = await _appUserRepository.GetListAsync(
            // extra filters
            tenantId: filterInput.TenantId,

            // standart filter
            options: new ListQueryOptions<AppUser>
            {
                Filter = filter,
                OrderByDynamic = string.IsNullOrWhiteSpace(filterInput.SortingText)
                    ? AppUserConsts.GetDefaultSorting()
                    : filterInput.SortingText,
                MaxResultCount = filterInput.MaxResultCount
            },
            cancellationToken: cancellationToken);

        var resultItems = new List<AppUserDto>();
        foreach (var item in result)
        {
            var resultItem = Mapper.Map<AppUser, AppUserDto>(item);
            resultItem.Roles = await _appUserRepository.GetUserRolesAsync(item);
            resultItem.Roles ??= new List<string>();

            resultItems.Add(resultItem);
        }

        return resultItems;
    }

    public async Task<List<AppUserDto>> GetSearchListAsync(GetAppUsersSearch searchInput)
    {
        if (searchInput == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        var items = await _appUserRepository.GetSearchListAsync(searchInput.TenantId,
            searchInput.SearchText, searchInput.SortingText, searchInput.MaxResultCount);

        if (items == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.RequestTimeout);
        }

        var resultItems = new List<AppUserDto>();
        foreach (var item in items)
        {
            var resultItem = Mapper.Map<AppUser, AppUserDto>(item);
            resultItem.Roles = await _appUserRepository.GetUserRolesAsync(item);
            resultItem.Roles ??= new List<string>();

            resultItems.Add(resultItem);
        }

        return resultItems;
    }

    public async Task<AppUserDto> CreateAsync(AppUserCreateDto input)
    {
        if (input == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        var appUser = await _appUserRepository.CreateAsync(
            tenantId: input.TenantId ?? Guid.Empty,
            tenantDomain: input.TenantDomain,
            userName: input.UserName,
            email: input.Email,
            phone: input.PhoneNumber,
            name: input.Name,
            surname: input.Surname,
            defaultLanguage: input.DefaultLanguage,
            avatarSuffixUrl: input.AvatarSuffixUrl,
            plainPassword: "Passw0rd!",
            roles: input.Roles is { Count: > 0 } ? input.Roles : new List<string>()
        );

        //INTEGRATION EVENT TRIGGER
        //
        //

        return Mapper.Map<AppUser, AppUserDto>(appUser);
    }

    public async Task UpdateAsync(AppUserUpdateDto input)
    {
        if (input == null || input.Id == Guid.Empty)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        await _appUserRepository.UpdateAsync(
            id: input.Id,
            userName: input.UserName,
            email: input.Email,
            phone: input.PhoneNumber,
            name: input.Name,
            surname: input.Surname,
            defaultLanguage: input.DefaultLanguage,
            roles: input.Roles is { Count: > 0 } ? input.Roles : new List<string>()
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

        await _appUserRepository.DeleteAsync(id);

        //INTEGRATION EVENT TRIGGER
        //
        //
    }
}