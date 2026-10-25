using System.ComponentModel.DataAnnotations;
using HsnSoft.Base;
using Xyz.AuthServer.Domain.UserHistoryDomain.Consts;

namespace Xyz.AuthServer.Application.Contracts.UserHistoryDomain.Dtos.Submits;

public sealed class UserHistoryUpdateDto
{
    [Required]
    [NotDefault]
    public Guid? Id { get; set; }

    [Required]
    [StringLength(UserHistoryConsts.ActionMaxLength)]
    public string? Action { get; set; }

    [NotWhiteSpace]
    [StringLength(UserHistoryConsts.NoteMaxLength)]
    public string? Note { get; set; }
}
