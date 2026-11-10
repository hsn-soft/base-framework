using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
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

/// <summary>The kinds of what a page loads that a request may leave unloaded (a page read for its text needs no video, font or style): the document itself is always loaded.</summary>
[Flags]
public enum BrowserResources
{
    None = 0,
    Media = 1,
    Fonts = 2,
    Stylesheets = 4,
    Scripts = 8,
    Images = 16,
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

    /// <summary>What the page does not load (<see cref="BrowserResources.None"/>: everything is loaded). The document is never left out, and a request the policy refuses stays refused.</summary>
    public BrowserResources BlockedResources { get; init; } = BrowserResources.None;

    /// <summary>The browser does not keep what it loaded between two pages (each page is fetched again).</summary>
    public bool DisableCache { get; init; }

    /// <summary>The page is told, before its own scripts run, that it is not driven by a robot (the properties a driven browser gives away are those of a person's browser): for a site that answers a robot differently. It is set when the page is navigated to, so it works for an address and not for HTML.</summary>
    public bool Stealth { get; init; }

    /// <summary>
    /// Functions that run in the page after it is ready, by name, in this order: each is the source of a JavaScript function that takes no argument (<c>() =&gt; Array.from(document.querySelectorAll('h1'), e =&gt; e.innerText)</c>) and returns what can be written as JSON;
    /// the result of each is in <see cref="RenderedPage.ScriptResults"/>. A function that fails does not fail the call: its result says why.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Scripts { get; init; }

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

        if (Scripts is not null && Scripts.Any(script => string.IsNullOrWhiteSpace(script.Key) || string.IsNullOrWhiteSpace(script.Value)))
        {
            throw new ArgumentException("A script has a name and a source.", nameof(BrowserRequest));
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
/// <param name="ScriptResults">The result of each function the request asked to run in the page, by name (none when it asked for none).</param>
public sealed record RenderedPage(string Html, Uri? FinalUrl, int? StatusCode, string Title, IReadOnlyDictionary<string, ScriptResult>? ScriptResults = null);

/// <summary>What a function run in a page returned (<see cref="Value"/>, as JSON) or why it failed (<see cref="Error"/>).</summary>
public sealed record ScriptResult(JsonElement? Value, string? Error)
{
    public bool IsSuccess => Error is null;
}

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
