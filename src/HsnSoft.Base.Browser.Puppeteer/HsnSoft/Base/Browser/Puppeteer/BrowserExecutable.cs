using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PuppeteerSharp;

namespace HsnSoft.Base.Browser.Puppeteer;

/// <summary>
/// Decides which Chrome starts: the one of <see cref="PuppeteerOptions.ExecutablePath"/> when it is there, else (only outside a container, and only when <see cref="PuppeteerOptions.DownloadWhenMissing"/> is on) one that is
/// downloaded once into a folder of the machine. A container never downloads: its image has the Chrome.
/// </summary>
internal sealed partial class BrowserExecutable(PuppeteerOptions options, ILogger logger, Func<string, string?>? environment = null, Func<string, bool>? fileExists = null, Func<string, CancellationToken, Task<string>>? download = null) : IDisposable
{
    private readonly Func<string, string?> _environment = environment ?? Environment.GetEnvironmentVariable;
    private readonly Func<string, bool> _fileExists = fileExists ?? File.Exists;
    private readonly Func<string, CancellationToken, Task<string>> _download = download ?? DownloadChromeAsync;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private string? _resolved;

    /// <summary>Whether the process runs in a container (the variable of the .NET images, the one of Kubernetes, an explicit <c>IS_IN_DOCKER_CONTAINER</c>, or the marker file of Docker).</summary>
    public bool InContainer
        => IsTrue(_environment("DOTNET_RUNNING_IN_CONTAINER"))
           || IsTrue(_environment("IS_IN_DOCKER_CONTAINER"))
           || !string.IsNullOrEmpty(_environment("KUBERNETES_SERVICE_HOST"))
           || _fileExists("/.dockerenv");

    /// <summary>Whether a Chrome that is not there would be downloaded.</summary>
    public bool WouldDownload => options.DownloadWhenMissing && !InContainer;

    /// <summary>The folder of the downloads: <c>Browser:DownloadPath</c>, else a folder of the machine for all the applications.</summary>
    public string DownloadFolder
        => string.IsNullOrWhiteSpace(options.DownloadPath)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HsnSoft", "browser")
            : options.DownloadPath;

    public async Task<string> ResolveAsync(CancellationToken cancellationToken)
    {
        if (_resolved is not null)
        {
            return _resolved;
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_resolved is not null)
            {
                return _resolved;
            }

            if (!string.IsNullOrWhiteSpace(options.ExecutablePath) && _fileExists(options.ExecutablePath))
            {
                return _resolved = options.ExecutablePath;
            }

            if (WouldDownload)
            {
                LogDownloading(logger, DownloadFolder);
                string path = await _download(DownloadFolder, cancellationToken).ConfigureAwait(false);
                LogDownloaded(logger, path);
                return _resolved = path;
            }

            if (string.IsNullOrWhiteSpace(options.ExecutablePath))
            {
                throw new InvalidOperationException("Browser:ExecutablePath is required here: " + (options.DownloadWhenMissing ? "the process runs in a container, which never downloads a Chrome: put one in the image." : "the Chrome to start."));
            }

            // Not there and not to be downloaded: let the start fail with the path in the message.
            return options.ExecutablePath;
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose() => _gate.Dispose();

    private static bool IsTrue(string? value) => string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) || value == "1";

    /// <summary>The one line that reaches the network (the stable Chrome of PuppeteerSharp's fetcher); everything around it is tested with a download that is a function.</summary>
    private static async Task<string> DownloadChromeAsync(string folder, CancellationToken cancellationToken)
        => (await new BrowserFetcher(new BrowserFetcherOptions { Browser = SupportedBrowser.Chrome, Path = folder }).DownloadAsync().WaitAsync(cancellationToken).ConfigureAwait(false)).GetExecutablePath();

    [LoggerMessage(Level = LogLevel.Information, Message = "Chrome is not there: it is downloaded once into {Folder} (Browser:DownloadWhenMissing, outside a container).")]
    private static partial void LogDownloading(ILogger logger, string folder);

    [LoggerMessage(Level = LogLevel.Information, Message = "Chrome is ready at {Path}.")]
    private static partial void LogDownloaded(ILogger logger, string path);
}
