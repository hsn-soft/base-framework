using HsnSoft.Base.Application.Dtos;
using JetBrains.Annotations;

namespace Hhs.IdentityService.Application.Contracts.AppUserDomain.Dtos;

public sealed class AppUserDto : EntityDto<Guid>
{
    public Guid TenantId { get; set; }

    [CanBeNull] public string TenantName { get; set; }

    public bool IsStatic { get; set; }

    [NotNull] public string UserName { get; set; } = string.Empty;
    [NotNull] public string Email { get; set; } = string.Empty;

    public bool EmailConfirmed { get; set; }

    [CanBeNull] public string PhoneNumber { get; set; }
    public bool PhoneNumberConfirmed { get; set; }

    [CanBeNull] public string DisplayName { get; set; }


    [CanBeNull] public string LanguageCode { get; set; }


    [CanBeNull] public string AvatarSuffixUrl { get; set; }

    public List<string> Roles { get; set; } = [];
}