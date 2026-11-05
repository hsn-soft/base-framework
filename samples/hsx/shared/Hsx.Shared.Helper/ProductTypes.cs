namespace Hsx.Shared.Helper;

/// <summary>The products that are sold to a customer (a subscription is a product of a customer). Every value is its own (hhs had three that were equal).</summary>
public enum ProductTypes
{
    Unknown = 0,

    /// <summary>The video platform.</summary>
    VideoPlatform = 11,

    /// <summary>The vertical video.</summary>
    VerticalVideo = 12,

    /// <summary>The podcast.</summary>
    Podcast = 13,

    /// <summary>The e-invoice.</summary>
    EInvoice = 14,
}

public static class ProductTypesExtensions
{
    /// <summary>The short code of a product, which is part of a scope key: <c>vp</c>, <c>cc</c>, <c>pc</c>, <c>ei</c>.</summary>
    public static string ToCode(this ProductTypes product) => product switch
    {
        ProductTypes.VideoPlatform => "vp",
        ProductTypes.VerticalVideo => "cc",
        ProductTypes.Podcast => "pc",
        ProductTypes.EInvoice => "ei",
        _ => throw new ArgumentOutOfRangeException(nameof(product), product, "The product is unknown."),
    };
}
