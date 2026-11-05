using System.Linq.Expressions;
using HsnSoft.Base.Application;
using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Domain.Sorting;
using HsnSoft.Base.Reflection;
using Hsx.IdentityService.Application.Contracts.PermissionDomain.Dtos;
using Hsx.IdentityService.Application.Contracts.PermissionDomain.Dtos.Filters;
using Hsx.IdentityService.Application.Contracts.PermissionDomain.Services;
using Hsx.IdentityService.Domain.Enums;
using Hsx.IdentityService.Domain.PermissionDomain.Entities;
using Hsx.IdentityService.Domain.PermissionDomain.Repositories;

namespace Hsx.IdentityService.Application.Services;

public sealed class PermissionAppService(IPermissionRepository permissions, IServiceProvider services) : ApplicationServiceBase(services), IPermissionAppService
{
    private static readonly SortFields<Permission> s_sorting = new SortFields<Permission>().Add("code", permission => permission.Code).Add("type", permission => permission.Type);

    public async Task<ListDataResultDto<PermissionDto>> GetFilterListAsync(GetPermissionsFilterDto filterInput, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filterInput);

        IReadOnlyList<Permission> found = await permissions.GetListAsync(filterInput.ToListOptions(Filter(filterInput), s_sorting, "code asc"), cancellationToken).ConfigureAwait(false);
        return new ListDataResultDto<PermissionDto>([.. found.Select(permission => new PermissionDto { Id = permission.Id, Code = permission.Code, Type = permission.Type, Name = permission.Name, Description = permission.Description, IsActive = permission.IsActive })]);
    }

    private static Expression<Func<Permission, bool>> Filter(GetPermissionsFilterDto input)
    {
        string? code = string.IsNullOrWhiteSpace(input.Code) ? null : input.Code.Trim().ToLowerInvariant();
        PermissionType? type = input.Type;
        return new FilterBuilder<Permission>()
            .And(code is null ? null : permission => permission.Code.Contains(code))
            .And(type is null ? null : permission => permission.Type == type.Value)
            .Build();
    }
}
