using System.Globalization;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using Hhs.IdentityService.Domain.AppRoleDomain.Exceptions;
using Hhs.IdentityService.Domain.AppUserDomain.Consts;
using Hhs.IdentityService.Domain.AppUserDomain.Entities;
using Hhs.IdentityService.Domain.AppUserDomain.Exceptions;
using Hhs.IdentityService.Domain.AppUserDomain.Repositories;
using Hhs.IdentityService.Domain.Localization;
using Hhs.IdentityService.EntityFrameworkCore.Context;
using Hhs.Shared.Helper.Utils;
using Hhs.Shared.Localization;
using HsnSoft.Base;
using HsnSoft.Base.Data;
using HsnSoft.Base.Domain.Models;
using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.Validation.Localization;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Hhs.IdentityService.EntityFrameworkCore.Repositories;

public class AppUserRepository(
    IdentityAppDbContext context,
    IStringLocalizerFactory stringLocalizerFactory,
    IDataFilter dataFilter,
    ICurrentTenant currentTenant,
    UserManager<AppUser> userManager)
    : IAppUserRepository
{
    private DbSet<AppUser> GetDbSet() => context?.Set<AppUser>();

    [NotNull] protected IStringLocalizer L { get; } = stringLocalizerFactory.CreateMultiple([typeof(IdentityServiceResource), typeof(ValidationResource), typeof(SharedResource)]);

    [CanBeNull] private IDataFilter DataFilter { get; } = dataFilter;

    [CanBeNull] private ICurrentTenant CurrentTenant { get; } = currentTenant;

    private Guid? CurrentTenantId => CurrentTenant?.Id;

    private bool IsMultiTenantFilterEnabled => CurrentTenantId != null && (DataFilter?.IsEnabled<IMultiTenant>() ?? false);

    private bool IsSoftDeleteFilterEnabled => DataFilter?.IsEnabled<ISoftDelete>() ?? false;

    private IQueryable<AppUser> GetQueryableUser(List<Guid> roleIds = null)
    {
        IQueryable<AppUser> query = GetDbSet();
        if (roleIds is { Count: > 0 })
        {
            query = from usr in context.Users
                join ur in context.UserRoles on usr.Id equals ur.UserId
                where roleIds.Contains(ur.RoleId)
                select usr;
        }

        return query.AsNoTracking();
    }

    public async Task<PagedQueryResult<AppUser>> GetPageListAsync(
        PagedQueryOptions<AppUser> options,
        Guid? tenantId = null,
        List<Guid> roleIds = null,
        CancellationToken cancellationToken = default)
    {
        var query = GetQueryableUser(roleIds);
        if (IsMultiTenantFilterEnabled)
        {
            query = query.Where(e => e.TenantId == CurrentTenantId);
        }
        else if (tenantId.HasValue)
        {
            query = query.Where(e => e.TenantId == tenantId.Value);
        }

        if (IsSoftDeleteFilterEnabled)
        {
            query = query.Where(e => e.IsDeleted == false);
        }

        if (options.Filter != null) query = query.Where(options.Filter);

        long totalCount = await query.LongCountAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(options.OrderByDynamic))
            query = query.OrderBy(options.OrderByDynamic);
        else if (options.OrderByEntity != null)
            query = options.OrderByEntity(query);

        if (options.PageNumber > 1)
        {
            query = query.Skip((options.PageNumber - 1) * options.MaxResultCount)
                .Take(options.MaxResultCount);
        }
        else
        {
            query = query.Take(options.MaxResultCount);
        }

        var items = await query.ToListAsync(cancellationToken);

        return new PagedQueryResult<AppUser> { Items = items, TotalCount = totalCount };
    }

    public async Task<List<AppUser>> GetListAsync(
        ListQueryOptions<AppUser> options,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default
    )
    {
        var query = GetQueryableUser();
        if (IsMultiTenantFilterEnabled)
        {
            query = query.Where(e => e.TenantId == CurrentTenantId);
        }
        else if (tenantId.HasValue)
        {
            query = query.Where(e => e.TenantId == tenantId.Value);
        }

        if (IsSoftDeleteFilterEnabled)
        {
            query = query.Where(e => e.IsDeleted == false);
        }

        if (options.Filter != null) query = query.Where(options.Filter);

        if (!string.IsNullOrWhiteSpace(options.OrderByDynamic))
            query = query.OrderBy(options.OrderByDynamic);
        else if (options.OrderByEntity != null)
            query = options.OrderByEntity(query);

        if (options.MaxResultCount.HasValue) query = query.Take(options.MaxResultCount.Value);

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<List<AppUser>> GetSearchListAsync(Guid? tenantId,
        string searchText = null,
        string sorting = null,
        int maxResultCount = int.MaxValue,
        CancellationToken cancellationToken = default
    )
    {
        var query = ApplyFilter(context.Users.AsQueryable(), tenantId, searchText);

        return await query
            .OrderBy(string.IsNullOrWhiteSpace(sorting) ? AppUserConsts.GetDefaultSorting(false) : sorting)
            .PageBy(0, maxResultCount)
            .ToListAsync(cancellationToken: cancellationToken);
    }

    public async Task<List<string>> GetUserRolesAsync(AppUser currentUser)
    {
        var roleList = (await userManager.GetRolesAsync(currentUser)).ToList();
        if (roleList is not { Count: > 0 }) return roleList;

        for (int i = 0; i < roleList.Count; i++)
        {
            roleList[i] = StringOperations.SplitFirstValue(roleList[i], "#");
        }

        return roleList;
    }

    public async Task<AppUser> FindWithIdAsync(Guid id)
        => await FindAsync(x => x.Id == id);

    public async Task<AppUser> FindAsync(Expression<Func<AppUser, bool>> predicate)
        => await context.Users
            .WhereIf(IsSoftDeleteFilterEnabled, e => e.IsDeleted == false)
            .WhereIf(IsMultiTenantFilterEnabled, e => e.TenantId == CurrentTenantId)
            .FirstOrDefaultAsync(predicate);


    public async Task<AppUser> CreateAsync(Guid tenantId, string tenantDomain,
        string userName,
        string email,
        string phone,
        string name,
        string surname,
        string defaultLanguage,
        string avatarSuffixUrl,
        ICollection<string> roles = null,
        string plainPassword = null
    )
        => await CreateAsync(Guid.NewGuid(), tenantId, tenantDomain,
            userName,
            email,
            phone,
            name,
            surname,
            defaultLanguage,
            avatarSuffixUrl,
            roles,
            plainPassword
        );

    public async Task<AppUser> CreateAsync(Guid id, Guid tenantId, string tenantDomain,
        string userName,
        string email,
        string phone,
        string name,
        string surname,
        string defaultLanguage,
        string avatarSuffixUrl,
        ICollection<string> roles = null,
        string plainPassword = null
    )
    {
        if (id == Guid.Empty) id = Guid.NewGuid();

        userName = StringOperations.SplitFirstValue(userName, "#");
        email = StringOperations.SplitFirstValue(email, "#");
        string checkedUserName = StringOperations.ReplaceInvalidChars(userName, false, "-").ToLower(new CultureInfo("en-US"));
        string checkedEmail = StringOperations.ReplaceInvalidChars(email, true, "-").ToLower(new CultureInfo("en-US"));
        string userTenantDomain = StringOperations.ReplaceInvalidChars(tenantDomain, false, "-").ToLower(new CultureInfo("en-US"));

        checkedUserName = $"{checkedUserName}#{userTenantDomain}";
        checkedEmail = $"{checkedEmail}#{userTenantDomain}";

        // Create draft AppUser
        var draftAppUser = new AppUser(
            tenantId: tenantId,
            tenantDomain: userTenantDomain,
            id: id,
            userName: checkedUserName,
            email: checkedEmail,
            phone: phone,
            name: name,
            surname: surname,
            defaultLanguage: defaultLanguage,
            avatarSuffixUrl: avatarSuffixUrl
        );

        //Domain Rule -> AppUser must be unique
        await DuplicateControlAsync(draftAppUser);

        //Domain Rule -> User role is exist
        if (roles is { Count: > 0 })
        {
            var checkedRoleList = new List<string>();
            foreach (string role in roles)
            {
                string split = StringOperations.SplitFirstValue(role, "#");
                string checkedRoleName = StringOperations.ReplaceInvalidChars(split, false, "-").ToLower(new CultureInfo("en-US"));
                checkedRoleList.Add($"{checkedRoleName}#{userTenantDomain}");
            }

            roles = checkedRoleList;

            await UserRolesControlAsync(roles);
        }

        var result = string.IsNullOrWhiteSpace(plainPassword)
            ? await userManager.CreateAsync(draftAppUser)
            : await userManager.CreateAsync(draftAppUser, plainPassword);

        if (!result.Succeeded) throw new AppUserIdentityException(L, result.Errors);

        // add user roles
        if (roles is { Count: > 0 })
        {
            await AddToRolesAsync(draftAppUser, roles);
        }

        return draftAppUser;
    }

    public async Task<AppUser> UpdateAsync(Guid id,
        string userName,
        string email,
        string phone,
        string name,
        string surname,
        string defaultLanguage,
        ICollection<string> roles = null)
    {
        var oldAppUser = await FindWithIdAsync(id);
        if (oldAppUser == null)
        {
            throw new AppUserNotFoundException(L, id.ToString());
        }

        userName = StringOperations.SplitFirstValue(userName, "#");
        email = StringOperations.SplitFirstValue(email, "#");

        string checkedUserName = StringOperations.ReplaceInvalidChars(userName, false, "-").ToLower(new CultureInfo("en-US"));
        string checkedEmail = StringOperations.ReplaceInvalidChars(email, true, "-").ToLower(new CultureInfo("en-US"));
        string userTenantDomain = StringOperations.ReplaceInvalidChars(oldAppUser.TenantDomain, false, "-").ToLower(new CultureInfo("en-US"));

        checkedUserName = $"{checkedUserName}#{userTenantDomain}";
        checkedEmail = $"{checkedEmail}#{userTenantDomain}";

        oldAppUser.SetUserName(checkedUserName);
        oldAppUser.SetEmail(checkedEmail);
        oldAppUser.SetPhone(phone);
        oldAppUser.SetName(name);
        oldAppUser.SetSurname(surname);
        oldAppUser.SetDefaultLanguage(defaultLanguage);

        //Domain Rule -> AppUser must be unique
        await DuplicateControlForUpdateAsync(oldAppUser);

        //Domain Rule -> User role is exist
        if (roles is { Count: > 0 })
        {
            var checkedRoleList = new List<string>();
            foreach (string role in roles)
            {
                string split = StringOperations.SplitFirstValue(role, "#");
                string checkedRoleName = StringOperations.ReplaceInvalidChars(split, false, "-").ToLower(new CultureInfo("en-US"));
                checkedRoleList.Add($"{checkedRoleName}#{userTenantDomain}");
            }

            roles = checkedRoleList;

            await UserRolesControlAsync(roles);
        }

        var result = await userManager.UpdateAsync(oldAppUser);
        if (!result.Succeeded) throw new AppUserIdentityException(L, result.Errors);

        // remove user roles
        var existRoles = (await userManager.GetRolesAsync(oldAppUser)).ToList();
        if (existRoles is { Count: > 0 })
        {
            await RemoveFromRolesAsync(oldAppUser, existRoles);
        }

        // add user roles
        if (roles is { Count: > 0 })
        {
            await AddToRolesAsync(oldAppUser, roles);
        }

        return oldAppUser;
    }

    public async Task DeleteAsync(Guid id)
    {
        var appUser = await FindWithIdAsync(id);
        if (appUser == null)
        {
            throw new AppUserNotFoundException(L, id.ToString());
        }

        string guidGenerated = Guid.NewGuid().ToString("N").ToUpper();
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
        var result = await userManager.UpdateAsync(appUser);
        if (!result.Succeeded) throw new AppUserIdentityException(L, result.Errors);
    }

    private IQueryable<AppUser> ApplyFilter(
        IQueryable<AppUser> query,
        Guid? tenantId,
        [CanBeNull] string searchText = null,
        [CanBeNull] string username = null,
        [CanBeNull] string email = null,
        bool? emailConfirmed = null,
        [CanBeNull] string phoneNumber = null,
        bool? phoneConfirmed = null,
        [CanBeNull] string name = null,
        [CanBeNull] string surname = null)
    {
        searchText = searchText?.ToLower(new CultureInfo("en-US"));
        username = username?.ToLower(new CultureInfo("en-US"));
        email = email?.ToLower(new CultureInfo("en-US"));
        phoneNumber = phoneNumber?.ToLower(new CultureInfo("en-US"));
        name = name?.ToLower(new CultureInfo("en-US"));
        surname = surname?.ToLower(new CultureInfo("en-US"));

        if (IsMultiTenantFilterEnabled)
        {
            query = query.Where(e => e.TenantId == CurrentTenantId);
        }
        else if (tenantId.HasValue)
        {
            query = query.Where(e => e.TenantId == tenantId.Value);
        }

        return query
            .WhereIf(IsSoftDeleteFilterEnabled, e => e.IsDeleted == false)
            .WhereIf(!string.IsNullOrWhiteSpace(searchText), e => e.UserName.ToLower(new CultureInfo("en-US")).Contains(searchText)
                                                                  || e.Email.ToLower(new CultureInfo("en-US")).Contains(searchText)
                                                                  || e.Name.ToLower(new CultureInfo("en-US")).Contains(searchText)
                                                                  || e.Surname.ToLower(new CultureInfo("en-US")).Contains(searchText))
            .WhereIf(!string.IsNullOrWhiteSpace(username), e => e.UserName.ToLower(new CultureInfo("en-US")).Contains(username))
            .WhereIf(!string.IsNullOrWhiteSpace(email), e => e.Email.ToLower(new CultureInfo("en-US")).Contains(email))
            .WhereIf(emailConfirmed.HasValue, e => e.EmailConfirmed == emailConfirmed)
            .WhereIf(!string.IsNullOrWhiteSpace(phoneNumber), e => e.PhoneNumber.ToLower(new CultureInfo("en-US")).StartsWith(phoneNumber))
            .WhereIf(phoneConfirmed.HasValue, e => e.PhoneNumberConfirmed == phoneConfirmed)
            .WhereIf(!string.IsNullOrWhiteSpace(name), e => e.Name.ToLower(new CultureInfo("en-US")).Contains(name))
            .WhereIf(!string.IsNullOrWhiteSpace(surname), e => e.Surname.ToLower(new CultureInfo("en-US")).Contains(surname));
    }

    private async Task AddToRolesAsync(AppUser appUser, IEnumerable<string> appRoles)
    {
        var result = await userManager.AddToRolesAsync(appUser, appRoles);
        if (!result.Succeeded) throw new AppUserIdentityException(L, result.Errors);
    }

    private async Task RemoveFromRolesAsync(AppUser appUser, IEnumerable<string> appRoles)
    {
        var result = await userManager.RemoveFromRolesAsync(appUser, appRoles);
        if (!result.Succeeded) throw new AppUserIdentityException(L, result.Errors);
    }

    private async Task UserRolesControlAsync(ICollection<string> roles)
    {
        foreach (string roleName in roles)
        {
            var result = await context.Roles
                .WhereIf(IsSoftDeleteFilterEnabled, e => e.IsDeleted == false)
                .WhereIf(IsMultiTenantFilterEnabled, e => e.TenantId == CurrentTenantId)
                .FirstOrDefaultAsync(x => x.Name == roleName);

            if (result == null)
            {
                throw new AppRoleNotFoundException(L, roleName);
            }
        }
    }

    private async Task DuplicateControlAsync(AppUser newAppUser)
    {
        var result = await FindAsync(x => x.NormalizedUserName == newAppUser.NormalizedUserName);
        if (result != null)
        {
            throw new AppUserUsernameDuplicateException(L, newAppUser.NormalizedUserName);
        }

        result = await FindAsync(x => x.NormalizedEmail == newAppUser.NormalizedEmail);
        if (result != null)
        {
            throw new AppUserEmailDuplicateException(L, newAppUser.NormalizedEmail);
        }
    }

    private async Task DuplicateControlForUpdateAsync(AppUser oldAppUser)
    {
        var result = await FindAsync(x =>
            x.Id != oldAppUser.Id
            //unique parameters
            && x.NormalizedUserName == oldAppUser.NormalizedUserName
        );
        if (result != null)
        {
            throw new AppUserUsernameDuplicateException(L, oldAppUser.NormalizedUserName);
        }

        result = await FindAsync(x =>
            x.Id != oldAppUser.Id
            //unique parameters
            && x.NormalizedEmail == oldAppUser.NormalizedEmail
        );
        if (result != null)
        {
            throw new AppUserEmailDuplicateException(L, oldAppUser.NormalizedEmail);
        }
    }
}