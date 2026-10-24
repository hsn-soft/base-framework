using System.ComponentModel.DataAnnotations;

namespace Abc.OrderService.Application.Contracts.Dtos;

/// <summary>A list that is not paged: how many it may have at most.</summary>
public class LimitedDataRequestDto
{
    [Range(1, 1000)]
    public int Limit { get; set; } = 1000;
}
