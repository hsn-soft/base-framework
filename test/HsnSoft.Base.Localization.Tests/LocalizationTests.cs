using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.Localization.Tests;

public class LocalizationTests
{
    private static string Json(string language, params (string Key, string Text)[] texts)
        => System.Text.Json.JsonSerializer.Serialize(new { culture = language, texts = texts.ToDictionary(pair => pair.Key, pair => pair.Text) });

    private static LocalizationStore Store(Action<LocalizationOptions>? configure = null)
    {
        LocalizationOptions options = new();
        configure?.Invoke(options);
        return new LocalizationStore(Options.Create(options));
    }

    private static Func<string, string?> Layer(params (string Language, string Json)[] files)
        => language => files.Where(file => file.Language == language).Select(file => file.Json).FirstOrDefault();

    [Fact]
    public void TheFrameworksOwnTexts_AreTheFirstLayer_AndCompleteInEveryLanguage()
    {
        LocalizationStore store = Store();

        Assert.Equal("Resource not found.", store.Find("en", "Status:404"));
        Assert.Equal("Kaynak bulunamadı.", store.Find("tr", "Status:404"));
        Assert.Equal("Ресурс не найден.", store.Find("ru", "Status:404"));
        Assert.Empty(store.Validate());
    }

    [Fact]
    public void TheLaterLayerWins_SoTheInnermostIsTheServicesOwn()
    {
        LocalizationStore store = Store(options => options
            .AddJson("shared", Layer(("en", Json("en", ("Status:404", "shared 404"), ("Shared:Only", "from shared")))))
            .AddJson("service", Layer(("en", Json("en", ("Status:404", "service 404"))))));

        Assert.Equal("service 404", store.Find("en", "Status:404"));
        Assert.Equal("from shared", store.Find("en", "Shared:Only"));
        Assert.Equal("Success", store.Find("en", "Status:200"));
    }

    [Fact]
    public void AnEmbeddedLayer_IsReadFromTheAssembly_ByLanguage()
    {
        LocalizationStore store = Store(options => options.AddEmbedded(Assembly.GetExecutingAssembly()));

        Assert.Equal("Hello World", string.Format(System.Globalization.CultureInfo.InvariantCulture, store.Find("en", "Test:Hello")!, "World"));
        Assert.Equal("Merhaba {0}", store.Find("tr", "Test:Hello"));
        Assert.Equal("Burada hiçbir şey yok", store.Find("tr", "Status:404"));
        Assert.Equal("Nothing here (the test layer)", store.Find("en", "Status:404"));
    }

    [Fact]
    public void AMissingText_FallsBackToTheDefaultLanguage_ThenToNothing()
    {
        LocalizationStore store = Store(options => options.AddJson("only-en", Layer(("en", Json("en", ("Only:English", "english"))))));

        Assert.Equal("english", store.Find("tr", "Only:English"));
        Assert.Equal("english", store.Find("xx", "Only:English"));
        Assert.Null(store.Find("tr", "No:Such"));
    }

    [Fact]
    public void AllTexts_OfALanguage_AreItsOwnOverTheDefaultOnes()
    {
        LocalizationStore store = Store(options => options.AddJson("layer", Layer(("en", Json("en", ("A", "a"), ("B", "b"))), ("tr", Json("tr", ("A", "ah"), ("B", "bh"))))));

        IReadOnlyDictionary<string, string> tr = store.All("tr");
        Assert.Equal("ah", tr["A"]);
        Assert.Equal("Başarılı", tr["Status:200"]);
        Assert.Equal("a", store.All("zz")["A"]);
    }

    [Fact]
    public void Validate_TellsWhatTheTranslationsAreMissing()
    {
        LocalizationStore store = Store(options => options.AddJson("layer", Layer(
            ("en", Json("en", ("A", "a {0}"), ("B", "b"))),
            ("tr", Json("tr", ("A", "a"), ("C", "c"))))));

        IReadOnlyList<string> problems = store.Validate();

        Assert.Contains(problems, problem => problem.Contains("'B' has no text in 'tr'", StringComparison.Ordinal));
        Assert.Contains(problems, problem => problem.Contains("'C' is in 'tr' but not in 'en'", StringComparison.Ordinal));
        Assert.Contains(problems, problem => problem.Contains("'A' has other places", StringComparison.Ordinal));
        Assert.Contains(problems, problem => problem.Contains("layer: has no texts in 'ru'", StringComparison.Ordinal));
    }

    [Fact]
    public void ALayerWithTextsOnlyInAnotherLanguage_IsReported()
    {
        LocalizationStore store = Store(options => options.AddJson("layer", Layer(("tr", Json("tr", ("A", "a"))))));

        Assert.Contains(store.Validate(), problem => problem.Contains("none in the default language", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("{ not json", "not valid")]
    [InlineData("{\"culture\":\"tr\",\"texts\":{}}", "not 'en'")]
    [InlineData("{\"culture\":\"en\"}", "not valid")]
    [InlineData("{\"culture\":\"en\",\"texts\":{\"A\":\"1\",\"A\":\"2\"}}", "there twice")]
    [InlineData("{\"culture\":\"en\",\"texts\":{\"\":\"1\"}}", "key is empty")]
    public void ABrokenFile_StopsTheLoad_WithWhichLayerAndWhy(string json, string why)
    {
        LocalizationStore store = Store(options => options.AddJson("broken-layer", language => language == "en" ? json : null));

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => store.Validate());

        Assert.Contains("broken-layer", error.Message, StringComparison.Ordinal);
        Assert.Contains(why, error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TheDefaultLanguage_MustBeSupported_AndTheLanguagesCanBeChosen()
    {
        Assert.Throws<InvalidOperationException>(() => Store(options => options.DefaultLanguage = "de"));

        LocalizationStore store = Store(options => (options.DefaultLanguage, options.SupportedLanguages) = ("tr", ["TR", "en", "tr"]));

        Assert.Equal("tr", store.DefaultLanguage);
        Assert.Equal(["tr", "en"], store.SupportedLanguages);
    }

    [Fact]
    public void TheAccessor_NormalizesTags_AndKeepsTheLanguageOfTheFlow()
    {
        LanguageAccessor accessor = new(Options.Create(new LocalizationOptions()));

        Assert.Equal("tr", accessor.Normalize("tr-TR"));
        Assert.Equal("tr", accessor.Normalize(" TR_tr "));
        Assert.Equal("ru", accessor.Normalize("ru"));
        Assert.Null(accessor.Normalize("de-DE"));
        Assert.Null(accessor.Normalize("*"));
        Assert.Null(accessor.Normalize(" "));
        Assert.Null(accessor.Normalize(null));
        Assert.Equal("en", accessor.Current);
        Assert.Equal(["en", "tr", "ru"], accessor.SupportedLanguages);

        using (accessor.Use("tr-TR"))
        {
            Assert.Equal("tr", accessor.Current);
            using (accessor.Use("ru"))
            {
                Assert.Equal("ru", accessor.Current);
            }

            Assert.Equal("tr", accessor.Current);
        }

        Assert.Equal("en", accessor.Current);
        Assert.Throws<ArgumentException>(() => accessor.Use("de"));
    }

    [Fact]
    public async Task TheLanguage_FollowsTheFlowOfTheCode_AcrossAwait()
    {
        LanguageAccessor accessor = new(Options.Create(new LocalizationOptions()));
        using IDisposable scope = accessor.Use("ru");

        string after = await Task.Run(async () =>
        {
            await Task.Yield();
            return accessor.Current;
        });

        Assert.Equal("ru", after);
    }

    [Fact]
    public void TheAccessor_HasADefaultLanguageToo()
    {
        LanguageAccessor accessor = new(Options.Create(new LocalizationOptions { DefaultLanguage = "TR" }));

        Assert.Equal("tr", accessor.DefaultLanguage);
        Assert.Equal("tr", accessor.Current);
    }

    private static ServiceProvider Provider(Action<LocalizationOptions>? configure = null, IConfiguration? configuration = null)
        => new ServiceCollection().AddLogging().AddBaseLocalization(configuration, configure).BuildServiceProvider();

    [Fact]
    public void FindIfPresent_AnswersLikeFind_AndNullForAKeyThatIsNotThere()
    {
        using ServiceProvider provider = Provider(options => options.AddEmbedded(Assembly.GetExecutingAssembly()));
        IMessageLocalizer localizer = provider.GetRequiredService<IMessageLocalizer>();

        Assert.Equal("Hello Ada", localizer.FindIfPresent("Test:Hello", "Ada"));
        Assert.Null(localizer.FindIfPresent("No:Such"));
    }

    [Fact]
    public void EveryValidationTextOfTheFramework_ExistsInAllThreeLanguages()
    {
        using ServiceProvider provider = Provider();
        string[] keys = ["Required", "IsNotEmpty", "MaxLength", "MinLength", "Range", "NotNegative", "Email", "Phone", "Url", "Pattern", "Compare", "Length", "InvalidValue", "InvalidJson", "Failed"];

        foreach (string language in new[] { "en", "tr", "ru" })
        {
            ILanguageAccessor accessor = provider.GetRequiredService<ILanguageAccessor>();
            using (accessor.Use(language))
            {
                IMessageLocalizer localizer = provider.GetRequiredService<IMessageLocalizer>();
                foreach (string key in keys)
                {
                    Assert.False(string.IsNullOrWhiteSpace(localizer.FindIfPresent("Validation:" + key)), $"{language}: Validation:{key}");
                }
            }
        }
    }

    [Fact]
    public void TheMessageLocalizer_FindsInTheCurrentLanguage_WithTheArguments_AndNullWhenNoLanguageHasIt()
    {
        using ServiceProvider provider = Provider(options => options.AddEmbedded(Assembly.GetExecutingAssembly()));
        IMessageLocalizer localizer = provider.GetRequiredService<IMessageLocalizer>();
        ILanguageAccessor language = provider.GetRequiredService<ILanguageAccessor>();

        Assert.Equal("Hello Ada", localizer.Find("Test:Hello", "Ada"));
        using (language.Use("tr"))
        {
            Assert.Equal("Merhaba Ada", localizer.Find("Test:Hello", "Ada"));
            Assert.Equal("Merhaba {0}", localizer.Find("Test:Hello"));
            Assert.Null(localizer.Find("No:Such"));
            Assert.Null(localizer.Find("No:Such"));
        }

        Assert.Throws<ArgumentNullException>(() => localizer.Find(null!));
    }

    [Fact]
    public void ATextWithAPlaceThatIsNotGiven_IsNotAnError()
    {
        using ServiceProvider provider = Provider(options => options.AddJson("layer", Layer(("en", Json("en", ("Two", "{0} and {1}"))))));

        Assert.Equal("{0} and {1}", provider.GetRequiredService<IMessageLocalizer>().Find("Two", "only one"));
    }

    [Fact]
    public void TheStandardLocalizers_AreTheSameTexts_NotFoundIsTheKey()
    {
        using ServiceProvider provider = Provider(options => options.AddEmbedded(Assembly.GetExecutingAssembly()));
        ILanguageAccessor language = provider.GetRequiredService<ILanguageAccessor>();
        IStringLocalizer plain = provider.GetRequiredService<IStringLocalizer>();
        IStringLocalizer<LocalizationTests> typed = provider.GetRequiredService<IStringLocalizer<LocalizationTests>>();
        IStringLocalizerFactory factory = provider.GetRequiredService<IStringLocalizerFactory>();

        using (language.Use("tr"))
        {
            Assert.Equal("Merhaba Ada", plain["Test:Hello", "Ada"].Value);
            Assert.Equal("Sadece burada", typed["Test:Only"].Value);
            Assert.Equal("Sadece burada", factory.Create(typeof(string))["Test:Only"].Value);
            Assert.Equal("Sadece burada", factory.Create("any", "where")["Test:Only"].Value);
            Assert.Contains(typed.GetAllStrings(includeParentCultures: true), text => text.Name == "Test:Only" && text.Value == "Sadece burada");
        }

        LocalizedString missing = plain["No:Such"];
        Assert.True(missing.ResourceNotFound);
        Assert.Equal("No:Such", missing.Value);
        Assert.True(plain["No:Such", "x"].ResourceNotFound);
        Assert.Equal("Hello Ada", typed["Test:Hello", "Ada"].Value);
        Assert.Throws<ArgumentNullException>(() => plain[null!]);
    }

    [Fact]
    public void TheLanguagesAreReadFromTheConfiguration_AndTheLayersAddedByEachCall_KeepTheirOrder()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Localization:DefaultLanguage"] = "tr",
            ["Localization:SupportedLanguages:0"] = "tr",
            ["Localization:SupportedLanguages:1"] = "en",
        }).Build();
        ServiceCollection services = new();
        services.AddLogging().AddBaseLocalization(configuration, options => options.AddJson("shared", Layer(("tr", Json("tr", ("K", "shared"))), ("en", Json("en", ("K", "shared-en"))))));
        services.AddBaseLocalization(configure: options => options.AddJson("service", Layer(("tr", Json("tr", ("K", "service"))), ("en", Json("en", ("K", "service-en"))))));
        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.Equal("tr", provider.GetRequiredService<ILanguageAccessor>().Current);
        Assert.Equal(["tr", "en"], provider.GetRequiredService<ILanguageAccessor>().SupportedLanguages);
        Assert.Equal("service", provider.GetRequiredService<IMessageLocalizer>().Find("K"));
        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddBaseLocalization());
    }

    [Fact]
    public async Task AtStart_ABrokenFileStopsTheHost_AndProblemsAreOnlyWarnings()
    {
        using ServiceProvider good = Provider(options => options.AddJson("layer", Layer(("en", Json("en", ("A", "a"))))));
        foreach (IHostedService service in good.GetServices<IHostedService>())
        {
            await service.StartAsync(CancellationToken.None);
            await service.StopAsync(CancellationToken.None);
        }

        using ServiceProvider broken = Provider(options => options.AddJson("broken", language => language == "en" ? "{" : null));
        IHostedService startup = broken.GetServices<IHostedService>().Single();
        await Assert.ThrowsAsync<InvalidOperationException>(() => startup.StartAsync(CancellationToken.None));
    }

    [Fact]
    public void TheLayerRegistrations_RejectNothingButNull()
    {
        LocalizationOptions options = new();

        Assert.Throws<ArgumentNullException>(() => options.AddEmbedded(null!));
        Assert.Throws<ArgumentException>(() => options.AddJson(" ", _ => null));
        Assert.Throws<ArgumentNullException>(() => options.AddJson("x", null!));
        Assert.Throws<ArgumentNullException>(() => new LocalizationStore(null!));
        LocalizationStore store = Store();
        Assert.Throws<ArgumentNullException>(() => store.Find(null!, "k"));
        Assert.Throws<ArgumentNullException>(() => store.Find("en", null!));
        Assert.Throws<ArgumentNullException>(() => store.All(null!));
    }

    [Fact]
    public void AnAssemblyWithoutTexts_AddsNothing()
    {
        LocalizationStore store = Store(options => options.AddEmbedded(typeof(object).Assembly));

        Assert.Equal("Success", store.Find("en", "Status:200"));
    }

    [Fact]
    public void AMissingKeyIsLoggedOnce()
    {
        List<string> logged = [];
        ServiceCollection services = new();
        services.AddLogging(logging => logging.AddProvider(new CaptureProvider(logged)));
        services.AddBaseLocalization();
        using ServiceProvider provider = services.BuildServiceProvider();
        IMessageLocalizer localizer = provider.GetRequiredService<IMessageLocalizer>();

        localizer.Find("No:Such");
        localizer.Find("No:Such");

        Assert.Single(logged);
        Assert.Contains("No:Such", logged[0], StringComparison.Ordinal);
    }

    private sealed class CaptureProvider(List<string> logged) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new CaptureLogger(logged);

        public void Dispose()
        {
        }
    }

    private sealed class CaptureLogger(List<string> logged) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Warning)
            {
                logged.Add(formatter(state, exception));
            }
        }
    }
}
