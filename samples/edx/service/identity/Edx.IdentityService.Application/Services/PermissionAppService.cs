using System.Linq.Expressions;
using Edx.IdentityService.Application.Contracts.IdentityDomain.Dtos;
using Edx.IdentityService.Application.Contracts.IdentityDomain.Dtos.Filters;
using Edx.IdentityService.Application.Contracts.IdentityDomain.Services;
using Edx.IdentityService.Domain.IdentityDomain.Entities;
using HsnSoft.Base;
using HsnSoft.Base.Application;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.Domain.Sorting;
using HsnSoft.Base.Reflection;

namespace Edx.IdentityService.Application.Services;

public sealed class PermissionAppService(IRepository<Permission, Guid> permissions, IServiceProvider services) : ApplicationService(services), IPermissionAppService
{
    private static readonly SortFields<Permission> s_sorting = new SortFields<Permission>().Add("code", permission => permission.Code).Add("type", permission => permission.Type);

    public async Task<List<PermissionDto>> GetFilterListAsync(GetPermissionsFilter filterInput, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filterInput);

        IReadOnlyList<Permission> found = await permissions.GetListAsync(filterInput.ToListOptions(Filter(filterInput), s_sorting, "code asc"), cancellationToken).ConfigureAwait(false);
        return [.. found.Select(permission => new PermissionDto { Id = permission.Id, Code = permission.Code, Type = permission.Type.ToString(), Name = permission.Name, IsActive = permission.IsActive })];
    }

    private static Expression<Func<Permission, bool>> Filter(GetPermissionsFilter input)
    {
        string? code = string.IsNullOrWhiteSpace(input.Code) ? null : input.Code.Trim().ToLowerInvariant();
        PermissionType? type = string.IsNullOrWhiteSpace(input.Type)
            ? null
            : Enum.TryParse(input.Type.Trim(), ignoreCase: true, out PermissionType parsed) && Enum.IsDefined(parsed)
                ? parsed
                : throw new BusinessException("filter_value_invalid", $"'{input.Type}' is not a permission type.").WithKey("filter_value_invalid", nameof(input.Type), input.Type);
        return new FilterBuilder<Permission>()
            .And(code is null ? null : permission => permission.Code.Contains(code))
            .And(type is null ? null : permission => permission.Type == type.Value)
            .Build();
    }
}
