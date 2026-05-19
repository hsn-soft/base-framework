using System.Globalization;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Security.Claims;
using Hhs.IdentityService.Domain.AppRoleDomain.Consts;
using Hhs.IdentityService.Domain.AppRoleDomain.Entities;
using Hhs.IdentityService.Domain.AppRoleDomain.Exceptions;
using Hhs.IdentityService.Domain.AppRoleDomain.Repositories;
using Hhs.IdentityService.Domain.Localization;
using Hhs.IdentityService.EntityFrameworkCore.Context;
using Hhs.Shared.Helper.Utils;
using Hhs.Shared.Localization;
using HsnSoft.Base;
using HsnSoft.Base.Data;
using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.Validation.Localization;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Hhs.IdentityService.EntityFrameworkCore.Repositories;

public class AppRoleRepository : IAppRoleRepository
{
    private readonly IdentityAppDbContext _context;
    private readonly RoleManager<AppRole> _roleManager;

    [NotNull]
    protected IStringLocalizer L { get; }

    [CanBeNull]
    private IDataFilter DataFilter { get; }

    [CanBeNull]
    private ICurrentTenant CurrentTenant { get; }

    private Guid? CurrentTenantId => CurrentTenant?.Id;

    private bool IsMultiTenantFilterEnabled => CurrentTenantId != null && (DataFilter?.IsEnabled<IMultiTenant>() ?? false);

    private bool IsSoftDeleteFilterEnabled => DataFilter?.IsEnabled<ISoftDelete>() ?? false;

    public AppRoleRepository(IdentityAppDbContext context,
        IStringLocalizerFactory stringLocalizerFactory,
        IDataFilter dataFilter,
        ICurrentTenant currentTenant,
        RoleManager<AppRole> roleManager)
    {
        _context = context;
        DataFilter = dataFilter;
        CurrentTenant = currentTenant;
        _roleManager = roleManager;

        L = stringLocalizerFactory.CreateMultiple(new List<Type>
        {
            typeof(IdentityServiceResource),
            typeof(ValidationResource),
            typeof(SharedResource)
        });
    }

    public async Task<List<AppRole>> GetPagedListWithFiltersAsync(Guid? tenantId,
        string name = null,
        bool? isDefault = null,
        bool? isStatic = null,
        bool? isPublic = null,
        string sorting = null,
        int maxResultCount = int.MaxValue,
        int skipCount = 0,
        CancellationToken cancellationToken = default
    )
    {
        var query = ApplyFilter(_context.Roles.AsQueryable(), tenantId, null,
            name, isDefault, isStatic, isPublic);

        // TODO: Convert new paging list
        return await query
            .OrderBy(string.IsNullOrWhiteSpace(sorting) ? AppRoleConsts.GetDefaultSorting(false) : sorting)
            .PageBy(0, maxResultCount)
            .ToListAsync(cancellationToken: cancellationToken);
    }

    public async Task<long> GetCountWithFiltersAsync(Guid? tenantId,
        string name = null,
        bool? isDefault = null,
        bool? isStatic = null,
        bool? isPublic = null,
        CancellationToken cancellationToken = default
    )
    {
        var query = ApplyFilter(_context.Roles.AsQueryable(), tenantId, null,
            name, isDefault, isStatic, isPublic);

        return await query.LongCountAsync(cancellationToken: cancellationToken);
    }

    public async Task<List<AppRole>> GetFilterListAsync(Guid? tenantId,
        string name = null,
        bool? isDefault = null,
        bool? isStatic = null,
        bool? isPublic = null,
        string sorting = null,
        CancellationToken cancellationToken = default
    )
    {
        var query = ApplyFilter(_context.Roles.AsQueryable(), tenantId, null,
            name, isDefault, isStatic, isPublic);

        return await query
            .OrderBy(string.IsNullOrWhiteSpace(sorting) ? AppRoleConsts.GetDefaultSorting(false) : sorting)
            .ToListAsync(cancellationToken: cancellationToken);
    }

    public async Task<List<AppRole>> GetSearchListAsync(Guid? tenantId,
        string searchText = null,
        string sorting = null,
        int maxResultCount = int.MaxValue,
        CancellationToken cancellationToken = default
    )
    {
        var query = ApplyFilter(_context.Roles.AsQueryable(), tenantId, searchText);

        return await query
            .OrderBy(string.IsNullOrWhiteSpace(sorting) ? AppRoleConsts.GetDefaultSorting(false) : sorting)
            .PageBy(0, maxResultCount)
            .ToListAsync(cancellationToken: cancellationToken);
    }

    public async Task<AppRole> FindWithIdAsync(Guid id)
        => await FindAsync(x => x.Id == id);

    public async Task<AppRole> FindAsync(Expression<Func<AppRole, bool>> predicate)
        => await _context.Roles
            .WhereIf(IsSoftDeleteFilterEnabled, e => e.IsDeleted == false)
            .WhereIf(IsMultiTenantFilterEnabled, e => e.TenantId == CurrentTenantId)
            .FirstOrDefaultAsync(predicate);

    public async Task<AppRole> CreateAsync(Guid tenantId, string tenantDomain,
        string name,
        bool isDefault,
        bool isStatic,
        bool isPublic)
        => await CreateAsync(Guid.NewGuid(), tenantId, tenantDomain,
            name,
            isDefault,
            isStatic,
            isPublic);

    public async Task<AppRole> CreateAsync(Guid id, Guid tenantId, string tenantDomain,
        string name,
        bool isDefault,
        bool isStatic,
        bool isPublic)
    {
        if (id == Guid.Empty) id = Guid.NewGuid();

        name = StringOperations.SplitFirstValue(name, "#");
        string checkedRoleName = StringOperations.ReplaceInvalidChars(name, false, "-").ToLower(new CultureInfo("en-US"));
        string roleDisplayName = checkedRoleName;
        string roleTenantDomain = StringOperations.ReplaceInvalidChars(tenantDomain, false, "-").ToLower(new CultureInfo("en-US"));

        checkedRoleName = $"{checkedRoleName}#{roleTenantDomain}";

        // Create draft AppRole
        var draftAppRole = new AppRole(
            id: id,
            name: checkedRoleName,
            isDefault: isDefault,
            isStatic: isStatic,
            isPublic: isPublic,
            tenantId: tenantId,
            tenantDomain: roleTenantDomain
        );

        //Domain Rule -> AppRole must be unique
        await DuplicateControlAsync(draftAppRole);

        var result = await _roleManager.CreateAsync(draftAppRole);
        if (!result.Succeeded) throw new AppRoleIdentityException(L, result.Errors);

        // add role claims
        await AddClaimAsync(draftAppRole, new Claim("role_name",
            StringOperations.FirstCharCapitalize(roleDisplayName, new[] { "-", "_" })));

        return draftAppRole;
    }

    public async Task<AppRole> UpdateAsync(Guid id,
        string name,
        bool isDefault,
        bool isStatic,
        bool isPublic)
    {
        var oldAppRole = await FindWithIdAsync(id);
        if (oldAppRole == null)
        {
            throw new AppRoleNotFoundException(L, id.ToString());
        }

        name = StringOperations.SplitFirstValue(name, "#");
        string checkedRoleName = StringOperations.ReplaceInvalidChars(name, false, "-").ToLower(new CultureInfo("en-US"));
        string roleDisplayName = checkedRoleName;
        string roleTenantDomain = StringOperations.ReplaceInvalidChars(oldAppRole.TenantDomain, false, "-").ToLower(new CultureInfo("en-US"));

        checkedRoleName = $"{checkedRoleName}#{roleTenantDomain}";

        oldAppRole.SetName(checkedRoleName);
        oldAppRole.IsDefault = isDefault;
        oldAppRole.IsStatic = isStatic;
        oldAppRole.IsPublic = isPublic;

        //Domain Rule -> AppRole must be unique
        await DuplicateControlForUpdateAsync(oldAppRole);

        var result = await _roleManager.UpdateAsync(oldAppRole);
        if (!result.Succeeded) throw new AppRoleIdentityException(L, result.Errors);

        // remove role claims
        var claims = await _roleManager.GetClaimsAsync(oldAppRole);
        foreach (var claim in claims)
        {
            await RemoveClaimAsync(oldAppRole, claim);
        }

        // add role claims
        await AddClaimAsync(oldAppRole, new Claim("role_name",
            StringOperations.FirstCharCapitalize(roleDisplayName, new[] { "-", "_" })));

        return oldAppRole;
    }

    public async Task DeleteAsync(Guid id)
    {
        var appRole = await FindWithIdAsync(id);
        if (appRole == null)
        {
            throw new AppRoleNotFoundException(L, id.ToString());
        }

        string guidGenerated = Guid.NewGuid().ToString("N").ToUpper();
        string uniqueRoleName = guidGenerated + "_" + appRole.Name;
        if (uniqueRoleName.Length > AppRoleConsts.NameMaxLength)
        {
            uniqueRoleName = uniqueRoleName[..AppRoleConsts.NameMaxLength];
        }

        appRole.SetName(uniqueRoleName);
        appRole.IsDeleted = true;
        var result = await _roleManager.UpdateAsync(appRole);
        if (!result.Succeeded) throw new AppRoleIdentityException(L, result.Errors);
    }

    private IQueryable<AppRole> ApplyFilter(
        IQueryable<AppRole> query,
        Guid? tenantId,
        [CanBeNull] string searchText = null,
        [CanBeNull] string name = null,
        bool? isDefault = null,
        bool? isStatic = null,
        bool? isPublic = null)
    {
        searchText = searchText?.ToLower(new CultureInfo("en-US"));
        name = name?.ToLower(new CultureInfo("en-US"));

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
            .WhereIf(!string.IsNullOrWhiteSpace(searchText), e => e.Name.ToLower(new CultureInfo("en-US")).Contains(searchText))
            .WhereIf(!string.IsNullOrWhiteSpace(name), e => e.Name.ToLower(new CultureInfo("en-US")).Contains(name))
            .WhereIf(isDefault.HasValue, e => e.IsDefault == isDefault)
            .WhereIf(isStatic.HasValue, e => e.IsStatic == isStatic)
            .WhereIf(isPublic.HasValue, e => e.IsPublic == isPublic);
    }

    private async Task AddClaimAsync(AppRole appRole, Claim claim)
    {
        var result = await _roleManager.AddClaimAsync(appRole, claim);
        if (!result.Succeeded) throw new AppRoleIdentityException(L, result.Errors);
    }

    private async Task RemoveClaimAsync(AppRole appRole, Claim claim)
    {
        var result = await _roleManager.RemoveClaimAsync(appRole, claim);
        if (!result.Succeeded) throw new AppRoleIdentityException(L, result.Errors);
    }

    private async Task DuplicateControlAsync(AppRole newAppRole)
    {
        var result = await FindAsync(x => x.NormalizedName == newAppRole.NormalizedName);
        if (result != null)
        {
            throw new AppRoleNameDuplicateException(L, newAppRole.NormalizedName);
        }
    }

    private async Task DuplicateControlForUpdateAsync(AppRole oldAppRole)
    {
        var result = await FindAsync(x =>
            x.Id != oldAppRole.Id
            //unique parameters
            && x.NormalizedName == oldAppRole.NormalizedName
        );

        if (result != null)
        {
            throw new AppRoleNameDuplicateException(L, oldAppRole.NormalizedName);
        }
    }
}