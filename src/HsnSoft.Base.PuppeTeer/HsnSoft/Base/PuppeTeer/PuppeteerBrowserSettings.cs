namespace HsnSoft.Base.PuppeTeer;

public sealed class PuppeteerBrowserSettings
{
    public bool Headless { get; init; } = true;
    public bool LogProcess { get; init; } = false;
    public string[] Args { get; init; } = [];
    public int PageMaxCount { get; init; } = 20;
    public int PageDefaultTimeoutMs { get; init; } = 30 * 1000;
    public int PageDefaultNavigationTimeoutMs { get; init; } = 30 * 1000;
    public int WaitFreePageTimeoutMs { get; init; } = 10 * 60 * 1000;
    public bool CleanIdlePages { get; init; } = false;
    public int CleanUpIntervalMs { get; init; } = 60 * 1000;
    public int CleaningPageMaxIdleTimeMs { get; init; } = 30 * 60 * 1000;
}