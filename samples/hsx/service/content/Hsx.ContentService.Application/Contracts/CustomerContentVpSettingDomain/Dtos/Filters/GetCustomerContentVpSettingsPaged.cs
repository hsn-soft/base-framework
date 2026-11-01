using System.ComponentModel.DataAnnotations;
using HsnSoft.Base.Application.Dtos;

namespace Hsx.ContentService.Application.Contracts.CustomerContentVpSettingDomain.Dtos.Filters;

public sealed class GetCustomerContentVpSettingsPaged : PagedDataRequestDto
{
    /// <summary>A part of the scope key.</summary>
    [StringLength(100)]
    public string? ScopeKey { get; set; }

    /// <summary>Only the is blocked.</summary>
    public bool? IsBlocked { get; set; }
}
