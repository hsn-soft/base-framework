using System.ComponentModel.DataAnnotations;
using HsnSoft.Base.Application.Dtos;

namespace Xyz.AuthServer.Application.Contracts.UserHistoryDomain.Dtos.Filters;

public sealed class GetUserHistoriesFilter : SortedAndLimitedDataRequestDto
{
    /// <summary>Only the history of this user.</summary>
    public Guid? UserId { get; set; }

    /// <summary>A part of the user name.</summary>
    [StringLength(100)]
    public string? UserName { get; set; }

    /// <summary>What happened (<c>created</c>, <c>updated</c>, <c>deleted</c>, <c>login</c>, ...); the exact word.</summary>
    [StringLength(32)]
    public string? Action { get; set; }
}
