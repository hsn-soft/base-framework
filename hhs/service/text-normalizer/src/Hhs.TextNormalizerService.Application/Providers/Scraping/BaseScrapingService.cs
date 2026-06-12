using System.Globalization;
using Hhs.TextNormalizerService.Application.Contracts.Providers.Dtos.Scraping;
using HsnSoft.Base.Logging.Abstracts;
using HsnSoft.Base.PuppeTeer;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PuppeteerSharp;

namespace Hhs.TextNormalizerService.Application.Providers.Scraping;

public abstract class BaseScrapingService(Uri targetUri, IAppConsoleLogger logger, IPuppeteerBrowser puppeTeerService)
{
    private readonly Uri _targetUri = targetUri ?? throw new ArgumentNullException(nameof(targetUri));
    protected IPuppeteerBrowser PuppeTeerService { get; } = puppeTeerService ?? throw new ArgumentNullException(nameof(puppeTeerService));
    protected IAppConsoleLogger Logger { get; } = logger ?? throw new ArgumentNullException(nameof(logger));

    public abstract Task<ScrapingResponseDto> RunAsync();

    protected virtual async Task<ScrapingResponseDto> RunScriptAsync(
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
        var returnModel = new ScrapingResponseDto();
        string tracePageId = Guid.CreateVersion7().ToString("N");

        EventHandler<RequestEventArgs>? handler;

        try
        {
            TryLogBrowserStats();

            await using var lease = await PuppeTeerService.AcquirePageAsync(
                async (page, ct) =>
                {
                    await ConfigureProxyAuthIfNeededAsync(page, tracePageId, ct).ConfigureAwait(false);

                    await page.SetUserAgentAsync(
                            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/123.0.6312.58 Safari/537.36")
                        .ConfigureAwait(false);

                    await page.SetExtraHttpHeadersAsync(new Dictionary<string, string> { ["Accept-Language"] = "tr-TR,tr;q=0.9" }).ConfigureAwait(false);

                    await page.SetCacheEnabledAsync(false).ConfigureAwait(false);

                    await InjectStealthAsync(page).ConfigureAwait(false);

                    await page.SetRequestInterceptionAsync(true).ConfigureAwait(false);

                    handler = async (_, args) =>
                    {
                        try
                        {
                            if (args.Request == null || page.IsClosed)
                                return;

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
                            {
                                await SafeAbortAsync(req);
                                return;
                            }

                            await SafeContinueAsync(req);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogDebug(
                                "{Service} | Page {PageId} request interception error: {Error}",
                                nameof(BaseScrapingService),
                                tracePageId,
                                ex.Message);
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
                new NavigationOptions { Timeout = 60000, WaitUntil = [WaitUntilNavigation.DOMContentLoaded] }).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            if (response is not { Ok: true })
            {
                var statusCode = response?.Status.ToString() ?? "NO_RESPONSE";
                var statusText = response?.StatusText ?? string.Empty;
                throw new InvalidOperationException($"{statusCode}:{statusText}");
            }

            await page.WaitForFunctionAsync(
                "() => document.readyState === 'interactive' || document.readyState === 'complete'",
                new WaitForFunctionOptions { Timeout = 30000 });

            // ---------------------
            // TITLE
            // ---------------------
            if (!string.IsNullOrWhiteSpace(titleScript))
            {
                try
                {
                    dynamic titleDynamic = await page.EvaluateFunctionAsync<dynamic>(titleScript).ConfigureAwait(false);
                    List<dynamic> titleList = JsonConvert.DeserializeObject<List<dynamic>>(titleDynamic.ToString()) ?? new List<dynamic>();
                    var titleStrList = titleList.Select(item => item.ToString()).ToList();
                    returnModel.Title = titleStrList.FirstOrDefault();

                    if (!string.IsNullOrWhiteSpace(returnModel.Title))
                        returnModel.Title = returnModel.Title.Replace("\"", "").Replace("\\", "");
                }
                catch (Exception ex)
                {
                    returnModel.Errors.Add("Title Error: " + ex.Message);
                }
            }

            // ---------------------
            // RELEASE TIME
            // ---------------------
            if (!string.IsNullOrWhiteSpace(releaseTimeScript))
            {
                try
                {
                    dynamic releaseTimeDynamic = await page.EvaluateFunctionAsync<dynamic>(releaseTimeScript).ConfigureAwait(false);

                    List<dynamic> releaseTimeList = JsonConvert.DeserializeObject<List<dynamic>>(releaseTimeDynamic.ToString()) ?? new List<dynamic>();

                    if (releaseTimeList.Count > 0)
                    {
                        foreach (dynamic rTime in releaseTimeList)
                        {
                            if (rTime is DateTime dt)
                            {
                                returnModel.ReleaseTime = dt.ToUniversalTime();
                                break;
                            }

                            var rTimeStr = rTime.ToString();
                            if (string.IsNullOrWhiteSpace(rTimeStr))
                                continue;

                            var host = _targetUri.Host.ToLower(new CultureInfo("en-US"));

                            if (host.Contains("techsummus.com"))
                            {
                                if (DateTime.TryParseExact(
                                        rTimeStr,
                                        "dd/MM/yyyy",
                                        new CultureInfo("tr-TR"),
                                        DateTimeStyles.None,
                                        out DateTime demoDate))
                                {
                                    if (demoDate is { Hour: 0, Minute: 0, Second: 0 })
                                        demoDate = demoDate.AddHours(6);

                                    returnModel.ReleaseTime = demoDate.ToUniversalTime();
                                    break;
                                }
                            }

                            if (host.Contains("diyetkolik.com"))
                            {
                                if (DateTime.TryParseExact(
                                        rTimeStr,
                                        "dd.MM.yyyy",
                                        new CultureInfo("tr-TR"),
                                        DateTimeStyles.None,
                                        out DateTime demoDate))
                                {
                                    if (demoDate is { Hour: 0, Minute: 0, Second: 0 })
                                        demoDate = demoDate.AddHours(6);

                                    returnModel.ReleaseTime = demoDate.ToUniversalTime();
                                    break;
                                }

                                continue;
                            }

                            if (DateTime.TryParse(rTimeStr, new CultureInfo("en-US"), DateTimeStyles.None, out DateTime tmpTime))
                            {
                                returnModel.ReleaseTime = tmpTime.ToUniversalTime();
                                break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    returnModel.Errors.Add("ReleaseTime Error: " + ex.Message);
                }
            }

            // ---------------------
            // SPOT
            // ---------------------
            if (!string.IsNullOrWhiteSpace(spotScript))
            {
                try
                {
                    dynamic spotDynamic = await page.EvaluateFunctionAsync<dynamic>(spotScript).ConfigureAwait(false);
                    List<dynamic> spotList = JsonConvert.DeserializeObject<List<dynamic>>(spotDynamic.ToString()) ?? new List<dynamic>();
                    var spotStrList = spotList.Select(item => item.ToString()).ToList();

                    var res = spotStrList.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
                    returnModel.Spot = string.Join(" ", res);

                    if (!string.IsNullOrWhiteSpace(returnModel.Spot))
                        returnModel.Spot = returnModel.Spot.Replace("\"", "").Replace("\\", "");
                }
                catch (Exception ex)
                {
                    returnModel.Errors.Add("Spot Error: " + ex.Message);
                }
            }

            // ---------------------
            // DETAILS
            // ---------------------
            if (!string.IsNullOrWhiteSpace(detailsScript))
            {
                try
                {
                    dynamic detailsDynamic = await page.EvaluateFunctionAsync<dynamic>(detailsScript).ConfigureAwait(false);

                    List<dynamic> detailList = JsonConvert.DeserializeObject<List<dynamic>>(detailsDynamic.ToString()) ?? new List<dynamic>();

                    var detailModelList =
                        detailList.Select(item => ((JObject)item).ToObject<ScrapingDetailModel>()).ToList();

                    var res = detailModelList
                        .Select(x =>
                            string.IsNullOrWhiteSpace(x?.TargetText)
                                ? string.Empty
                                : x.TargetText.Replace("\"", "").Replace("\\", ""))
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .ToArray();

                    returnModel.Details = string.Join(" ", res);
                }
                catch (Exception ex)
                {
                    returnModel.Errors.Add("Details Error: " + ex.Message);
                }
            }

            // ---------------------
            // IMAGE
            // ---------------------
            if (!string.IsNullOrWhiteSpace(imageScript))
            {
                try
                {
                    dynamic imageDynamic = await page.EvaluateFunctionAsync<dynamic>(imageScript).ConfigureAwait(false);

                    List<dynamic> imageList = JsonConvert.DeserializeObject<List<dynamic>>(imageDynamic.ToString()) ?? new List<dynamic>();
                    var imageStrList = imageList.Select(item => item.ToString()).ToList();
                    returnModel.ImageUrl = imageStrList.FirstOrDefault();

                    if (!string.IsNullOrWhiteSpace(returnModel.ImageUrl))
                        returnModel.ImageUrl = returnModel.ImageUrl.Replace("\"", "").Replace("\\", "");
                }
                catch (Exception ex)
                {
                    returnModel.Errors.Add("Image Error: " + ex.Message);
                }
            }

            if (!string.IsNullOrWhiteSpace(returnModel.Title) ||
                !string.IsNullOrWhiteSpace(returnModel.Spot) ||
                !string.IsNullOrWhiteSpace(returnModel.Details))
            {
                return returnModel;
            }

            returnModel.HasError = true;
            returnModel.Errors.Add("Scraping title, spot or details are empty");
            return returnModel;
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("{Service} | Page {PageId} cancelled", nameof(BaseScrapingService), tracePageId);
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"PuppeTeer Browser Page {tracePageId} navigation error: {ex.Message}", ex);
        }
        finally
        {
            // Lease dispose sırasında page otomatik kapanır
            Logger.LogDebug("{Service} | Page {PageId} closed", nameof(BaseScrapingService), tracePageId);
        }
    }

    private void TryLogBrowserStats()
    {
        try
        {
            Logger.LogDebug(
                "{Service} | Page => Active: {ActivePagesCount}, Max: {MaxPagesCount}",
                nameof(BaseScrapingService),
                PuppeTeerService.ActivePagesCount.ToString(),
                PuppeTeerService.MaxPagesCount.ToString());
        }
        catch
        {
            // ignore
        }
    }

    private async Task ConfigureProxyAuthIfNeededAsync(
        IPage page,
        string tracePageId,
        CancellationToken cancellationToken)
    {
        if (!PuppeTeerService.HasProxyServer)
            return;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var proxyUser = Environment.GetEnvironmentVariable("PUPPETEER_PROXY_USERNAME");
            var proxyPass = Environment.GetEnvironmentVariable("PUPPETEER_PROXY_PASSWORD");

            if (!string.IsNullOrWhiteSpace(proxyUser) && !string.IsNullOrWhiteSpace(proxyPass))
            {
                await page.AuthenticateAsync(new Credentials { Username = proxyUser, Password = proxyPass }).ConfigureAwait(false);

                Logger.LogDebug(
                    "{Service} | Page {PageId} Authentication Success : {AuthUser}",
                    nameof(BaseScrapingService),
                    tracePageId,
                    proxyUser);
            }
            else
            {
                Logger.LogWarning(
                    "{Service} | Page {PageId} proxy credentials are empty",
                    nameof(BaseScrapingService),
                    tracePageId);
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(
                "{Service} | Page {PageId} Authentication Failed {AuthError}",
                nameof(BaseScrapingService),
                tracePageId,
                ex.Message);
        }
    }

    private static async Task InjectStealthAsync(IPage page)
    {
        await page.EvaluateFunctionOnNewDocumentAsync("""
                                                          () => {
                                                              Object.defineProperty(navigator, 'webdriver', { get: () => false });

                                                              Object.defineProperty(navigator, 'languages', {
                                                                  get: () => ['tr-TR', 'tr']
                                                              });

                                                              window.chrome = { runtime: {} };

                                                              const originalQuery = window.navigator.permissions.query;
                                                              window.navigator.permissions.query = (parameters) =>
                                                                  parameters.name === 'notifications'
                                                                      ? Promise.resolve({ state: Notification.permission })
                                                                      : originalQuery(parameters);

                                                              Object.defineProperty(navigator, 'plugins', {
                                                                  get: () => [1, 2, 3, 4]
                                                              });

                                                              Object.defineProperty(navigator, 'mimeTypes', {
                                                                  get: () => [1, 2, 3]
                                                              });

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
    {
        if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            return url.Replace("http://", "https://", StringComparison.OrdinalIgnoreCase);
        }

        return url;
    }

    private static async Task SafeContinueAsync(IRequest request)
    {
        try
        {
            await request.ContinueAsync().ConfigureAwait(false);
        }
        catch
        {
            // ignore
        }
    }

    private static async Task SafeAbortAsync(IRequest request)
    {
        try
        {
            await request.AbortAsync().ConfigureAwait(false);
        }
        catch
        {
            // ignore
        }
    }
}