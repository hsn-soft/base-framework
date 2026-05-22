using Hhs.IdentityService.Domain.AuthDomain.Consts;
using Hhs.IdentityService.Domain.AuthDomain.Entities;
using Hhs.IdentityService.Domain.AuthDomain.Exceptions;
using Hhs.IdentityService.Domain.AuthDomain.Repositories;
using Hhs.IdentityService.Domain.Localization;
using Hhs.IdentityService.Domain.TenantDomain.Exceptions;
using Hhs.IdentityService.EntityFrameworkCore.Context;
using Hhs.Shared.Helper.Utils;
using Hhs.Shared.Localization;
using HsnSoft.Base.Data;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.Validation.Localization;
using JetBrains.Annotations;
using Microsoft.Extensions.Localization;

namespace Hhs.IdentityService.EntityFrameworkCore.Repositories;

public class EfCoreAppRoleRepository : EfCoreGenericRepository<AppRole, Guid>, IAppRoleRepository
{
    [NotNull] private IStringLocalizer L { get; }

    private readonly IDataFilter _dataFilter;
    private readonly ICurrentTenant _currentTenant;

    public EfCoreAppRoleRepository(
        IServiceProvider provider,
        IStringLocalizerFactory stringLocalizerFactory,
        IdentityServiceDbContext dbContext, IDataFilter dataFilter, ICurrentTenant currentTenant) : base(provider, dbContext)
    {
        L = stringLocalizerFactory.CreateMultiple([typeof(IdentityServiceResource), typeof(ValidationResource), typeof(SharedResource)]);
        _dataFilter = dataFilter;
        _currentTenant = currentTenant;
    }

    public async Task<AppRole> CreateAsync(Guid tenantId,
        string name,
        bool isDefault = false,
        bool isStatic = false)
        => await CreateAsync(Guid.CreateVersion7(), tenantId,
            name,
            isDefault,
            isStatic);

    public async Task<AppRole> CreateAsync(Guid id, Guid tenantId,
        string name,
        bool isDefault = false,
        bool isStatic = false)
    {
        if (id == Guid.Empty) id = Guid.CreateVersion7();

        // Create draft AppRole
        var draftAppRole = new AppRole(
            id: id,
            tenantId: tenantId,
            name: name,
            isDefault: isDefault,
            isStatic: isStatic
        );

        //Domain Rule -> Tenant Management Access
        TenantCrudControl(draftAppRole.TenantId);

        //Domain Rule -> AppRole must be unique
        await DuplicateControlAsync(draftAppRole);

        _ = await InsertAsync(draftAppRole);
        return draftAppRole;
    }

    public async Task<AppRole> UpdateAsync(Guid id,
        string name,
        bool isDefault = false,
        bool isStatic = false)
    {
        var oldAppRole = await GetSingleOrDefaultAsync(x => x.Id == id);
        if (oldAppRole == null)
        {
            throw new AppRoleNotFoundException(L, id.ToString());
        }

        name = StringOperations.SplitFirstValue(name, "#");

        oldAppRole.SetName(name);
        oldAppRole.IsDefault = isDefault;
        oldAppRole.IsStatic = isStatic;

        //Domain Rule -> AppRole must be unique
        await DuplicateControlForUpdateAsync(oldAppRole);

        //Domain Rules
        // Rule01
        // Rule02
        _ = await UpdateAsync(oldAppRole);
        return oldAppRole;
    }

    public async Task DeleteAsync(Guid id)
    {
        var appRole = await GetSingleOrDefaultAsync(x => x.Id == id);
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

        await UpdateAsync(appRole);
    }

    private void TenantCrudControl(Guid targetTenantId)
    {
        if (!_dataFilter.IsEnabled<IMultiTenant>()) return;

        if (_currentTenant.IsSystemTenant) return;

        if (_currentTenant.AllowedTenantIds.Contains(targetTenantId)) return;

        throw new UnauthorizedTenantException(L, targetTenantId.ToString());
    }

    private async Task DuplicateControlAsync(AppRole newAppRole)
    {
        var old = await GetSingleOrDefaultAsync(x =>
            x.Id == newAppRole.Id
            || (x.TenantId == newAppRole.TenantId && x.NormalizedName == newAppRole.NormalizedName)
        );
        if (old != null)
        {
            throw new AppRoleNameDuplicateException(L, newAppRole.NormalizedName).WithData(nameof(newAppRole.TenantId), newAppRole.TenantId);
        }
    }

    private async Task DuplicateControlForUpdateAsync(AppRole oldAppRole)
    {
        var result = await GetSingleOrDefaultAsync(x =>
            x.Id != oldAppRole.Id &&
            //unique parameters
            x.TenantId == oldAppRole.TenantId && x.NormalizedName == oldAppRole.NormalizedName
        );

        if (result != null)
        {
            throw new AppRoleNameDuplicateException(L, oldAppRole.NormalizedName);
        }
    }
}