using System.ComponentModel.DataAnnotations;
using Abc.IdentityService.Domain.UserDomain.Consts;

namespace Abc.IdentityService.Application.Contracts.UserDomain.Dtos.Submits;

public sealed class UserNameCheckDto
{
    [Required]
    [StringLength(UserConsts.UserNameMaxLength)]
    public string UserName { get; set; } = string.Empty;
}
