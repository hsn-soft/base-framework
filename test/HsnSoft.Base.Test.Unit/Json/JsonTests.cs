using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using HsnSoft.Base.Json;
using HsnSoft.Base.Json.Newtonsoft;
using HsnSoft.Base.Json.Newtonsoft.Converters;
using HsnSoft.Base.Json.SystemTextJson;
using HsnSoft.Base.Json.SystemTextJson.JsonConverters;
using HsnSoft.Base.Timing;
using HsnSoft.Base.VirtualFileSystem;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using MsOptions = Microsoft.Extensions.Options.Options;
using JsonException = System.Text.Json.JsonException;
using JsonSerializer = System.Text.Json.JsonSerializer;
using NewtonsoftJson = Newtonsoft.Json.JsonConvert;

namespace HsnSoft.Base.Test.Unit.Json;

public class JsonTests
{

    public enum Level { Low, High }

    public class Dto
    {
        public string FullName { get; set; }
        public Level Level { get; set; }
        public bool Flag { get; set; }
        public DateTime When { get; set; }
        public DateTime? MaybeWhen { get; set; }
        public Dictionary<string, int> Map { get; set; }
    }

    private static ServiceProvider Provider(Action<IServiceCollection> configure = null)
    {
        var services = new ServiceCollection();
        services.AddBaseJsonServiceCollection();
        services.AddTransient<BaseHybridJsonSerializer>();
        services.AddTransient<BaseNewtonsoftJsonSerializerProvider>();
        services.AddTransient<BaseSystemTextJsonSerializerProvider>();
        services.AddTransient<BaseSystemTextJsonUnsupportedTypeMatcher>();
        services.AddTransient<BaseDateTimeConverter>();
        services.AddTransient<BaseNullableDateTimeConverter>();
        services.AddTransient<BaseJsonIsoDateTimeConverter>();
        configure?.Invoke(services);
        return services.BuildServiceProvider();
    }

    private static Utf8JsonReader ReaderFor(string json)
    {
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
        reader.Read();
        return reader;
    }

    private static void ReadDateTime(System.Text.Json.Serialization.JsonConverter<DateTime> c, string json)
    {
        var r = ReaderFor(json);
        c.Read(ref r, typeof(DateTime), new JsonSerializerOptions());
    }

    private static void ReadNullableDateTime(System.Text.Json.Serialization.JsonConverter<DateTime?> c, string json)
    {
        var r = ReaderFor(json);
        c.Read(ref r, typeof(DateTime?), new JsonSerializerOptions());
    }

    // ---------- System.Text.Json ----------

    [Fact]
    public void BaseDateTimeConverter_Works()
    {
        using var sp = Provider();
        var conv = sp.GetRequiredService<BaseDateTimeConverter>();
        var options = new JsonSerializerOptions();

        var reader = ReaderFor("\"2020-01-02T03:04:05Z\"");
        conv.Read(ref reader, typeof(DateTime), options).Kind.Should().Be(DateTimeKind.Utc);
        FluentActions.Invoking(() => ReadDateTime(conv, "5")).Should().Throw<InvalidOperationException>();
        FluentActions.Invoking(() => ReadDateTime(conv, "\"zzz\"")).Should().Throw<JsonException>();

        using var ms = new MemoryStream();
        using (var w = new Utf8JsonWriter(ms)) conv.Write(w, new DateTime(2020, 1, 2, 0, 0, 0, DateTimeKind.Utc), options);
        Encoding.UTF8.GetString(ms.ToArray()).Should().Contain("2020-01-02");

        using var sp2 = Provider(s => s.Configure<BaseJsonOptions>(o => o.DefaultDateTimeFormat = "yyyy-MM-dd"));
        var fmt = sp2.GetRequiredService<BaseDateTimeConverter>();
        var okReader = ReaderFor("\"2020-01-02\"");
        fmt.Read(ref okReader, typeof(DateTime), options).Day.Should().Be(2);
        FluentActions.Invoking(() => ReadDateTime(fmt, "\"nope\"")).Should().Throw<JsonException>();
        FluentActions.Invoking(() => ReadDateTime(fmt, "5")).Should().Throw<JsonException>();
        using var ms2 = new MemoryStream();
        using (var w = new Utf8JsonWriter(ms2)) fmt.Write(w, new DateTime(2020, 1, 2, 0, 0, 0, DateTimeKind.Utc), options);
        Encoding.UTF8.GetString(ms2.ToArray()).Should().Be("\"2020-01-02\"");
    }

    [Fact]
    public void BaseNullableDateTimeConverter_Works()
    {
        using var sp = Provider();
        var conv = sp.GetRequiredService<BaseNullableDateTimeConverter>();
        var options = new JsonSerializerOptions();

        var reader = ReaderFor("\"2020-01-02T03:04:05Z\"");
        conv.Read(ref reader, typeof(DateTime?), options).Should().NotBeNull();
        var notDate = ReaderFor("\"zzz\"");
        conv.Read(ref notDate, typeof(DateTime?), options).Should().BeNull();

        using var msNull = new MemoryStream();
        using (var w = new Utf8JsonWriter(msNull)) conv.Write(w, null, options);
        Encoding.UTF8.GetString(msNull.ToArray()).Should().Be("null");
        using var ms = new MemoryStream();
        using (var w = new Utf8JsonWriter(ms)) conv.Write(w, new DateTime(2020, 1, 2, 0, 0, 0, DateTimeKind.Utc), options);
        Encoding.UTF8.GetString(ms.ToArray()).Should().Contain("2020-01-02");

        using var sp2 = Provider(s => s.Configure<BaseJsonOptions>(o => o.DefaultDateTimeFormat = "yyyy-MM-dd"));
        var fmt = sp2.GetRequiredService<BaseNullableDateTimeConverter>();
        var okReader = ReaderFor("\"2020-01-02\"");
        fmt.Read(ref okReader, typeof(DateTime?), options)!.Value.Day.Should().Be(2);
        FluentActions.Invoking(() => ReadNullableDateTime(fmt, "\"nope\"")).Should().Throw<JsonException>();
        FluentActions.Invoking(() => ReadNullableDateTime(fmt, "5")).Should().Throw<JsonException>();
        using var ms2 = new MemoryStream();
        using (var w = new Utf8JsonWriter(ms2)) fmt.Write(w, new DateTime(2020, 1, 2, 0, 0, 0, DateTimeKind.Utc), options);
        Encoding.UTF8.GetString(ms2.ToArray()).Should().Be("\"2020-01-02\"");
    }

    [Fact]
    public void BaseStringToBooleanConverter_Works()
    {
        var options = new JsonSerializerOptions { Converters = { new BaseStringToBooleanConverter() } };
        JsonSerializer.Deserialize<bool>("\"true\"", options).Should().BeTrue();
        JsonSerializer.Deserialize<bool>("\"False\"", options).Should().BeFalse();
        JsonSerializer.Deserialize<bool>("true", options).Should().BeTrue();
        FluentActions.Invoking(() => JsonSerializer.Deserialize<bool>("\"maybe\"", options)).Should().Throw<JsonException>();
        JsonSerializer.Serialize(true, options).Should().Be("true");
    }

    [Fact]
    public void ObjectToInferredTypesConverter_Works()
    {
        var options = new JsonSerializerOptions { Converters = { new ObjectToInferredTypesConverter() } };
        JsonSerializer.Deserialize<object>("true", options).Should().Be(true);
        JsonSerializer.Deserialize<object>("false", options).Should().Be(false);
        JsonSerializer.Deserialize<object>("5", options).Should().Be(5L);
        JsonSerializer.Deserialize<object>("5.5", options).Should().Be(5.5d);
        JsonSerializer.Deserialize<object>("\"2020-01-02T00:00:00Z\"", options).Should().BeOfType<DateTime>();
        JsonSerializer.Deserialize<object>("\"text\"", options).Should().Be("text");
        JsonSerializer.Deserialize<object>("{\"a\":1}", options).Should().BeOfType<JsonElement>();
        JsonSerializer.Serialize<object>(7, options).Should().Be("7");
    }

    [Fact]
    public void BaseStringToEnum_Converters_Work()
    {
        var factory = new BaseStringToEnumFactory();
        factory.CanConvert(typeof(Level)).Should().BeTrue();
        factory.CanConvert(typeof(int)).Should().BeFalse();
        new BaseStringToEnumFactory(JsonNamingPolicy.CamelCase, false).Should().NotBeNull();

        var options = new JsonSerializerOptions { Converters = { factory } };
        JsonSerializer.Serialize(Level.High, options).Should().Be("1");
        JsonSerializer.Deserialize<Level>("\"Low\"", options).Should().Be(Level.Low);
        JsonSerializer.Deserialize<Level>("1", options).Should().Be(Level.High);
        JsonSerializer.Serialize(Level.Low, options).Should().Be("0");

        var direct = new BaseStringToEnumConverter<Level>();
        direct.CanConvert(typeof(Level)).Should().BeTrue();
        direct.CanConvert(typeof(int)).Should().BeFalse();
        new BaseStringToEnumConverter<Level>(JsonNamingPolicy.CamelCase, true).Should().NotBeNull();
    }

    [Fact]
    public void SystemTextJsonProvider_Works()
    {
        using var sp = Provider(s => s.Configure<BaseSystemTextJsonSerializerOptions>(o => o.UnsupportedTypes.Add<Uri>()));
        var provider = sp.GetRequiredService<BaseSystemTextJsonSerializerProvider>();
        provider.CanHandle(typeof(Dto)).Should().BeTrue();
        provider.CanHandle(typeof(Uri)).Should().BeFalse();

        var dto = new Dto { FullName = "a", Level = Level.High, Flag = true, When = new DateTime(2020, 1, 2, 0, 0, 0, DateTimeKind.Utc) };
        var json = provider.Serialize(dto);
        json.Should().Contain("\"fullName\":\"a\"").And.Contain("\"level\":1");
        provider.Serialize(dto, false, true).Should().Contain("\n");
        provider.Serialize(dto, true, true).Should().Contain("fullName");
        provider.Deserialize<Dto>(json).FullName.Should().Be("a");
        ((Dto)provider.Deserialize(typeof(Dto), json)).Level.Should().Be(Level.High);
        provider.Deserialize<Dto>(provider.Serialize(dto, false), false).FullName.Should().Be("a");
    }

    [Fact]
    public void NewtonsoftProvider_Works()
    {
        using var sp = Provider();
        var provider = sp.GetRequiredService<BaseNewtonsoftJsonSerializerProvider>();
        provider.CanHandle(typeof(object)).Should().BeTrue();

        var dto = new Dto { FullName = "a", When = new DateTime(2020, 1, 2, 0, 0, 0, DateTimeKind.Utc), Map = new() { ["Key"] = 1 } };
        var json = provider.Serialize(dto);
        json.Should().Contain("\"fullName\":\"a\"").And.Contain("\"Key\":1");
        provider.Serialize(dto, false).Should().Contain("\"FullName\"");
        provider.Serialize(dto, true, true).Should().Contain("\n");
        provider.Deserialize<Dto>(json).FullName.Should().Be("a");
        ((Dto)provider.Deserialize(typeof(Dto), json)).Map["Key"].Should().Be(1);
        new BaseNewtonsoftJsonSerializerOptions().Converters.Should().BeEmpty();
    }

    [Fact]
    public void NewtonsoftDateTimeConverter_Works()
    {
        using var sp = Provider();
        var conv = sp.GetRequiredService<BaseJsonIsoDateTimeConverter>();
        conv.CanConvert(typeof(DateTime)).Should().BeTrue();
        conv.CanConvert(typeof(DateTime?)).Should().BeTrue();
        conv.CanConvert(typeof(string)).Should().BeFalse();

        var settings = new JsonSerializerSettings { Converters = { conv } };
        var json = NewtonsoftJson.SerializeObject(new DateTime(2020, 1, 2, 0, 0, 0, DateTimeKind.Utc), settings);
        json.Should().Contain("2020-01-02");
        NewtonsoftJson.DeserializeObject<DateTime>(json, settings).Kind.Should().Be(DateTimeKind.Utc);
        NewtonsoftJson.DeserializeObject<DateTime?>("null", settings).Should().BeNull();
        NewtonsoftJson.SerializeObject((DateTime?)null, settings).Should().Be("null");

        using var sp2 = Provider(s => s.Configure<BaseJsonOptions>(o => o.DefaultDateTimeFormat = "yyyy-MM-dd"));
        var fmt = sp2.GetRequiredService<BaseJsonIsoDateTimeConverter>();
        var s2 = new JsonSerializerSettings { Converters = { fmt } };
        NewtonsoftJson.SerializeObject(new DateTime(2020, 1, 2, 0, 0, 0, DateTimeKind.Utc), s2).Should().Be("\"2020-01-02\"");
    }

    [Fact]
    public void HybridSerializer_UsesProviders()
    {
        using var sp = Provider(s => s.Configure<BaseSystemTextJsonSerializerOptions>(o => o.UnsupportedTypes.Add<Dto>()));
        var serializer = sp.GetRequiredService<BaseHybridJsonSerializer>();

        serializer.Serialize(new { A = 1 }).Should().Be("{\"a\":1}");
        var dto = new Dto { FullName = "x" };
        var json = serializer.Serialize(dto);
        serializer.Deserialize<Dto>(json).FullName.Should().Be("x");
        ((Dto)serializer.Deserialize(typeof(Dto), json)).FullName.Should().Be("x");
        serializer.Deserialize<Level>("\"High\"").Should().Be(Level.High);
        serializer.Serialize(null).Should().Be("null");

        FluentActions.Invoking(() => serializer.Deserialize<Dto>(null)).Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => serializer.Deserialize(typeof(Dto), null)).Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void HybridSerializer_NoProviderThrows()
    {
        var services = new ServiceCollection();
        services.AddOptions<BaseJsonOptions>();
        var sp = services.BuildServiceProvider();
        var serializer = new BaseHybridJsonSerializer(sp.GetRequiredService<IOptions<BaseJsonOptions>>(), sp.GetRequiredService<IServiceScopeFactory>());
        FluentActions.Invoking(() => serializer.Serialize(new object())).Should().Throw<BaseException>();
    }

    [Fact]
    public void JsonOptions_Defaults_And_NonHybridSetup()
    {
        var o = new BaseJsonOptions();
        o.UseHybridSerializer.Should().BeTrue();
        o.Providers.Should().BeEmpty();
        o.DefaultDateTimeFormat.Should().BeNull();

        var services = new ServiceCollection();
        services.Configure<BaseJsonOptions>(x => x.UseHybridSerializer = false);
        services.AddBaseJsonServiceCollection();
        var opts = services.BuildServiceProvider().GetRequiredService<IOptions<BaseJsonOptions>>().Value;
        opts.Providers.Should().ContainSingle().Which.Should().Be(typeof(BaseNewtonsoftJsonSerializerProvider));
    }

    [Fact]
    public void SystemTextJsonOptions_Setup_ConfiguresConverters()
    {
        using var sp = Provider();
        var options = sp.GetRequiredService<IOptions<BaseSystemTextJsonSerializerOptions>>().Value;
        options.JsonSerializerOptions.Converters.Should().Contain(c => c is BaseDateTimeConverter);
        options.JsonSerializerOptions.Converters.Should().Contain(c => c is BaseStringToEnumFactory);
        options.JsonSerializerOptions.Encoder.Should().NotBeNull();
        options.UnsupportedTypes.Should().BeEmpty();

        var matcher = sp.GetRequiredService<BaseSystemTextJsonUnsupportedTypeMatcher>();
        matcher.Match(typeof(Dto)).Should().BeFalse();
    }

}
