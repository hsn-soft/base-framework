using HsnSoft.Base.Application.Dtos;

namespace Abc.IdentityService.Application.Contracts.UserHistoryDomain.Dtos;

public sealed class UserHistoryDto : AuditedEntityDto<Guid>
{
    public Guid UserId { get; init; }

    public string UserName { get; init; } = string.Empty;

    public string Action { get; init; } = string.Empty;

    public string? Note { get; init; }

    public Guid TenantId { get; init; }
}
