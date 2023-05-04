﻿using System.Threading.Tasks;
using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.Security.Claims;

namespace HsnSoft.Base.Authorization.Permissions;

public class ClientPermissionValueProvider : PermissionValueProvider
{
    public const string ProviderName = "C";

    public override string Name => ProviderName;

    protected ICurrentTenant CurrentTenant { get; }

    public ClientPermissionValueProvider(IPermissionStore permissionStore, ICurrentTenant currentTenant) : base(permissionStore)
    {
        CurrentTenant = currentTenant;
    }

    public override async Task<PermissionGrantResult> CheckAsync(PermissionValueCheckContext context)
    {
        var clientId = context.Principal?.FindFirst(BaseClaimTypes.ClientId)?.Value ?? context.Principal?.FindFirst("client_id")?.Value;

        if (clientId == null)
        {
            return PermissionGrantResult.Undefined;
        }

        using (CurrentTenant.Change(null))
        {
            return await PermissionStore.IsGrantedAsync(context.Permission.Name, Name, clientId)
                ? PermissionGrantResult.Granted
                : PermissionGrantResult.Undefined;
        }
    }
}