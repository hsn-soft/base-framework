using System.ComponentModel.DataAnnotations;
using Abc.IdentityService.Domain.UserHistoryDomain.Consts;
using HsnSoft.Base;

namespace Abc.IdentityService.Application.Contracts.UserHistoryDomain.Dtos.Submits;

public sealed class UserHistoryCreateDto
{
    [Required]
    [NotDefault]
    public Guid? UserId { get; set; }

    [Required]
    [StringLength(UserHistoryConsts.UserNameMaxLength)]
    public string? UserName { get; set; }

    [Required]
    [StringLength(UserHistoryConsts.ActionMaxLength)]
    public string? Action { get; set; }

    [NotWhiteSpace]
    [StringLength(UserHistoryConsts.NoteMaxLength)]
    public string? Note { get; set; }
}
