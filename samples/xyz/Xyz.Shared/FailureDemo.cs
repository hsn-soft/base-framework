namespace Xyz.Shared;

/// <summary>Quantities that make the sample fail on purpose, so the logs of a failure can be looked at (see samples/README.md).</summary>
public static class FailureDemo
{
    /// <summary>The order request throws.</summary>
    public const int RequestQuantity = 13;

    /// <summary>The handler of the product service throws: the order is saved, the message fails.</summary>
    public const int HandlerQuantity = 14;
}
