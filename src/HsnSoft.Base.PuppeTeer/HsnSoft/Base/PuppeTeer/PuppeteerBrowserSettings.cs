namespace HsnSoft.Base.PuppeTeer;

public sealed class PuppeteerBrowserSettings
{
    public int PageMaxCount { get; set; } = 10;

    public bool Headless { get; set; } = true;

    public string[] Args { get; set; } = [];

    public int BrowserLaunchRetryCount { get; set; } = 3;

    public int BrowserLaunchRetryDelaySeconds { get; set; } = 2;

    public int WaitFreePageTimeoutMs { get; set; } = 30000;

    public int DefaultPageTimeoutMs { get; set; } = 30000;

    public int DefaultNavigationTimeoutMs { get; set; } = 60000;

    public int ShutdownDrainTimeoutSeconds { get; set; } = 30;
}