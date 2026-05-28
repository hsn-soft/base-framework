using System.Net;
using Hhs.IdentityService.Application.Contracts.AppUserDomain.Dtos;
using Hhs.IdentityService.Application.Contracts.AppUserDomain.Dtos.Filters;
using Hhs.IdentityService.Application.Contracts.AppUserDomain.Dtos.Submits;
using Hhs.IdentityService.Application.Contracts.AppUserDomain.Services;
using Hhs.IdentityService.Domain.AuthDomain.Consts;
using Hhs.IdentityService.Domain.AuthDomain.Entities;
using Hhs.IdentityService.Domain.AuthDomain.Exceptions;
using Hhs.IdentityService.Domain.AuthDomain.Repositories;
using Hhs.IdentityService.Domain.TenantDomain.Repositories;
using Hhs.Shared.Helper.Utils;
using HsnSoft.Base;
using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Domain.Models;
using HsnSoft.Base.Logging.Abstracts;
using HsnSoft.Base.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace Hhs.IdentityService.Application.Services;

public sealed class AppUserAppService : ApplicationServiceBase, IAppUserAppService
{
    private const string DefaultPass = "Passw0rd!";
    private readonly IAppConsoleLogger _logger;
    private readonly IAppUserRepository _appUserRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITenantRepository _tenantRepository;
    private readonly IAppRoleRepository _appRoleRepository;
    private readonly IAppUserRoleRepository _appUserRoleRepository;
    private readonly IAuthPasswordPolicyRepository _authPasswordPolicyRepository;

    public AppUserAppService(
        IServiceProvider provider,
        IAppUserRepository appUserRepository,
        IPasswordHasher passwordHasher,
        ITenantRepository tenantRepository,
        IAppRoleRepository appRoleRepository,
        IAppUserRoleRepository appUserRoleRepository,
        IAuthPasswordPolicyRepository authPasswordPolicyRepository
    ) : base(provider)
    {
        _logger = provider.GetRequiredService<IAppConsoleLogger>();

        _appUserRepository = appUserRepository;
        _passwordHasher = passwordHasher;
        _tenantRepository = tenantRepository;
        _appRoleRepository = appRoleRepository;
        _appUserRoleRepository = appUserRoleRepository;
        _authPasswordPolicyRepository = authPasswordPolicyRepository;
    }

    public async Task<AppUserDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        var item = await _appUserRepository.GetSingleOrDefaultAsync<AppUserDto>(
            predicate: x => x.Id == id,
            includeEntity: q => q
                .Include(x => x.Tenant)
                .Include(x => x.UserRoles)
                .ThenInclude(x => x.Role),
            configuration: Mapper.ConfigurationProvider, cancellationToken: cancellationToken);
        if (item == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.NotFound);
        }

        return item;
    }

    public async Task<PagedDataResultDto<AppUserDto>> GetPagedListAsync(GetAppUsersPaged pagedInput, CancellationToken cancellationToken = default)
    {
        if (pagedInput == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        pagedInput.SearchText = StringOperations.Normalize(pagedInput.SearchText);
        pagedInput.UserName = StringOperations.Normalize(pagedInput.UserName);
        pagedInput.Email = StringOperations.Normalize(pagedInput.Email);

        var filter = new FilterBuilder<AppUser>()
            .And(!string.IsNullOrWhiteSpace(pagedInput.SearchText)
                ? e => e.NormalizedUserName.Contains(pagedInput.SearchText) || e.NormalizedEmail.Contains(pagedInput.SearchText)
                : null)
            .And(pagedInput.TenantId.HasValue ? e => e.TenantId == pagedInput.TenantId.Value : null)
            .And(!string.IsNullOrWhiteSpace(pagedInput.UserName) ? e => e.NormalizedUserName.Contains(pagedInput.UserName) : null)
            .And(!string.IsNullOrWhiteSpace(pagedInput.Email) ? e => e.NormalizedEmail.Contains(pagedInput.Email) : null)
            .And(!string.IsNullOrWhiteSpace(pagedInput.PhoneNumber) ? e => e.PhoneNumber != null && e.PhoneNumber.Contains(pagedInput.PhoneNumber) : null)
            .And(pagedInput.EmailConfirmed.HasValue ? e => e.EmailConfirmed == pagedInput.EmailConfirmed.Value : null)
            .And(pagedInput.PhoneNumberConfirmed.HasValue ? e => e.PhoneNumberConfirmed == pagedInput.PhoneNumberConfirmed.Value : null)
            .Build();

        var result = await _appUserRepository.GetPageListAsync<AppUserDto>(
            options: new PagedQueryOptions<AppUser>
            {
                Filter = filter,
                IncludeEntity = q => q
                    .Include(x => x.Tenant)
                    .Include(x => x.UserRoles)
                    .ThenInclude(x => x.Role),
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
            .And(!string.IsNullOrWhiteSpace(filterInput.PhoneNumber) ? e => e.PhoneNumber != null && e.PhoneNumber.Contains(filterInput.PhoneNumber) : null)
            .And(filterInput.EmailConfirmed.HasValue ? e => e.EmailConfirmed == filterInput.EmailConfirmed.Value : null)
            .And(filterInput.PhoneNumberConfirmed.HasValue ? e => e.PhoneNumberConfirmed == filterInput.PhoneNumberConfirmed.Value : null)
            .Build();

        return await _appUserRepository.GetListAsync<AppUserDto>(
            options: new ListQueryOptions<AppUser>
            {
                Filter = filter,
                IncludeEntity = q => q
                    .Include(x => x.Tenant)
                    .Include(x => x.UserRoles)
                    .ThenInclude(x => x.Role),
                OrderByDynamic = string.IsNullOrWhiteSpace(filterInput.SortingText)
                    ? AppUserConsts.GetDefaultSorting()
                    : filterInput.SortingText,
                MaxResultCount = filterInput.MaxResultCount
            }, Mapper.ConfigurationProvider, cancellationToken: cancellationToken);
    }

    public async Task<List<AppUserSearchDto>> GetSearchListAsync(GetAppUsersSearch searchInput, CancellationToken cancellationToken = default)
    {
        if (searchInput == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        searchInput.SearchText = StringOperations.Normalize(searchInput.SearchText);

        var filter = new FilterBuilder<AppUser>()
            .And(!string.IsNullOrWhiteSpace(searchInput.SearchText)
                ? e => e.NormalizedUserName.Contains(searchInput.SearchText) || e.NormalizedEmail.Contains(searchInput.SearchText)
                : null)
            .Build();

        return await _appUserRepository.GetListAsync<AppUserSearchDto>(
            options: new ListQueryOptions<AppUser>
            {
                Filter = filter,
                IncludeEntity = q => q.Include(x => x.Tenant),
                OrderByDynamic = string.IsNullOrWhiteSpace(searchInput.SortingText)
                    ? AppUserConsts.GetDefaultSorting()
                    : searchInput.SortingText,
                MaxResultCount = searchInput.MaxResultCount
            }, Mapper.ConfigurationProvider, cancellationToken: cancellationToken);
    }

    public async Task<AppUserDto> CreateAsync(AppUserCreateDto input)
    {
        if (input == null || CurrentUser == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        input.TenantId ??= Guid.Empty;
        if (!CurrentUser.IsSystemTenant && !CurrentUser.AllowedTenantIds.Contains(input.TenantId.Value))
        {
            throw new BaseHttpException((int)HttpStatusCode.Forbidden);
        }

        #region Role Control

        List<string> normalizedRoles = input.Roles
            .Select(role => StringOperations.Normalize(StringOperations.ReplaceInvalidChars(role)))
            .Where(checkRoleName => !string.IsNullOrWhiteSpace(checkRoleName))
            .Distinct()
            .ToList();

        if (normalizedRoles.Count == 0)
        {
            throw new AppRoleNotFoundException(L, "N/A");
        }

        var checkRoles = await _appRoleRepository.GetListAsync(
            options: new ListQueryOptions<AppRole> { Filter = x => x.TenantId == input.TenantId && normalizedRoles.Contains(x.NormalizedName) },
            selector: r => r.Id
        );
        if (checkRoles.Count != normalizedRoles.Count)
        {
            throw new AppRoleNotFoundException(L, "N/A");
        }

        #endregion

        // Password Rule Control
        // await ValidatePasswordAsync(input.TenantId.Value, plainPassword);

        var appUser = await _appUserRepository.CreateAsync(
            tenantId: input.TenantId ?? Guid.Empty,
            userName: input.UserName ?? string.Empty,
            email: input.Email ?? string.Empty,
            passwordHash: _passwordHasher.Hash(DefaultPass),
            displayName: input.DisplayName,
            avatarSuffixUrl: input.AvatarSuffixUrl,
            phoneNumber: input.PhoneNumber,
            languageCode: input.LanguageCode
        );

        // Create draft AppUser
        await _appUserRoleRepository.CreateManyAsync(appUser.Id, checkRoles);

        //INTEGRATION EVENT TRIGGER
        //
        //

        return await GetAsync(appUser.Id);
    }

    public async Task UpdateAsync(AppUserUpdateDto input)
    {
        if (input == null || input.Id == Guid.Empty)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        var oldAppUser = await _appUserRepository.GetSingleOrDefaultAsync(x => x.Id == input.Id);
        if (oldAppUser == null)
        {
            throw new AppUserNotFoundException(L, input.Id.ToString());
        }

        List<string> normalizedRoles = input.Roles
            .Select(role => StringOperations.Normalize(StringOperations.ReplaceInvalidChars(role)))
            .Where(checkRoleName => !string.IsNullOrWhiteSpace(checkRoleName))
            .Distinct()
            .ToList();

        if (normalizedRoles.Count == 0)
        {
            throw new AppRoleNotFoundException(L, "N/A");
        }

        var checkedRoleIds = await _appRoleRepository.GetListAsync(
            options: new ListQueryOptions<AppRole> { Filter = x => x.TenantId == oldAppUser.TenantId && normalizedRoles.Contains(x.NormalizedName) },
            selector: r => r.Id
        );
        if (checkedRoleIds.Count != normalizedRoles.Count)
        {
            throw new AppRoleNotFoundException(L, "N/A");
        }

        await _appUserRepository.UpdateAsync(
            id: input.Id,
            userName: input.UserName ?? string.Empty,
            email: input.Email ?? string.Empty,
            displayName: input.DisplayName,
            avatarSuffixUrl: input.AvatarSuffixUrl,
            phoneNumber: input.PhoneNumber,
            languageCode: input.LanguageCode
        );

        await _appUserRoleRepository.RemoveManyWithUserId(oldAppUser.Id);
        await _appUserRoleRepository.CreateManyAsync(oldAppUser.Id, checkedRoleIds);

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

    // private async Task ValidatePasswordAsync(Guid tenantId, string password)
    // {
    //     var policy = await  _authPasswordPolicyRepository.GetSingleOrDefaultAsync(x => x.TenantId == tenantId);
    //
    //     policy ??= new AuthPasswordPolicy { TenantId = tenantId };
    //
    //     if (password.Length < policy.MinLength)
    //         throw new Exception($"Şifre en az {policy.MinLength} karakter olmalıdır.");
    //
    //     if (policy.RequireDigit && !password.Any(char.IsDigit))
    //         throw new Exception("Şifre en az bir rakam içermelidir.");
    //
    //     if (policy.RequireLowercase && !password.Any(char.IsLower))
    //         throw new Exception("Şifre en az bir küçük harf içermelidir.");
    //
    //     if (policy.RequireUppercase && !password.Any(char.IsUpper))
    //         throw new Exception("Şifre en az bir büyük harf içermelidir.");
    //
    //     if (policy.RequireNonAlphanumeric && password.All(char.IsLetterOrDigit))
    //         throw new Exception("Şifre en az bir özel karakter içermelidir.");
    // }
}