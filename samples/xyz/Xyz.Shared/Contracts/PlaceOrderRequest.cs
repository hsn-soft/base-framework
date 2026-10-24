using HsnSoft.Base.Logging.Masking;

namespace Xyz.Shared.Contracts;

/// <summary>
/// The request body of an order. It is a contract of the system, so the gateway sees it too: what is marked <see cref="SensitiveDataAttribute"/>
/// is hidden in every log of it, in the order service and in the gateway, as the attribute says.
/// </summary>
public sealed record PlaceOrderRequest
{
    public Guid ProductId { get; init; }

    public int Quantity { get; init; }

    /// <summary>Only the last four digits stay visible.</summary>
    [SensitiveData(ShowLast = 4, PreserveLength = true)]
    public string? CardNumber { get; init; }

    [SensitiveData]
    public string? Phone { get; init; }
}
