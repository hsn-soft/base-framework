using System.ComponentModel.DataAnnotations;
using Abc.ShipmentService.Domain.ShipmentHistoryDomain.Consts;
using HsnSoft.Base;

namespace Abc.ShipmentService.Application.Contracts.ShipmentHistoryDomain.Dtos.Submits;

/// <summary>What can be corrected of a record: its outcome and its note (which parcel it was is a fact).</summary>
public sealed class ShipmentHistoryUpdateDto
{
    [Required]
    [NotDefault]
    public Guid? Id { get; set; }

    [Required]
    [StringLength(ShipmentHistoryConsts.OutcomeMaxLength)]
    public string? Outcome { get; set; }

    [NotWhiteSpace]
    [StringLength(ShipmentHistoryConsts.NoteMaxLength)]
    public string? Note { get; set; }
}
