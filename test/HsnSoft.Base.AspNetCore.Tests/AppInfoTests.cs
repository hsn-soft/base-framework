using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace HsnSoft.Base.AspNetCore.Tests;

public class AppInfoTests
{
    private sealed class Environment(string name, string application) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;

        public string ApplicationName { get; set; } = application;

        public string ContentRootPath { get; set; } = "";

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private static Func<string, string?> Variables(Dictionary<string, string?> values) => key => values.GetValueOrDefault(key);

    [Fact]
    public void TheId_IsTheFirstOfAppIdPodNameAndHostname_ElseANewOne()
    {
        var environment = new Environment("uat", "Abc.OrderService");

        Assert.Equal("a", AppInfo.Create(environment, null, Variables(new() { ["APP_ID"] = "a", ["POD_NAME"] = "p", ["HOSTNAME"] = "h" })).Id);
        Assert.Equal("p", AppInfo.Create(environment, null, Variables(new() { ["APP_ID"] = " ", ["POD_NAME"] = "p", ["HOSTNAME"] = "h" })).Id);
        Assert.Equal("h", AppInfo.Create(environment, null, Variables(new() { ["HOSTNAME"] = "h" })).Id);
        string generated = AppInfo.Create(environment, null, Variables([])).Id;
        Assert.Equal(32, generated.Length);
        Assert.NotEqual(generated, AppInfo.Create(environment, null, Variables([])).Id);
    }

    [Fact]
    public void TheName_IsTheSettingOrTheNameOfTheApplication_AndTheEnvironmentIsKept()
    {
        var environment = new Environment("production", "Abc.OrderService");

        AppInfo named = AppInfo.Create(environment, "order", Variables([]));
        AppInfo unnamed = AppInfo.Create(environment, null, Variables([]));

        Assert.Equal("order", named.Name);
        Assert.Equal("Abc.OrderService", unnamed.Name);
        Assert.Equal("production", unnamed.Environment);
    }

    [Fact]
    public void TheVersion_IsTheBuildNumber_ElseTheVersionOfTheAssembly()
    {
        var environment = new Environment("uat", "x");

        Assert.Equal("2026.10.41", AppInfo.Create(environment, null, Variables(new() { ["BUILD_NUMBER"] = "2026.10.41" })).Version);
        Assert.False(string.IsNullOrWhiteSpace(AppInfo.Create(environment, null, Variables([])).Version));
    }

    [Fact]
    public void WithTheProcessVariables_TheInformationIsMadeToo()
    {
        AppInfo info = AppInfo.Create(new Environment("uat", "x"));

        Assert.False(string.IsNullOrWhiteSpace(info.Id));
        Assert.True(info.StartedAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task TheRoot_AnswersJson_ToAnyoneAndHasTheInformation()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["Service:Name"] = "order" });
        builder.AddBaseHosting();
        await using WebApplication app = builder.Build();
        app.MapBaseInfo();
        await app.StartAsync();

        HttpResponseMessage response = await app.GetTestClient().GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("order", json.RootElement.GetProperty("name").GetString());
        Assert.False(string.IsNullOrWhiteSpace(json.RootElement.GetProperty("id").GetString()));
        Assert.Equal(app.Environment.EnvironmentName, json.RootElement.GetProperty("environment").GetString());
        Assert.True(json.RootElement.TryGetProperty("version", out _));
        Assert.True(json.RootElement.TryGetProperty("startedAt", out _));
    }

    [Fact]
    public void TheThreadPool_IsOnlyRaised()
    {
        ThreadPool.GetMinThreads(out int worker, out int completion);

        Assert.Equal((worker, completion), BaseHostingExtensions.ApplyThreadPool(new BaseThreadPoolOptions()));
        Assert.Equal((worker, completion), BaseHostingExtensions.ApplyThreadPool(new BaseThreadPoolOptions { MinWorkerThreads = 1, MinCompletionPortThreads = 1 }));
        Assert.Equal((worker + 2, completion + 1), BaseHostingExtensions.ApplyThreadPool(new BaseThreadPoolOptions { MinWorkerThreads = worker + 2, MinCompletionPortThreads = completion + 1 }));
        ThreadPool.GetMinThreads(out int afterWorker, out int afterCompletion);
        Assert.Equal((worker + 2, completion + 1), (afterWorker, afterCompletion));
    }

    [Fact]
    public void NullArguments_AreRejected()
    {
        Assert.Throws<ArgumentNullException>(() => AppInfo.Create(null!));
        Assert.Throws<ArgumentNullException>(() => BaseHostingExtensions.MapBaseInfo(null!));
        Assert.Throws<ArgumentNullException>(() => BaseHostingExtensions.ApplyThreadPool(null!));
    }
}
