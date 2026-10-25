using HsnSoft.Base.Application.Dtos;

namespace Abc.IdentityService.Application.Contracts.UserHistoryDomain.Dtos;

/// <summary>What a search shows of a history record: enough to recognize and pick one.</summary>
public sealed class UserHistorySearchDto : EntityDto<Guid>
{
    public string UserName { get; init; } = string.Empty;

    public string Action { get; init; } = string.Empty;
}
