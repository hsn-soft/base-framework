using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HsnSoft.Base.Browser;

/// <summary>
/// A browser the application uses: the page after its scripts ran (<see cref="GetPageAsync"/>), a page as a PDF (<see cref="RenderPdfAsync"/>) and as an image (<see cref="ScreenshotAsync"/>), from an address or from HTML. The engine that
/// does it (<c>HsnSoft.Base.Browser.Puppeteer</c>) is the provider the host registers. A page that is rendered runs scripts and loads what it names, so <see cref="BrowserUrlPolicy"/> decides every address it may load.
/// </summary>
public interface IBrowserRenderer
{
    /// <summary>The page after its scripts ran: the HTML of the document, where it ended up and its title.</summary>
    Task<RenderedPage> GetPageAsync(BrowserRequest request, CancellationToken cancellationToken = default);

    /// <summary>The page as a PDF.</summary>
    Task<byte[]> RenderPdfAsync(BrowserRequest request, PdfOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>The page as an image.</summary>
    Task<byte[]> ScreenshotAsync(BrowserRequest request, ScreenshotOptions? options = null, CancellationToken cancellationToken = default);
}

/// <summary>When the page counts as loaded.</summary>
public enum WaitCondition
{
    /// <summary>The load event: the page and what it names (images, styles) arrived.</summary>
    Load,

    /// <summary>The document was read, its other resources may still come.</summary>
    DomContentLoaded,

    /// <summary>The network has been quiet for half a second: what the scripts fetch has arrived too (a page that builds itself in the browser).</summary>
    NetworkIdle,
}

/// <summary>What to render: an address (<see cref="Url"/>) or HTML (<see cref="Html"/>), not both.</summary>
public sealed record BrowserRequest
{
    /// <summary>The page to open (http or https).</summary>
    public Uri? Url { get; init; }

    /// <summary>HTML to render in place of an address (an invoice that was made as HTML). It runs scripts and loads what it names, as a page does.</summary>
    public string? Html { get; init; }

    /// <summary>When the page counts as loaded. Default: <see cref="WaitCondition.Load"/>.</summary>
    public WaitCondition WaitUntil { get; init; } = WaitCondition.Load;

    /// <summary>A selector that has to be in the page before the result is taken (an element the scripts add).</summary>
    public string? WaitForSelector { get; init; }

    /// <summary>The time the whole call may take, in seconds; not set: the setting of the provider.</summary>
    public int? TimeoutSeconds { get; init; }

    public int ViewportWidth { get; init; } = 1280;

    public int ViewportHeight { get; init; } = 800;

    public string? UserAgent { get; init; }

    /// <summary>Headers sent with every request of the page (an <c>Accept-Language</c>); never a user's token to an address from outside.</summary>
    public IReadOnlyDictionary<string, string>? Headers { get; init; }

    public static BrowserRequest FromUrl(Uri url) => new() { Url = url };

    public static BrowserRequest FromHtml(string html) => new() { Html = html };

    /// <summary>Whether the request says what to render, once, and the sizes make sense; an <see cref="ArgumentException"/> says what is wrong.</summary>
    public void Validate()
    {
        if (Url is null == string.IsNullOrEmpty(Html))
        {
            throw new ArgumentException("A request has an address or HTML, one of them.", nameof(BrowserRequest));
        }

        if (Url is not null && (!Url.IsAbsoluteUri || Url.Scheme is not ("http" or "https")))
        {
            throw new ArgumentException("The address has to be an absolute http or https address.", nameof(BrowserRequest));
        }

        if (ViewportWidth is < 100 or > 10_000 || ViewportHeight is < 100 or > 10_000)
        {
            throw new ArgumentException("The viewport has to be between 100 and 10000 pixels each way.", nameof(BrowserRequest));
        }

        if (TimeoutSeconds is < 1 or > 600)
        {
            throw new ArgumentException("The timeout has to be between 1 and 600 seconds.", nameof(BrowserRequest));
        }
    }
}

/// <summary>The page after its scripts ran.</summary>
/// <param name="Html">The document as it is now.</param>
/// <param name="FinalUrl">Where the page ended up after redirects (<see langword="null"/> for HTML that was given).</param>
/// <param name="StatusCode">The status of the answer that was the page (<see langword="null"/> for HTML that was given).</param>
/// <param name="Title">The title of the page.</param>
public sealed record RenderedPage(string Html, Uri? FinalUrl, int? StatusCode, string Title);

/// <summary>The paper sizes of <see cref="PdfOptions"/>.</summary>
public enum PaperFormat
{
    A3,
    A4,
    A5,
    Letter,
    Legal,
}

/// <summary>How a page becomes a PDF.</summary>
public sealed record PdfOptions
{
    public PaperFormat Format { get; init; } = PaperFormat.A4;

    public bool Landscape { get; init; }

    /// <summary>Whether backgrounds (colours, images) are printed. Default: true.</summary>
    public bool PrintBackground { get; init; } = true;

    /// <summary>The same margin on every side, in millimetres. Default: 10.</summary>
    public int MarginMillimeters { get; init; } = 10;
}

/// <summary>The image formats of <see cref="ScreenshotOptions"/>.</summary>
public enum ImageFormat
{
    Png,
    Jpeg,
}

/// <summary>How a page becomes an image.</summary>
public sealed record ScreenshotOptions
{
    public ImageFormat Format { get; init; } = ImageFormat.Png;

    /// <summary>The quality of a JPEG, 1 to 100. Default: 80.</summary>
    public int Quality { get; init; } = 80;

    /// <summary>The whole page, not only what is in the viewport.</summary>
    public bool FullPage { get; init; }
}

/// <summary>The page could not be rendered (the address could not be loaded, the browser failed); the message says why.</summary>
public class BrowserException(string message, Exception? inner = null) : Exception(message, inner);

/// <summary>An address the policy does not allow, as the page to open.</summary>
public sealed class BrowserBlockedException(string message) : BrowserException(message);
