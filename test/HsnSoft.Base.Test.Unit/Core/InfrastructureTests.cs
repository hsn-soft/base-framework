using System.Reflection;
using FluentAssertions;
using HsnSoft.Base.ExceptionHandling;
using HsnSoft.Base.Localization;
using HsnSoft.Base.Logging;
using HsnSoft.Base.Logging.Models;
using HsnSoft.Base.Options;
using HsnSoft.Base.Reflection;
using HsnSoft.Base.Threading;
using HsnSoft.Base.Tracing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace HsnSoft.Base.Test.Unit.Core;

public class InfrastructureTests
{
    // ---------- Exception handling ----------

    private class RecordingSubscriber : ExceptionSubscriber
    {
        public List<ExceptionNotificationContext> Received { get; } = new();

        public override Task HandleAsync(ExceptionNotificationContext context)
        {
            Received.Add(context);
            return Task.CompletedTask;
        }
    }

    private class ThrowingSubscriber : IExceptionSubscriber
    {
        public Task HandleAsync(ExceptionNotificationContext context) => throw new InvalidOperationException("boom");
    }

    [Fact]
    public void ExceptionNotificationContext_Works()
    {
        var ex = new InvalidOperationException();
        var ctx = new ExceptionNotificationContext(ex);
        ctx.Exception.Should().BeSameAs(ex);
        ctx.LogLevel.Should().Be(LogLevel.Error);
        ctx.Handled.Should().BeTrue();

        var ctx2 = new ExceptionNotificationContext(ex, LogLevel.Critical, false);
        ctx2.LogLevel.Should().Be(LogLevel.Critical);
        ctx2.Handled.Should().BeFalse();

        new ExceptionNotificationContext(new BusinessException(logLevel: LogLevel.Information)).LogLevel.Should().Be(LogLevel.Information);
        FluentActions.Invoking(() => new ExceptionNotificationContext(null)).Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task ExceptionNotifier_InvokesSubscribers_AndSwallowsFailures()
    {
        var good = new RecordingSubscriber();
        var services = new ServiceCollection();
        services.AddSingleton<IExceptionSubscriber>(new ThrowingSubscriber());
        services.AddSingleton<IExceptionSubscriber>(good);
        var provider = services.BuildServiceProvider();

        var notifier = new ExceptionNotifier(provider.GetRequiredService<IServiceScopeFactory>());
        notifier.Logger.Should().NotBeNull();
        await notifier.NotifyAsync(new Exception("x"), LogLevel.Warning, false);
        good.Received.Should().ContainSingle();
        good.Received[0].LogLevel.Should().Be(LogLevel.Warning);
        good.Received[0].Handled.Should().BeFalse();

        await FluentActions.Awaiting(() => notifier.NotifyAsync(null)).Should().ThrowAsync<ArgumentNullException>();
        FluentActions.Invoking(() => { _ = ExceptionNotifierExtensions.NotifyAsync(null, new Exception()); }).Should().Throw<ArgumentNullException>();

        await NullExceptionNotifier.Instance.NotifyAsync(new ExceptionNotificationContext(new Exception()));
        NullExceptionNotifier.Instance.Should().BeSameAs(NullExceptionNotifier.Instance);
    }

    [Fact]
    public void LocalizationContext_ResolvesFactory()
    {
        var factory = new Mock<IStringLocalizerFactory>().Object;
        var provider = new ServiceCollection().AddSingleton(factory).BuildServiceProvider();
        var ctx = new LocalizationContext(provider);
        ctx.LocalizerFactory.Should().BeSameAs(factory);
        ctx.ServiceProvider.Should().BeSameAs(provider);
    }

    // ---------- Logging ----------

    private class TestLogger : DefaultBaseLogger
    {
        public TestLogger(IConfiguration configuration) : base(configuration) { }
        public ILogger Inner => Logger;
    }

    [Fact]
    public void DefaultBaseLogger_LogsWithAndWithoutConfiguredLevel()
    {
        var withLevel = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string> { ["Logging:LogLevel:Default"] = "Debug" })
            .Build();
        var l1 = new TestLogger(withLevel);
        l1.Inner.IsEnabled(LogLevel.Debug).Should().BeTrue();
        l1.LogDebug("d {0}", 1);
        l1.LogInformation("i {0}", 1);
        l1.LogWarning("w {0}", 1);
        l1.LogError("e {0}", 1);
        l1.LogError(new Exception("x"), "e {0}", 1);

        var noLevel = new ConfigurationBuilder().Build();
        var l2 = new TestLogger(noLevel);
        l2.Inner.IsEnabled(LogLevel.Debug).Should().BeFalse();
        l2.Inner.IsEnabled(LogLevel.Information).Should().BeTrue();
    }

    private static Exception Thrown(Exception e)
    {
        try { throw e; }
        catch (Exception caught) { return caught; }
    }

    [Fact]
    public void LogHelper_Generate_Works()
    {
        var plain = LogHelper.Generate("m", new { a = 1 }, "fac", "cid");
        plain.Message.Should().Be("m");
        plain.Facility.Should().Be("fac");
        plain.CorrelationId.Should().Be("cid");
        plain.Reference.Should().NotBeNull();
        plain.StackTrace.Should().BeNull();
        plain.LogId.Should().NotBeNullOrEmpty();
        plain.CreatedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));

        var withEx = LogHelper.Generate("m", exception: Thrown(new Exception("outer", new Exception("inner"))));
        withEx.StackTrace.ErrorDetail.Should().Be("outer inner");
        withEx.StackTrace.StackMethodName.Should().NotBeNull();
        withEx.StackTrace.StackLineNumber.Should().BeGreaterThanOrEqualTo(0);
        withEx.StackTrace.StackFileName.Should().NotBeNull();

        var noInner = LogHelper.Generate("m", exception: new Exception("solo"));
        noInner.StackTrace.ErrorDetail.Should().Be("solo");
        noInner.StackTrace.StackMethodName.Should().BeNull();
        noInner.StackTrace.StackFileName.Should().BeNull();
        noInner.StackTrace.StackLineNumber.Should().Be(0);
    }

    [Fact]
    public void LogModels_HoldValues()
    {
        var d = new StackTraceLogDetail { ErrorDetail = "e", StackFileName = "f", StackMethodName = "m", StackLineNumber = 3 };
        d.ErrorDetail.Should().Be("e");
        d.StackFileName.Should().Be("f");
        d.StackMethodName.Should().Be("m");
        d.StackLineNumber.Should().Be(3);

        var m = new FrameworkLogModel { LogId = "id", CreatedAtUtc = DateTime.MinValue, Message = "m", CorrelationId = "c", Facility = "f", Reference = 1, StackTrace = d };
        m.LogId.Should().Be("id");
        m.CreatedAtUtc.Should().Be(DateTime.MinValue);
        m.Message.Should().Be("m");
        m.CorrelationId.Should().Be("c");
        m.Facility.Should().Be("f");
        m.Reference.Should().Be(1);
        m.StackTrace.Should().BeSameAs(d);
        new FrameworkLogModel().Message.Should().BeEmpty();
    }

    // ---------- Options / threading / tracing ----------

    private class MyOptions { public string Value { get; set; } = "init"; }

    private class MyManager : BaseDynamicOptionsManager<MyOptions>
    {
        public List<string> Overridden { get; } = new();
        public MyManager(IOptionsFactory<MyOptions> factory) : base(factory) { }

        protected override Task OverrideOptionsAsync(string name, MyOptions options)
        {
            Overridden.Add(name);
            options.Value = "overridden";
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task DynamicOptionsManager_Works()
    {
        var factory = new OptionsFactory<MyOptions>(Array.Empty<IConfigureOptions<MyOptions>>(), Array.Empty<IPostConfigureOptions<MyOptions>>());
        var manager = new MyManager(factory);
        await manager.SetAsync();
        await manager.SetAsync("named");
        manager.Overridden.Should().Equal(MsOptions.DefaultName, "named");
        manager.Value.Value.Should().Be("overridden");

        await ((IOptions<MyOptions>)manager).SetAsync();
        await ((IOptions<MyOptions>)manager).SetAsync("n2");
        manager.Overridden.Should().HaveCount(4);

        var plain = MsOptions.Create(new MyOptions());
        await FluentActions.Awaiting(() => plain.SetAsync()).Should().ThrowAsync<BaseException>();
        await FluentActions.Awaiting(() => plain.SetAsync("x")).Should().ThrowAsync<BaseException>();
    }

    [Fact]
    public async Task OneTimeRunners_RunOnce()
    {
        var count = 0;
        var sync = new OneTimeRunner();
        sync.Run(() => count++);
        sync.Run(() => count++);
        count.Should().Be(1);

        var asyncRunner = new AsyncOneTimeRunner();
        await asyncRunner.RunAsync(() => { count++; return Task.CompletedTask; });
        await asyncRunner.RunAsync(() => { count++; return Task.CompletedTask; });
        count.Should().Be(2);
    }

    [Fact]
    public async Task OneTimeRunners_ConcurrentCallsRunOnce()
    {
        var count = 0;
        var runner = new AsyncOneTimeRunner();
        await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => runner.RunAsync(async () => { await Task.Delay(10); Interlocked.Increment(ref count); })));
        count.Should().Be(1);

        var sync = new OneTimeRunner();
        await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => Task.Run(() => sync.Run(() => Interlocked.Increment(ref count)))));
        count.Should().Be(2);
    }

    [Fact]
    public void Tracing_Works()
    {
        var o = new BaseCorrelationIdOptions();
        o.HttpHeaderName.Should().Be("X-Correlation-Id");
        o.SetResponseHeader.Should().BeTrue();
        o.HttpHeaderName = "x";
        o.SetResponseHeader = false;
        o.HttpHeaderName.Should().Be("x");

        var p = new DefaultCorrelationIdProvider();
        var a = p.Get();
        a.Should().HaveLength(32);
        p.Get().Should().NotBe(a);

        ApplicationIdentifier.AppId = "id";
        ApplicationIdentifier.AppName = "name";
        ApplicationIdentifier.AppId.Should().Be("id");
        ApplicationIdentifier.AppName.Should().Be("name");
    }

    // ---------- Microsoft.Extensions helpers ----------

    [Fact]
    public void ConfigurationHelper_BuildConfiguration_Works()
    {
        var dir = Path.Combine(Path.GetTempPath(), "hsn-cfg-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            File.WriteAllText(Path.Combine(dir, "appsettings.json"), "{\"A\":\"1\",\"B\":\"base\"}");
            File.WriteAllText(Path.Combine(dir, "appsettings.Staging.json"), "{\"B\":\"env\"}");

            var cfg = ConfigurationHelper.BuildConfiguration(new BaseConfigurationBuilderOptions
            {
                BasePath = dir,
                EnvironmentName = "Staging",
                CommandLineArgs = new[] { "--C=3" }
            }, b => b.AddInMemoryCollection(new Dictionary<string, string> { ["D"] = "4" }));
            cfg["A"].Should().Be("1");
            cfg["B"].Should().Be("env");
            cfg["C"].Should().Be("3");
            cfg["D"].Should().Be("4");

            var dev1 = ConfigurationHelper.BuildConfiguration(new BaseConfigurationBuilderOptions { BasePath = dir, EnvironmentName = "Development", UserSecretsId = "hsn-test-secrets" });
            dev1["A"].Should().Be("1");
            var dev2 = ConfigurationHelper.BuildConfiguration(new BaseConfigurationBuilderOptions { BasePath = dir, EnvironmentName = "Development", UserSecretsAssembly = typeof(InfrastructureTests).Assembly });
            dev2["A"].Should().Be("1");
            ConfigurationHelper.BuildConfiguration(new BaseConfigurationBuilderOptions { BasePath = dir, EnvironmentName = "Development" })["A"].Should().Be("1");

            var defaults = ConfigurationHelper.BuildConfiguration();
            defaults.Should().NotBeNull();
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void BaseConfigurationBuilderOptions_Defaults()
    {
        var o = new BaseConfigurationBuilderOptions
        {
            UserSecretsAssembly = typeof(string).Assembly,
            UserSecretsId = "u",
            EnvironmentName = "e",
            BasePath = "b",
            EnvironmentVariablesPrefix = "p"
        };
        new BaseConfigurationBuilderOptions().FileName.Should().Be("appsettings");
        o.UserSecretsAssembly.Should().NotBeNull();
        o.UserSecretsId.Should().Be("u");
        o.EnvironmentName.Should().Be("e");
        o.BasePath.Should().Be("b");
        o.EnvironmentVariablesPrefix.Should().Be("p");
    }

    [Fact]
    public void ServiceCollectionExtensions_Work()
    {
        var services = new ServiceCollection();
        services.IsAdded<string>().Should().BeFalse();
        services.AddSingleton("inst");
        services.IsAdded<string>().Should().BeTrue();
        services.IsAdded(typeof(int)).Should().BeFalse();
        services.GetSingletonInstanceOrNull<string>().Should().Be("inst");
        services.GetSingletonInstanceOrNull<IDisposable>().Should().BeNull();
        services.GetSingletonInstance<string>().Should().Be("inst");
        FluentActions.Invoking(() => services.GetSingletonInstance<IDisposable>()).Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ConfigurationExtensions_Work()
    {
        var cfg1 = new ConfigurationBuilder().Build();
        var cfg2 = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string> { ["k"] = "v" }).Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(cfg1);
        services.GetConfiguration().Should().BeSameAs(cfg1);
        services.ReplaceConfiguration(cfg2);
        services.GetConfiguration().Should().BeSameAs(cfg2);

        var withHost = new ServiceCollection();
        withHost.AddSingleton(new HostBuilderContext(new Dictionary<object, object>()) { Configuration = cfg1 });
        withHost.AddSingleton<IConfiguration>(cfg2);
        withHost.GetConfiguration().Should().BeSameAs(cfg1);

        var hostNoConfig = new ServiceCollection();
        hostNoConfig.AddSingleton(new HostBuilderContext(new Dictionary<object, object>()));
        hostNoConfig.AddSingleton<IConfiguration>(cfg2);
        hostNoConfig.GetConfiguration().Should().BeSameAs(cfg2);
    }

    [Fact]
    public void HostBuilder_AddAppSettingsSecretsJson_Works()
    {
        var host = Host.CreateDefaultBuilder().AddAppSettingsSecretsJson().AddAppSettingsSecretsJson(false, false, "x.json");
        FluentActions.Invoking(() => host.Build()).Should().Throw<FileNotFoundException>();
        BaseHostingHostBuilderExtensions.AppSettingsSecretJsonPath.Should().Be("appsettings.secrets.json");

        var ok = Host.CreateDefaultBuilder().AddAppSettingsSecretsJson();
        FluentActions.Invoking(() => ok.Build()).Should().NotThrow();
    }

    [Fact]
    public void LocalizerFactory_Create_Generic_Works()
    {
        var localizer = new Mock<IStringLocalizer>().Object;
        var factory = new Mock<IStringLocalizerFactory>();
        factory.Setup(f => f.Create(typeof(InfrastructureTests))).Returns(localizer);
        factory.Object.Create<InfrastructureTests>().Should().BeSameAs(localizer);
    }

    // ---------- AssemblyHelper catch branch ----------

    private class ThrowingAssembly : Assembly
    {
        public override Type[] GetTypes() => throw new ReflectionTypeLoadException(new[] { typeof(string) }, new Exception[] { new Exception() });
    }

    [Fact]
    public void AssemblyHelper_GetAllTypes_HandlesReflectionTypeLoadException()
        => AssemblyHelper.GetAllTypes(new ThrowingAssembly()).Should().Equal(typeof(string));
}
