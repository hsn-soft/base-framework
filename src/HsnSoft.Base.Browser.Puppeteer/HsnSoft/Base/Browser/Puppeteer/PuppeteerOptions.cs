using System;
using System.Collections.Generic;

namespace HsnSoft.Base.Browser.Puppeteer;

/// <summary>
/// The settings of the Chrome that renders (section <c>Browser</c>). Chrome is not downloaded by the application: <see cref="ExecutablePath"/> names the one the machine or the image has. The defaults are the safe ones (the sandbox of
/// Chrome on, certificates checked); a container that runs as root has to say so with <see cref="DisableSandbox"/>.
/// </summary>
public sealed class PuppeteerOptions
{
    public const string SectionName = "Browser";

    /// <summary>The Chrome (or Chromium) to start (<c>/usr/bin/chromium</c>). Required.</summary>
    public string ExecutablePath { get; set; } = string.Empty;

    /// <summary>
    /// Whether Chrome runs without its sandbox (<c>--no-sandbox</c>). Default: <see langword="false"/>. Chrome refuses to start as root with the sandbox, so a container that runs as root needs it on, and then **the page it renders can
    /// reach the machine**: run such a container as a user that is not root where you can, and never render pages you do not trust without a sandbox.
    /// </summary>
    public bool DisableSandbox { get; set; }

    /// <summary>Whether Chrome writes its shared memory to a temporary folder (<c>--disable-dev-shm-usage</c>): for a container whose <c>/dev/shm</c> is small (Docker gives 64 MB). Default: false.</summary>
    public bool DisableDevShmUsage { get; set; }

    /// <summary>Whether a page whose certificate is wrong is loaded anyway. Default: <see langword="false"/>; for a development machine only.</summary>
    public bool IgnoreCertificateErrors { get; set; }

    /// <summary>A proxy every request of the browser goes through (<c>http://proxy.internal:3128</c>).</summary>
    public string? ProxyServer { get; set; }

    /// <summary>The user name the proxy asks for (a secret: from the environment, never in a settings file). Used only with <see cref="ProxyServer"/>.</summary>
    public string? ProxyUserName { get; set; }

    /// <summary>The password the proxy asks for (a secret: from the environment, never in a settings file).</summary>
    public string? ProxyPassword { get; set; }

    /// <summary>More arguments for Chrome.</summary>
    public IList<string> ExtraArguments { get; } = [];

    /// <summary>How many pages are rendered at once. Default: 4. The others wait.</summary>
    public int MaxConcurrentPages { get; set; } = 4;

    /// <summary>How long a call waits for a place among the pages, in seconds; then it fails with a <see cref="TimeoutException"/>. Default: 30.</summary>
    public int WaitForPageSeconds { get; set; } = 30;

    /// <summary>The time one call may take in all (the page, its scripts, the result), in seconds, unless the request says another. Default: 60.</summary>
    public int OperationTimeoutSeconds { get; set; } = 60;

    /// <summary>The time the page may take to load, in seconds. Default: 30.</summary>
    public int NavigationTimeoutSeconds { get; set; } = 30;

    /// <summary>After how many pages the browser is replaced by a new one (Chrome grows with use). Default: 200; 0 for never.</summary>
    public int RecycleAfterPages { get; set; } = 200;

    /// <summary>How many times the start of Chrome is tried before the call fails. Default: 2.</summary>
    public int LaunchAttempts { get; set; } = 2;

    /// <summary>How long a stopping host waits for the pages that are being rendered, in seconds. Default: 20.</summary>
    public int ShutdownTimeoutSeconds { get; set; } = 20;

    /// <summary>The hosts a rendered page may load from: a name or <c>*.example.com</c>; empty for any public host.</summary>
    public IList<string> AllowedHosts { get; } = [];

    /// <summary>Whether a rendered page may load addresses of the inside of a network. Default: <see langword="false"/>; for a development machine only.</summary>
    public bool AllowPrivateNetworks { get; set; }

    internal void Validate()
    {
        if (string.IsNullOrWhiteSpace(ExecutablePath))
        {
            throw new InvalidOperationException("Browser:ExecutablePath is required: the Chrome to start.");
        }

        Range(MaxConcurrentPages, 1, 64, nameof(MaxConcurrentPages));
        Range(WaitForPageSeconds, 1, 600, nameof(WaitForPageSeconds));
        Range(OperationTimeoutSeconds, 1, 600, nameof(OperationTimeoutSeconds));
        Range(NavigationTimeoutSeconds, 1, 600, nameof(NavigationTimeoutSeconds));
        Range(RecycleAfterPages, 0, 1_000_000, nameof(RecycleAfterPages));
        Range(LaunchAttempts, 1, 10, nameof(LaunchAttempts));
        Range(ShutdownTimeoutSeconds, 0, 600, nameof(ShutdownTimeoutSeconds));
    }

    private static void Range(int value, int minimum, int maximum, string name)
    {
        if (value < minimum || value > maximum)
        {
            throw new InvalidOperationException($"Browser:{name} has to be between {minimum} and {maximum}.");
        }
    }
}
