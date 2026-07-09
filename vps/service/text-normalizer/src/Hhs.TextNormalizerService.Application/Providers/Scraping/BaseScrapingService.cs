using System.Globalization;
using System.Text.Json.Serialization;
using Hhs.Shared.Helper.Retry;
using HsnSoft.Base.Logging.Abstracts;
using HsnSoft.Base.PuppeTeer;
using PuppeteerSharp;

namespace Hhs.TextNormalizerService.Application.Providers.Scraping;

public abstract class BaseScrapingService(Uri targetUri, IAppConsoleLogger logger, IPuppeteerBrowser puppeteerService)
{
    private readonly Uri _targetUri = targetUri ?? throw new ArgumentNullException(nameof(targetUri));
    protected IPuppeteerBrowser PuppeteerService { get; } = puppeteerService ?? throw new ArgumentNullException(nameof(puppeteerService));
    protected IAppConsoleLogger Logger { get; } = logger ?? throw new ArgumentNullException(nameof(logger));

    public abstract Task<ScraperResultDto> RunAsync();

    protected virtual async Task<ScraperResultDto> RunScriptAsync(
        string titleScript,
        string releaseTimeScript,
        string spotScript,
        string detailsScript,
        string imageScript,
        bool isEnabledFontBlocked = false,
        bool isEnabledCssBlocked = false,
        bool isEnabledScriptBlocked = false,
        CancellationToken cancellationToken = default)
    {
        var result = new ScraperResultDto();
        string tracePageId = Guid.CreateVersion7().ToString("N");

        EventHandler<RequestEventArgs>? handler = null;

        try
        {
            TryLogBrowserStats();

            await using var lease = await PuppeteerService.AcquirePageAsync(
                async (page, ct) =>
                {
                    await ConfigureProxyAuthIfNeededAsync(page, tracePageId, ct).ConfigureAwait(false);

                    await page.SetUserAgentAsync(
                            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/123.0.6312.58 Safari/537.36")
                        .ConfigureAwait(false);

                    await page.SetExtraHttpHeadersAsync(new Dictionary<string, string> { ["Accept-Language"] = "tr-TR,tr;q=0.9" })
                        .ConfigureAwait(false);

                    await page.SetCacheEnabledAsync(false).ConfigureAwait(false);

                    await InjectStealthAsync(page).ConfigureAwait(false);

                    await page.SetRequestInterceptionAsync(true).ConfigureAwait(false);

                    handler = async (_, args) =>
                    {
                        try
                        {
                            if (args.Request == null || page.IsClosed) return;

                            var req = args.Request;
                            var resourceType = req.ResourceType;

                            if (req.IsNavigationRequest && resourceType == ResourceType.Document)
                            {
                                await SafeContinueAsync(req);
                                return;
                            }

                            bool shouldBlock =
                                resourceType == ResourceType.Media ||
                                (isEnabledFontBlocked && resourceType == ResourceType.Font) ||
                                (isEnabledCssBlocked && resourceType == ResourceType.StyleSheet) ||
                                (isEnabledScriptBlocked && resourceType == ResourceType.Script);

                            if (shouldBlock)
                                await SafeAbortAsync(req);
                            else
                                await SafeContinueAsync(req);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogDebug("{Service} | Page {PageId} request interception error: {Error}",
                                nameof(BaseScrapingService), tracePageId, ex.Message);
                        }
                    };

                    page.Request += handler;
                },
                cancellationToken).ConfigureAwait(false);

            var page = lease.Page;

            Logger.LogDebug("{Service} | Page {PageId} opened", nameof(BaseScrapingService), tracePageId);

            string checkedUrl = NormalizeUrl(_targetUri.AbsoluteUri);

            var response = await page.GoToAsync(
                checkedUrl,
                new NavigationOptions { Timeout = 60000, WaitUntil = [WaitUntilNavigation.DOMContentLoaded] }
            ).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            if (response is not { Ok: true })
            {
                string statusCode = response?.Status.ToString() ?? "NO_RESPONSE";
                string statusText = response?.StatusText ?? string.Empty;
                // No response at all (network/DNS-level failure) is treated as transient; a real
                // non-OK HTTP status is classified by its code (5xx/429 transient, other 4xx permanent).
                bool isRetryable = response is null || ExceptionClassifier.IsRetryable(response.Status);

                return new ScraperResultDto
                {
                    HasError = true,
                    IsRetryable = isRetryable,
                    Errors = { $"{statusCode}:{statusText}" }
                };
            }

            await page.WaitForFunctionAsync(
                "() => document.readyState === 'interactive' || document.readyState === 'complete'",
                new WaitForFunctionOptions { Timeout = 30000 }).ConfigureAwait(false);

            // TITLE
            if (!string.IsNullOrWhiteSpace(titleScript))
            {
                try
                {
                    string[] items = await page.EvaluateFunctionAsync<string[]>(titleScript).ConfigureAwait(false);
                    result.Title = items?.FirstOrDefault(s => !string.IsNullOrWhiteSpace(s))
                        ?.Replace("\"", "").Replace("\\", "");
                }
                catch (Exception ex) { result.Errors.Add("Title Error: " + ex.Message); }
            }

            // RELEASE TIME
            if (!string.IsNullOrWhiteSpace(releaseTimeScript))
            {
                try
                {
                    string[] items = await page.EvaluateFunctionAsync<string[]>(releaseTimeScript).ConfigureAwait(false);
                    if (items is { Length: > 0 })
                    {
                        string host = _targetUri.Host.ToLower(new CultureInfo("en-US"));
                        foreach (string rTimeStr in items.Where(s => !string.IsNullOrWhiteSpace(s)))
                        {
                            if (host.Contains("techsummus.com") &&
                                DateTime.TryParseExact(rTimeStr, "dd/MM/yyyy", new CultureInfo("tr-TR"), DateTimeStyles.None, out var techDate))
                            {
                                if (techDate is { Hour: 0, Minute: 0, Second: 0 }) techDate = techDate.AddHours(6);
                                result.ReleaseTimeUtc = techDate.ToUniversalTime();
                                break;
                            }

                            if (host.Contains("diyetkolik.com") &&
                                DateTime.TryParseExact(rTimeStr, "dd.MM.yyyy", new CultureInfo("tr-TR"), DateTimeStyles.None, out var dietDate))
                            {
                                if (dietDate is { Hour: 0, Minute: 0, Second: 0 }) dietDate = dietDate.AddHours(6);
                                result.ReleaseTimeUtc = dietDate.ToUniversalTime();
                                break;
                            }

                            if (DateTime.TryParse(rTimeStr, new CultureInfo("en-US"), DateTimeStyles.None, out var parsed))
                            {
                                result.ReleaseTimeUtc = parsed.ToUniversalTime();
                                break;
                            }
                        }
                    }
                }
                catch (Exception ex) { result.Errors.Add("ReleaseTime Error: " + ex.Message); }
            }

            // SPOT
            if (!string.IsNullOrWhiteSpace(spotScript))
            {
                try
                {
                    string[] items = await page.EvaluateFunctionAsync<string[]>(spotScript).ConfigureAwait(false);
                    result.Spot = string.Join(" ", (items ?? []).Where(s => !string.IsNullOrWhiteSpace(s)))
                        .Replace("\"", "").Replace("\\", "");
                    if (string.IsNullOrWhiteSpace(result.Spot)) result.Spot = null;
                }
                catch (Exception ex) { result.Errors.Add("Spot Error: " + ex.Message); }
            }

            // DETAILS
            if (!string.IsNullOrWhiteSpace(detailsScript))
            {
                try
                {
                    var items = await page.EvaluateFunctionAsync<ScrapingDetailModel[]>(detailsScript).ConfigureAwait(false);
                    result.Details = string.Join(" ", (items ?? [])
                        .Where(x => !string.IsNullOrWhiteSpace(x?.TargetText))
                        .Select(x => x!.TargetText!.Replace("\"", "").Replace("\\", ""))
                        .Where(s => !string.IsNullOrWhiteSpace(s)));
                    if (string.IsNullOrWhiteSpace(result.Details)) result.Details = null;
                }
                catch (Exception ex) { result.Errors.Add("Details Error: " + ex.Message); }
            }

            // IMAGE
            if (!string.IsNullOrWhiteSpace(imageScript))
            {
                try
                {
                    string[] items = await page.EvaluateFunctionAsync<string[]>(imageScript).ConfigureAwait(false);
                    result.ImageUrl = items?.FirstOrDefault(s => !string.IsNullOrWhiteSpace(s))
                        ?.Replace("\"", "").Replace("\\", "");
                }
                catch (Exception ex) { result.Errors.Add("Image Error: " + ex.Message); }
            }

            if (!string.IsNullOrWhiteSpace(result.Title) ||
                !string.IsNullOrWhiteSpace(result.Spot) ||
                !string.IsNullOrWhiteSpace(result.Details))
            {
                return result;
            }

            result.HasError = true;
            result.IsRetryable = false;
            result.Errors.Add("Scraping title, spot or details are empty");
            return result;
        }
        catch (OperationCanceledException ex)
        {
            Logger.LogWarning("{Service} | Page {PageId} cancelled", nameof(BaseScrapingService), tracePageId);
            return new ScraperResultDto { HasError = true, IsRetryable = true, Errors = { ex.Message } };
        }
        catch (Exception ex) when (ex is PuppeteerException or PuppeteerSharp.ProcessException)
        {
            // Puppeteer's own exception hierarchy (navigation failures, target closed/crashed,
            // wait-task timeouts, browser process issues) is transient/infra-level by nature.
            return new ScraperResultDto
            {
                HasError = true,
                IsRetryable = true,
                Errors = { $"PuppeteerBrowser Page {tracePageId} error: {ex.Message}" }
            };
        }
        catch (Exception ex)
        {
            return new ScraperResultDto
            {
                HasError = true,
                IsRetryable = ExceptionClassifier.IsRetryable(ex),
                Errors = { $"PuppeteerBrowser Page {tracePageId} navigation error: {ex.Message}" }
            };
        }
        finally
        {
            Logger.LogDebug("{Service} | Page {PageId} closed", nameof(BaseScrapingService), tracePageId);
        }
    }

    private void TryLogBrowserStats()
    {
        try
        {
            Logger.LogDebug("{Service} | Page => Active: {ActivePagesCount}, Max: {MaxPagesCount}",
                nameof(BaseScrapingService),
                PuppeteerService.ActivePagesCount.ToString(),
                PuppeteerService.MaxPagesCount.ToString());
        }
        catch { /* ignore */ }
    }

    private async Task ConfigureProxyAuthIfNeededAsync(IPage page, string tracePageId, CancellationToken cancellationToken)
    {
        if (!PuppeteerService.HasProxyServer) return;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            string proxyUser = Environment.GetEnvironmentVariable("PUPPETEER_PROXY_USERNAME");
            string proxyPass = Environment.GetEnvironmentVariable("PUPPETEER_PROXY_PASSWORD");

            if (!string.IsNullOrWhiteSpace(proxyUser) && !string.IsNullOrWhiteSpace(proxyPass))
            {
                await page.AuthenticateAsync(new Credentials { Username = proxyUser, Password = proxyPass }).ConfigureAwait(false);
                Logger.LogDebug("{Service} | Page {PageId} proxy auth ok: {AuthUser}",
                    nameof(BaseScrapingService), tracePageId, proxyUser);
            }
            else
            {
                Logger.LogWarning("{Service} | Page {PageId} proxy credentials are empty",
                    nameof(BaseScrapingService), tracePageId);
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning("{Service} | Page {PageId} proxy auth failed: {Error}",
                nameof(BaseScrapingService), tracePageId, ex.Message);
        }
    }

    private static async Task InjectStealthAsync(IPage page)
    {
        await page.EvaluateFunctionOnNewDocumentAsync("""
            () => {
                Object.defineProperty(navigator, 'webdriver', { get: () => false });
                Object.defineProperty(navigator, 'languages', { get: () => ['tr-TR', 'tr'] });
                window.chrome = { runtime: {} };
                const originalQuery = window.navigator.permissions.query;
                window.navigator.permissions.query = (parameters) =>
                    parameters.name === 'notifications'
                        ? Promise.resolve({ state: Notification.permission })
                        : originalQuery(parameters);
                Object.defineProperty(navigator, 'plugins', { get: () => [1, 2, 3, 4] });
                Object.defineProperty(navigator, 'mimeTypes', { get: () => [1, 2, 3] });
                Object.defineProperty(navigator, 'hardwareConcurrency', { get: () => 8 });
                Object.defineProperty(navigator, 'deviceMemory', { get: () => 8 });
                Object.defineProperty(navigator, 'platform', { get: () => 'Win32' });
                Object.defineProperty(navigator, 'maxTouchPoints', { get: () => 0 });
                const getParameter = WebGLRenderingContext.prototype.getParameter;
                WebGLRenderingContext.prototype.getParameter = function(parameter) {
                    if (parameter === 37445) return 'Intel Inc.';
                    if (parameter === 37446) return 'Intel(R) HD Graphics 630';
                    return getParameter.call(this, parameter);
                };
            }
        """).ConfigureAwait(false);
    }

    private static string NormalizeUrl(string url)
        => url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            ? url.Replace("http://", "https://", StringComparison.OrdinalIgnoreCase)
            : url;

    private static async Task SafeContinueAsync(IRequest request)
    {
        try { await request.ContinueAsync().ConfigureAwait(false); }
        catch { /* ignore */ }
    }

    private static async Task SafeAbortAsync(IRequest request)
    {
        try { await request.AbortAsync().ConfigureAwait(false); }
        catch { /* ignore */ }
    }
}

internal sealed class ScrapingDetailModel
{
    [JsonPropertyName("targetText")]
    public string? TargetText { get; set; }

    [JsonPropertyName("filterValue")]
    public string? FilterValue { get; set; }
}
