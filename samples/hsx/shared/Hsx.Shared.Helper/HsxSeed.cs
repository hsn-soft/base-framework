namespace Hsx.Shared.Helper;

/// <summary>
/// The demo data of the Hsx sample that more than one service needs to agree on: the company <c>ilbak</c> with its customer <c>cnbce</c>, to which three resellers sell a product each: audio-time the video platform, tech-summus the podcast, epartner the e-invoice. The scope keys follow
/// from the customer and the product, so the content service writes its demo content under the same keys the identity service puts into the tokens.
/// </summary>
public static class HsxSeed
{
    public static readonly Guid CompanyId = new("00000000-0000-7000-8000-00000000b001");

    public static readonly Guid CustomerId = new("00000000-0000-7000-8000-00000000b002");

    public const string CompanyName = "ilbak";

    public const string CustomerDomain = "cnbce.com";

    /// <summary>The scope key of the video platform of the demo customer (sold by audio-time).</summary>
    public static string VideoPlatformScope => ScopeKeyHelper.Generate(CustomerId, ProductTypes.VideoPlatform);

    /// <summary>The scope key of the podcast of the demo customer (sold by tech-summus).</summary>
    public static string PodcastScope => ScopeKeyHelper.Generate(CustomerId, ProductTypes.Podcast);

    /// <summary>The scope key of the e-invoice of the demo customer (sold by epartner).</summary>
    public static string EInvoiceScope => ScopeKeyHelper.Generate(CustomerId, ProductTypes.EInvoice);

    /// <summary>The password of every demo user (a sample; a real system never has one).</summary>
    public const string DemoPassword = "Hsx-Demo-123";
}
