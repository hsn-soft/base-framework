using System.ComponentModel.DataAnnotations;
using HsnSoft.Base;
using Xyz.ShipmentService.Domain.ShipmentHistoryDomain.Consts;

namespace Xyz.ShipmentService.Application.Contracts.ShipmentHistoryDomain.Dtos.Submits;

public sealed class ShipmentHistoryCreateDto
{
    [Required]
    [NotDefault]
    public Guid? ShipmentId { get; set; }

    [Required]
    [NotDefault]
    public Guid? OrderId { get; set; }

    [Required]
    [StringLength(ShipmentHistoryConsts.TrackingNumberMaxLength)]
    public string? TrackingNumber { get; set; }

    [Required]
    [StringLength(ShipmentHistoryConsts.OutcomeMaxLength)]
    public string? Outcome { get; set; }

    [NotWhiteSpace]
    [StringLength(ShipmentHistoryConsts.NoteMaxLength)]
    public string? Note { get; set; }
}
