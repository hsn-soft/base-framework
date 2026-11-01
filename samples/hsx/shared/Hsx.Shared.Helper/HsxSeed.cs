namespace Hsx.Shared.Helper;

/// <summary>
/// The demo data of the Hsx sample that more than one service needs to agree on: one company with one customer, which two resellers were sold different products (Techsummus the video platform, Pixel the vertical video). The scope keys
/// follow from the customer, so the content service writes its demo content under the same keys the identity service puts into the tokens.
/// </summary>
public static class HsxSeed
{
    public static readonly Guid CompanyId = new("00000000-0000-7000-8000-00000000b001");

    public static readonly Guid CustomerId = new("00000000-0000-7000-8000-00000000b002");

    public const string CompanyName = "demo-company";

    public const string CustomerDomain = "demo.example.com";

    /// <summary>The scope key of the video platform of the demo customer (sold by Techsummus).</summary>
    public static string VideoPlatformScope => ScopeKeyHelper.Generate(CustomerId, ProductTypes.VideoPlatform);

    /// <summary>The scope key of the vertical video of the demo customer (sold by Pixel).</summary>
    public static string VerticalVideoScope => ScopeKeyHelper.Generate(CustomerId, ProductTypes.VerticalVideo);

    /// <summary>The password of every demo user (a sample; a real system never has one).</summary>
    public const string DemoPassword = "Hsx-Demo-123";
}
