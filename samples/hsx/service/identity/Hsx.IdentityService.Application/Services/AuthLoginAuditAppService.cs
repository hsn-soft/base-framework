using System.Linq.Expressions;
using HsnSoft.Base.Application;
using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Authorization;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.Reflection;
using HsnSoft.Base.Text;
using Hsx.IdentityService.Application.Contracts.AuthLoginAuditDomain.Dtos;
using Hsx.IdentityService.Application.Contracts.AuthLoginAuditDomain.Dtos.Filters;
using Hsx.IdentityService.Application.Contracts.AuthLoginAuditDomain.Services;
using Hsx.IdentityService.Domain.AuthLoginAuditDomain.Consts;
using Hsx.IdentityService.Domain.AuthLoginAuditDomain.Entities;
using Hsx.IdentityService.Domain.AuthLoginAuditDomain.Exceptions;
using Hsx.IdentityService.Domain.AuthLoginAuditDomain.Repositories;
using Hsx.Shared.Contracts.Permissions;

namespace Hsx.IdentityService.Application.Services;

public sealed class AuthLoginAuditAppService(IAuthLoginAuditRepository audits, IPermissionChecker permissions, TimeProvider time, IServiceProvider services) : ApplicationServiceBase(services), IAuthLoginAuditAppService
{
    public async Task<AuthLoginAuditDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // the limit is asked first: a caller that has none is refused before a query is made (framework ADR-0083)
        DateTimeOffset oldest = await OldestReadableAsync(cancellationToken).ConfigureAwait(false);
        AuthLoginAuditDto? audit = await audits.FindAsync<AuthLoginAuditDto>(id, cancellationToken: cancellationToken).ConfigureAwait(false);
        return audit is not null && audit.CreatedAt >= oldest && IsOfCallersTenants(audit.TenantId) ? audit : throw AuthLoginAuditNotFound.For(id);
    }

    public async Task<PagedDataResultDto<AuthLoginAuditDto>> GetPagedListAsync(GetAuthLoginAuditsPagedDto pagedInput, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagedInput);

        DateTimeOffset oldest = await OldestReadableAsync(cancellationToken).ConfigureAwait(false);
        PagedQueryResult<AuthLoginAuditDto> page = await audits.GetPagedListAsync<AuthLoginAuditDto>(
            pagedInput.ToPagedOptions(Filter(pagedInput, oldest), AuthLoginAuditSorting.Fields, AuthLoginAuditSorting.Default), cancellationToken: cancellationToken).ConfigureAwait(false);
        return page.ToPagedDto(pagedInput);
    }

    // the moment before which the caller's role does not let it read: now less the days of the constraint
    private async Task<DateTimeOffset> OldestReadableAsync(CancellationToken cancellationToken)
    {
        int days = await permissions.RequireIntAsync(IdentityPermissions.Constraints.AuditLogs.MaxDays, cancellationToken).ConfigureAwait(false);
        return time.GetUtcNow().AddDays(-Math.Max(days, 0));
    }

    // the tenants of the caller's token (itself and the ones under it); the system tenant sees them all, and the sign-ins of users nobody knows
    private bool IsOfCallersTenants(Guid? tenantId)
        => CurrentTenant.IsSystemTenant || (tenantId is { } id && CurrentTenant.AllowedTenantIds.Contains(id));

    private Expression<Func<AuthLoginAudit, bool>> Filter(GetAuthLoginAuditsPagedDto input, DateTimeOffset oldest)
    {
        string? search = string.IsNullOrWhiteSpace(input.SearchText) ? null : StringHelper.Normalize(input.SearchText);
        DateTimeOffset since = input.From is { } from && from > oldest ? from : oldest;
        Guid[] visible = [.. CurrentTenant.AllowedTenantIds];
        bool system = CurrentTenant.IsSystemTenant;
        return new FilterBuilder<AuthLoginAudit>()
            .And(audit => audit.CreatedAt >= since)
            .And(system ? null : audit => audit.TenantId != null && visible.Contains(audit.TenantId.Value))
            .And(search is null ? null : audit => audit.NormalizedUserNameOrEmail.Contains(search))
            .And(input.UserId is null ? null : audit => audit.UserId == input.UserId.Value)
            .And(input.TenantId is null ? null : audit => audit.TenantId == input.TenantId.Value)
            .And(input.IsSuccess is null ? null : audit => audit.IsSuccess == input.IsSuccess.Value)
            .And(input.To is null ? null : audit => audit.CreatedAt <= input.To.Value)
            .Build();
    }
}
