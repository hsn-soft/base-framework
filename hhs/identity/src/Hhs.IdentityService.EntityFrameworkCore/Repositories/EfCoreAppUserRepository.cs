using Hhs.IdentityService.Domain.AuthDomain.Consts;
using Hhs.IdentityService.Domain.AuthDomain.Entities;
using Hhs.IdentityService.Domain.AuthDomain.Exceptions;
using Hhs.IdentityService.Domain.AuthDomain.Repositories;
using Hhs.IdentityService.Domain.Localization;
using Hhs.IdentityService.Domain.TenantDomain.Exceptions;
using Hhs.IdentityService.EntityFrameworkCore.Context;
using Hhs.Shared.Localization;
using HsnSoft.Base.Data;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.Validation.Localization;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Hhs.IdentityService.EntityFrameworkCore.Repositories;

public class EfCoreAppUserRepository : EfCoreGenericRepository<AppUser, Guid>, IAppUserRepository
{
    [NotNull] private IStringLocalizer L { get; }

    private readonly IDataFilter _dataFilter;
    private readonly ICurrentTenant _currentTenant;

    private readonly IAuthPasswordPolicyRepository _authPasswordPolicyRepository;

    public EfCoreAppUserRepository(
        IServiceProvider provider,
        IStringLocalizerFactory stringLocalizerFactory,
        IdentityServiceDbContext dbContext,
        IDataFilter dataFilter,
        ICurrentTenant currentTenant, IAuthPasswordPolicyRepository authPasswordPolicyRepository) : base(provider, dbContext)
    {
        L = stringLocalizerFactory.CreateMultiple([typeof(IdentityServiceResource), typeof(ValidationResource), typeof(SharedResource)]);
        _dataFilter = dataFilter;
        _currentTenant = currentTenant;
        _authPasswordPolicyRepository = authPasswordPolicyRepository;
    }

    public async Task<AppUser> CreateAsync(Guid tenantId,
        string userName,
        string email,
        string passwordHash,
        bool isStatic = false,
        string displayName = null,
        string avatarSuffixUrl = null,
        string phoneNumber = null,
        string languageCode = null
    )
        => await CreateAsync(Guid.CreateVersion7(), tenantId,
            userName,
            email,
            passwordHash,
            isStatic,
            displayName,
            avatarSuffixUrl,
            phoneNumber,
            languageCode
        );

    public async Task<AppUser> CreateAsync(Guid id, Guid tenantId,
        string userName,
        string email,
        string passwordHash,
        bool isStatic = false,
        string displayName = null,
        string avatarSuffixUrl = null,
        string phoneNumber = null,
        string languageCode = null
    )
    {
        if (id == Guid.Empty) id = Guid.CreateVersion7();

        // Create draft AppUser
        var draftAppUser = new AppUser(
            id: id,
            tenantId: tenantId,
            userName: userName,
            email: email,
            passwordHash: passwordHash,
            isStatic: isStatic,
            displayName: displayName,
            avatarSuffixUrl: avatarSuffixUrl,
            phoneNumber: phoneNumber,
            languageCode: languageCode
        );

        //Domain Rule -> Tenant Management Access
        TenantCrudControl(draftAppUser.TenantId);

        //Domain Rule -> AppUser must be unique
        await DuplicateControlAsync(draftAppUser);

        _ = await InsertAsync(draftAppUser);
        return draftAppUser;
    }

    public async Task<AppUser> UpdateAsync(Guid id,
        string userName,
        string email,
        string displayName = null,
        string avatarSuffixUrl = null,
        string phoneNumber = null,
        string languageCode = null)
    {
        var oldAppUser = await GetSingleOrDefaultAsync(x => x.Id == id);
        if (oldAppUser == null)
        {
            throw new AppUserNotFoundException(L, id.ToString());
        }

        oldAppUser.SetUserName(userName);
        oldAppUser.SetEmail(email);
        oldAppUser.SetDisplayName(displayName);
        oldAppUser.SetAvatarSuffixUrl(avatarSuffixUrl);
        oldAppUser.SetPhoneNumber(phoneNumber);
        oldAppUser.SetLanguageCode(languageCode);

        //Domain Rule -> AppUser must be unique
        await DuplicateControlForUpdateAsync(oldAppUser);

        //Domain Rules
        // Rule01
        // Rule02
        _ = await UpdateAsync(oldAppUser);
        return oldAppUser;
    }

    public async Task SetLoginFailureStatesAsync(Guid tenantId, Guid appUserId, int failedLoginCount)
    {
        var policy = await _authPasswordPolicyRepository.GetSingleOrDefaultAsync(x => x.TenantId == tenantId);
        policy ??= new AuthPasswordPolicy { TenantId = tenantId };

        failedLoginCount++;

        DateTime? lockoutEndAt = null;
        if (failedLoginCount >= policy.MaxFailedLoginCount)
        {
            lockoutEndAt = DateTime.UtcNow.AddMinutes(policy.LockoutMinutes);
        }

        int affectedCount = await GetDbSet().Where(b => b.Id == appUserId).ExecuteUpdateAsync(s => s
            .SetProperty(a => a.FailedLoginCount, failedLoginCount)
            .SetProperty(a => a.LockoutEndAt, lockoutEndAt)
        );

        if (affectedCount < 1)
        {
            throw new AppUserNotFoundException(L, appUserId.ToString());
        }
    }

    public async Task SetLoginSuccessStatesAsync(Guid appUserId)
    {
        DateTime? lockoutEndAt = null;
        int affectedCount = await GetDbSet().Where(b => b.Id == appUserId).ExecuteUpdateAsync(s => s
            .SetProperty(a => a.FailedLoginCount, 0)
            .SetProperty(a => a.LockoutEndAt, lockoutEndAt)
            .SetProperty(a => a.LastLoginAt, DateTime.UtcNow)
        );

        if (affectedCount < 1)
        {
            throw new AppUserNotFoundException(L, appUserId.ToString());
        }
    }

    public async Task DeleteAsync(Guid id)
    {
        var appUser = await GetSingleOrDefaultAsync(x => x.Id == id);
        if (appUser == null)
        {
            throw new AppUserNotFoundException(L, id.ToString());
        }

        string guidGenerated = Guid.CreateVersion7().ToString("N").ToUpper();

        string uniqueUserName = guidGenerated + "_" + appUser.UserName;
        if (uniqueUserName.Length > AppUserConsts.UserNameMaxLength)
        {
            uniqueUserName = uniqueUserName[..AppUserConsts.UserNameMaxLength];
        }

        string uniqueEmail = guidGenerated + "_" + appUser.Email;
        if (uniqueEmail.Length > AppUserConsts.EmailMaxLength)
        {
            uniqueEmail = uniqueEmail[..AppUserConsts.EmailMaxLength];
        }

        appUser.SetUserName(uniqueUserName);
        appUser.SetEmail(uniqueEmail);
        appUser.IsDeleted = true;

        await UpdateAsync(appUser);
    }

    private void TenantCrudControl(Guid targetTenantId)
    {
        if (!_dataFilter.IsEnabled<IMultiTenant>()) return;

        if (_currentTenant.IsSystemTenant) return;

        if (_currentTenant.AllowedTenantIds.Contains(targetTenantId)) return;

        throw new UnauthorizedTenantException(L, targetTenantId.ToString());
    }

    private async Task DuplicateControlAsync(AppUser newAppUser)
    {
        var old = await GetSingleOrDefaultAsync(x =>
            x.Id == newAppUser.Id
            || (x.TenantId == newAppUser.TenantId && x.NormalizedUserName == newAppUser.NormalizedUserName)
        );
        if (old != null)
        {
            throw new AppUserUsernameDuplicateException(L, newAppUser.NormalizedUserName).WithData(nameof(newAppUser.TenantId), newAppUser.TenantId);
        }

        old = await GetSingleOrDefaultAsync(x =>
            x.Id == newAppUser.Id
            || (x.TenantId == newAppUser.TenantId && x.NormalizedEmail == newAppUser.NormalizedEmail)
        );
        if (old != null)
        {
            throw new AppUserEmailDuplicateException(L, newAppUser.NormalizedEmail).WithData(nameof(newAppUser.TenantId), newAppUser.TenantId);
        }
    }

    private async Task DuplicateControlForUpdateAsync(AppUser oldAppUser)
    {
        var result = await GetSingleOrDefaultAsync(x =>
            x.Id != oldAppUser.Id &&
            //unique parameters
            x.TenantId == oldAppUser.TenantId && x.NormalizedUserName == oldAppUser.NormalizedUserName
        );

        if (result != null)
        {
            throw new AppUserUsernameDuplicateException(L, oldAppUser.NormalizedUserName);
        }

        result = await GetSingleOrDefaultAsync(x =>
            x.Id != oldAppUser.Id &&
            //unique parameters
            x.TenantId == oldAppUser.TenantId && x.NormalizedEmail == oldAppUser.NormalizedEmail
        );

        if (result != null)
        {
            throw new AppUserEmailDuplicateException(L, oldAppUser.NormalizedEmail);
        }
    }
}