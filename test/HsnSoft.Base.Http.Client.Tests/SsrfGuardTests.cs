using System.Net;
using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.Http.Client.Tests;

public class SsrfGuardTests
{
    [Theory]
    [InlineData("8.8.8.8", true)]
    [InlineData("93.184.216.34", true)]
    [InlineData("2606:4700:4700::1111", true)]
    [InlineData("127.0.0.1", false)]
    [InlineData("0.0.0.0", false)]
    [InlineData("10.1.2.3", false)]
    [InlineData("172.16.0.1", false)]
    [InlineData("172.31.255.255", false)]
    [InlineData("172.32.0.1", true)]
    [InlineData("192.168.1.1", false)]
    [InlineData("169.254.169.254", false)]
    [InlineData("100.64.0.1", false)]
    [InlineData("100.128.0.1", true)]
    [InlineData("198.18.0.1", false)]
    [InlineData("192.0.2.5", false)]
    [InlineData("224.0.0.1", false)]
    [InlineData("255.255.255.255", false)]
    [InlineData("::1", false)]
    [InlineData("::", false)]
    [InlineData("fe80::1", false)]
    [InlineData("fc00::1", false)]
    [InlineData("fd12:3456::1", false)]
    [InlineData("ff02::1", false)]
    [InlineData("::ffff:127.0.0.1", false)]
    [InlineData("::ffff:8.8.8.8", true)]
    public void AnAddress_IsPublic_OnlyWhenItIsOfTheInternet(string address, bool expected)
        => Assert.Equal(expected, SsrfGuard.IsPublic(IPAddress.Parse(address)));

    [Fact]
    public void TheAddressOfACall_IsChecked()
    {
        SsrfGuardOptions options = new();
        options.AllowedHosts.Add("files.example.com");
        options.AllowedHosts.Add("*.cdn.example.com");

        SsrfGuard.CheckAddress(new Uri("https://files.example.com/a"), options);
        SsrfGuard.CheckAddress(new Uri("http://eu.cdn.example.com:80/a"), options);
        Assert.Throws<SsrfBlockedException>(() => SsrfGuard.CheckAddress(new Uri("https://cdn.example.com/a"), options));
        Assert.Throws<SsrfBlockedException>(() => SsrfGuard.CheckAddress(new Uri("https://evil.com/a"), options));
        Assert.Throws<SsrfBlockedException>(() => SsrfGuard.CheckAddress(new Uri("https://files.example.com:8443/a"), options));
        Assert.Throws<SsrfBlockedException>(() => SsrfGuard.CheckAddress(new Uri("ftp://files.example.com/a"), options));
        Assert.Throws<SsrfBlockedException>(() => SsrfGuard.CheckAddress(new Uri("file:///etc/passwd"), options));
        Assert.Throws<SsrfBlockedException>(() => SsrfGuard.CheckAddress(new Uri("/relative", UriKind.Relative), options));
        Assert.Throws<SsrfBlockedException>(() => SsrfGuard.CheckAddress(null, options));
        Assert.Throws<ArgumentNullException>(() => SsrfGuard.CheckAddress(new Uri("https://a.com"), null!));
    }

    [Fact]
    public void AnIpAddressOfTheInside_IsRefusedBeforeAnyLookup_UnlessPrivateNetworksAreAllowed()
    {
        SsrfGuardOptions open = new();
        open.AllowedPorts.Clear();

        Assert.Throws<SsrfBlockedException>(() => SsrfGuard.CheckAddress(new Uri("http://169.254.169.254/latest/meta-data"), open));
        Assert.Throws<SsrfBlockedException>(() => SsrfGuard.CheckAddress(new Uri("http://[::1]:9000/"), open));
        Assert.Throws<SsrfBlockedException>(() => SsrfGuard.CheckAddress(new Uri("http://127.0.0.1:9000/"), open));
        SsrfGuard.CheckAddress(new Uri("http://8.8.8.8:9000/"), open);
        SsrfGuardOptions privateOk = new() { AllowPrivateNetworks = true };
        privateOk.AllowedPorts.Clear();
        SsrfGuard.CheckAddress(new Uri("http://127.0.0.1:9000/"), privateOk);
        Assert.Throws<SsrfBlockedException>(() => SsrfGuard.CheckAddress(new Uri("http://files.example.com/"), new SsrfGuardOptions { RequireHttps = true }));
        SsrfGuard.CheckAddress(new Uri("https://files.example.com/"), new SsrfGuardOptions { RequireHttps = true });
    }

    [Fact]
    public async Task TheHandler_RefusesBeforeTheCallIsSent()
    {
        using HttpClient client = new(new SsrfGuardHandler(new SsrfGuardOptions()) { InnerHandler = new HttpClientHandler() });

        await Assert.ThrowsAsync<SsrfBlockedException>(() => client.GetAsync("http://169.254.169.254/latest/meta-data"));
        await Assert.ThrowsAsync<SsrfBlockedException>(() => client.GetAsync("ftp://files.example.com/a"));
        await Assert.ThrowsAsync<ArgumentNullException>(() => new HttpMessageInvoker(new SsrfGuardHandler(new SsrfGuardOptions()) { InnerHandler = new HttpClientHandler() }).SendAsync(null!, CancellationToken.None));
    }

    private static (HttpListener Listener, int Port) Listen()
    {
        for (int port = 20_000 + Random.Shared.Next(20_000); ; port++)
        {
            HttpListener listener = new();
            listener.Prefixes.Add($"http://127.0.0.1:{port}/");
            try
            {
                listener.Start();
                return (listener, port);
            }
            catch (HttpListenerException)
            {
                listener.Close();
            }
        }
    }

    [Fact]
    public async Task AGuardedClient_ConnectsOnlyWhereTheGuardAllows_AndDoesNotFollowARedirect()
    {
        (HttpListener listener, int port) = Listen();
        using (listener)
        {
            Task serve = Task.Run(async () =>
            {
                for (int answered = 0; answered < 2; answered++)
                {
                    HttpListenerContext context = await listener.GetContextAsync();
                    if (context.Request.Url!.AbsolutePath == "/redirect")
                    {
                        context.Response.StatusCode = 302;
                        context.Response.RedirectLocation = "http://169.254.169.254/latest/meta-data";
                    }
                    else
                    {
                        context.Response.StatusCode = 200;
                    }

                    context.Response.Close();
                }
            });

            ServiceCollection refused = new();
            refused.AddHttpClient("g").AddSsrfGuard(options => options.AllowedPorts.Clear());
            ServiceCollection allowed = new();
            allowed.AddHttpClient("g").AddSsrfGuard(options =>
            {
                options.AllowedPorts.Clear();
                options.AllowPrivateNetworks = true;
            });

            using ServiceProvider refusedProvider = refused.BuildServiceProvider();
            using ServiceProvider allowedProvider = allowed.BuildServiceProvider();
            HttpClient blocked = refusedProvider.GetRequiredService<IHttpClientFactory>().CreateClient("g");
            HttpClient open = allowedProvider.GetRequiredService<IHttpClientFactory>().CreateClient("g");

            await Assert.ThrowsAsync<SsrfBlockedException>(() => blocked.GetAsync($"http://127.0.0.1:{port}/ok"));
            HttpResponseMessage ok = await open.GetAsync($"http://127.0.0.1:{port}/ok");
            HttpResponseMessage redirect = await open.GetAsync($"http://127.0.0.1:{port}/redirect");
            await serve;

            Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
            Assert.Equal(HttpStatusCode.Found, redirect.StatusCode);
        }
    }

    [Fact]
    public async Task ANameThatResolvesToTheInside_IsRefusedWhereTheConnectionIsMade()
    {
        ServiceCollection services = new();
        services.AddHttpClient("g").AddSsrfGuard(options => options.AllowedPorts.Clear());
        using ServiceProvider provider = services.BuildServiceProvider();
        HttpClient client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("g");

        HttpRequestException error = await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync("http://localhost:9/"));

        Assert.IsType<SsrfBlockedException>(error.InnerException ?? error);
        Assert.Throws<ArgumentNullException>(() => ((IHttpClientBuilder)null!).AddSsrfGuard());
    }

    [Fact]
    public async Task AnAllowedAddressThatCannotBeReached_IsAnHttpRequestException()
    {
        (HttpListener listener, int port) = Listen();
        listener.Close();   // nothing listens on the port now
        ServiceCollection services = new();
        services.AddHttpClient("g").AddSsrfGuard(options =>
        {
            options.AllowedPorts.Clear();
            options.AllowPrivateNetworks = true;
        });
        using ServiceProvider provider = services.BuildServiceProvider();
        HttpClient client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("g");

        HttpRequestException error = await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync($"http://127.0.0.1:{port}/"));

        Assert.NotNull(error);
    }
}
