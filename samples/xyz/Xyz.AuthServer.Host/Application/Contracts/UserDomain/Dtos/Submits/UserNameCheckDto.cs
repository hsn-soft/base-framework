using System.ComponentModel.DataAnnotations;
using Xyz.AuthServer.Domain.UserDomain.Consts;

namespace Xyz.AuthServer.Application.Contracts.UserDomain.Dtos.Submits;

public sealed class UserNameCheckDto
{
    [Required]
    [StringLength(UserConsts.UserNameMaxLength)]
    public string UserName { get; set; } = string.Empty;
}
