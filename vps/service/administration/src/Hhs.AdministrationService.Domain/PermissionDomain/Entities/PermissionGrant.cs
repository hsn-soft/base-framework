using Hhs.AdministrationService.Domain.PermissionDomain.Consts;
using Hhs.Shared.Localization;
using HsnSoft.Base.Domain.Entities;
using JetBrains.Annotations;

namespace Hhs.AdministrationService.Domain.PermissionDomain.Entities;

public sealed class PermissionGrant : Entity<Guid>
{
    [NotNull]
    public string Name { get; private set; }

    [NotNull]
    public string ProviderName { get; private set; }

    [NotNull]
    public string ProviderKey { get; private set; }

    private PermissionGrant()
    {
        Name = string.Empty;
        ProviderName = string.Empty;
        ProviderKey = string.Empty;
    }

    internal PermissionGrant(Guid id,
        string name,
        string providerName,
        string providerKey) : this()
    {
        Id = id;

        SetName(name);
        SetProviderName(providerName);
        SetProviderKey(providerKey);
    }

    public void SetName(string name)
    {
        Name = LocalizedModelValidator.NotNull(name, $"{nameof(PermissionGrant)}:{nameof(Name)}", PermissionGrantConsts.NameMaxLength);
    }

    public void SetProviderName(string providerName)
    {
        ProviderName = LocalizedModelValidator.NotNull(providerName, $"{nameof(PermissionGrant)}:{nameof(ProviderName)}", PermissionGrantConsts.ProviderNameMaxLength);
    }

    public void SetProviderKey(string providerKey)
    {
        ProviderKey = LocalizedModelValidator.NotNull(providerKey, $"{nameof(PermissionGrant)}:{nameof(ProviderKey)}", PermissionGrantConsts.ProviderKeyMaxLength);
    }
}