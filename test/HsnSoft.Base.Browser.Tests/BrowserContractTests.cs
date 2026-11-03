namespace HsnSoft.Base.Browser.Tests;

public class BrowserContractTests
{
    private static BrowserUrlPolicy Policy(Action<BrowserUrlPolicyOptions>? configure = null)
    {
        BrowserUrlPolicyOptions options = new();
        configure?.Invoke(options);
        return new BrowserUrlPolicy(options);
    }

    // ---------------------------------------------------------------- the policy

    [Theory]
    [InlineData("https://8.8.8.8/page", true)]
    [InlineData("http://8.8.8.8:8080/page", true)]
    [InlineData("https://[2606:4700:4700::1111]/", true)]
    [InlineData("about:blank", true)]
    [InlineData("data:text/html,<p>x</p>", true)]
    [InlineData("DATA:image/png;base64,AAAA", true)]
    [InlineData("http://127.0.0.1/", false)]
    [InlineData("http://127.0.0.1:9200/_search", false)]
    [InlineData("http://169.254.169.254/latest/meta-data", false)]
    [InlineData("http://10.0.0.5/admin", false)]
    [InlineData("http://192.168.1.1/", false)]
    [InlineData("http://[::1]/", false)]
    [InlineData("http://[fd00::1]/", false)]
    [InlineData("http://localhost/", false)]
    [InlineData("file:///etc/passwd", false)]
    [InlineData("ftp://8.8.8.8/x", false)]
    [InlineData("chrome://settings", false)]
    [InlineData("javascript:alert(1)", false)]
    [InlineData("not a url", false)]
    [InlineData("/relative", false)]
    [InlineData("http://this-host-does-not-exist.invalid/", false)]
    public async Task AnAddress_IsAllowed_OnlyWhenItLoadsNothingOrIsOfThePublicInternet(string url, bool expected)
        => Assert.Equal(expected, await Policy().AllowsAsync(url));

    [Fact]
    public async Task WithPrivateNetworksAllowed_TheInsideIsReachable_ButTheSchemesAreStillChecked()
    {
        BrowserUrlPolicy policy = Policy(options => options.AllowPrivateNetworks = true);

        Assert.True(await policy.AllowsAsync("http://127.0.0.1:5000/"));
        Assert.True(await policy.AllowsAsync("http://localhost:5000/"));
        Assert.False(await policy.AllowsAsync("file:///etc/passwd"));
    }

    [Fact]
    public async Task AllowedHosts_NarrowWhatIsLoaded()
    {
        BrowserUrlPolicy policy = Policy(options =>
        {
            options.AllowedHosts.Add("8.8.8.8");
            options.AllowedHosts.Add("*.example.com");
            options.AllowPrivateNetworks = true;
        });

        Assert.True(await policy.AllowsAsync("https://8.8.8.8/x"));
        Assert.True(await policy.AllowsAsync("https://cdn.example.com/x"));
        Assert.False(await policy.AllowsAsync("https://example.com/x"));
        Assert.False(await policy.AllowsAsync("https://1.1.1.1/x"));
        Assert.True(await policy.AllowsAsync("about:blank"));
    }

    [Fact]
    public async Task WhatALookupSaid_IsRememberedForTheNextRequest_AndTheCacheIsBounded()
    {
        BrowserUrlPolicy policy = Policy();

        Assert.False(await policy.AllowsAsync("http://10.1.1.1/a"));
        Assert.False(await policy.AllowsAsync("http://10.1.1.1/b"));
        for (int host = 0; host < 1100; host++)
        {
            Assert.True(await policy.AllowsAsync($"http://8.8.{host / 250}.{(host % 250) + 1}/"));
        }

        Assert.True(await policy.AllowsAsync("http://8.8.8.8/"));
        await Assert.ThrowsAsync<ArgumentNullException>(() => policy.AllowsAsync(null!));
    }

    // ---------------------------------------------------------------- the request

    [Fact]
    public void ARequest_HasAnAddressOrHtml_OneOfThem()
    {
        BrowserRequest.FromUrl(new Uri("https://example.com/a")).Validate();
        BrowserRequest.FromHtml("<p>x</p>").Validate();

        Assert.Throws<ArgumentException>(() => new BrowserRequest().Validate());
        Assert.Throws<ArgumentException>(() => (new BrowserRequest { Url = new Uri("https://example.com"), Html = "<p>x</p>" }).Validate());
        Assert.Throws<ArgumentException>(() => BrowserRequest.FromUrl(new Uri("ftp://example.com/a")).Validate());
        Assert.Throws<ArgumentException>(() => BrowserRequest.FromUrl(new Uri("/relative", UriKind.Relative)).Validate());
    }

    [Theory]
    [InlineData(99, 800, null)]
    [InlineData(1280, 10_001, null)]
    [InlineData(1280, 800, 0)]
    [InlineData(1280, 800, 601)]
    public void TheViewportAndTheTimeoutAreBounded(int width, int height, int? timeout)
        => Assert.Throws<ArgumentException>(() => (BrowserRequest.FromHtml("<p>x</p>") with { ViewportWidth = width, ViewportHeight = height, TimeoutSeconds = timeout }).Validate());

    [Fact]
    public void TheDefaults_AreWhatAPageNeeds()
    {
        BrowserRequest request = BrowserRequest.FromHtml("<p>x</p>");

        Assert.Equal(WaitCondition.Load, request.WaitUntil);
        Assert.Equal((1280, 800), (request.ViewportWidth, request.ViewportHeight));
        Assert.Equal((PaperFormat.A4, false, true, 10), (new PdfOptions().Format, new PdfOptions().Landscape, new PdfOptions().PrintBackground, new PdfOptions().MarginMillimeters));
        Assert.Equal((ImageFormat.Png, 80, false), (new ScreenshotOptions().Format, new ScreenshotOptions().Quality, new ScreenshotOptions().FullPage));
        Assert.IsAssignableFrom<BrowserException>(new BrowserBlockedException("x"));
    }
}
