using System.ComponentModel.DataAnnotations;

namespace Abc.OrderService.Application.Contracts.Dtos;

/// <summary>A search by a piece of text (for a box that suggests while one types): how many answers at most.</summary>
public class SearchDataRequestDto
{
    [StringLength(100)]
    public string? SearchText { get; set; }

    [Range(1, 100)]
    public int Limit { get; set; } = 20;
}
