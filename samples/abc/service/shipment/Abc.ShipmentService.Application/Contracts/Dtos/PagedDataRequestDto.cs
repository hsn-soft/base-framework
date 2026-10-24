using System.ComponentModel.DataAnnotations;

namespace Abc.ShipmentService.Application.Contracts.Dtos;

/// <summary>Which page of a list is asked for.</summary>
public class PagedDataRequestDto
{
    [Range(0, int.MaxValue)]
    public int Skip { get; set; }

    [Range(1, 100)]
    public int Take { get; set; } = 20;
}
