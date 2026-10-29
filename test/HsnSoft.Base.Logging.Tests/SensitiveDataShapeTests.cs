using System.Reflection;
using System.Text.Json.Serialization;
using HsnSoft.Base.Logging.Masking;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HsnSoft.Base.Logging.Tests;

public class SensitiveDataShapeTests
{
    public sealed class Card
    {
        [SensitiveData(ShowLast = 4, PreserveLength = true)]
        public string? Number { get; set; }

        public string? Holder { get; set; }
    }

    public sealed class Customer
    {
        public string? Name { get; set; }

        [SensitiveData]
        public string? Phone { get; set; }

        [JsonPropertyName("tc_no")]
        [SensitiveData(SubstituteText = "[hidden]")]
        public string? NationalId { get; set; }

        public Card? Card { get; set; }

        public List<Card> OtherCards { get; set; } = [];

        public Dictionary<string, Card> CardsByName { get; set; } = [];

        public Card[] Spare { get; set; } = [];

        public int? Age { get; set; }

        public DateTime Born { get; set; }

        public string this[int index] => string.Empty;

        public IEnumerable<int> Numbers { get; set; } = [];

        public System.Collections.IEnumerable Untyped { get; set; } = Array.Empty<int>();

        public Customer? Referrer { get; set; }

        [SensitiveData]
        public List<string> Secrets { get; set; } = [];
    }

    public sealed class Envelope<T>
    {
        public T? Payload { get; set; }
    }

    public sealed class Deep
    {
        public Deep? Next { get; set; }

        [SensitiveData]
        public string? Bottom { get; set; }
    }

    [Fact]
    public void TheSensitiveProperties_AreFoundInTheClassAndInsideIt_ByTheirJsonNames()
    {
        IReadOnlyDictionary<string, SensitiveDataAttribute> keys = SensitiveDataShape.Of(typeof(Customer));

        Assert.Equal(["Number", "Phone", "Secrets", "tc_no"], keys.Keys.Order(StringComparer.Ordinal));
        Assert.True(keys["number"].PreserveLength);
        Assert.Equal("[hidden]", keys["TC_NO"].SubstituteText);
        Assert.Same(keys, SensitiveDataShape.Of(typeof(Customer)));
    }

    [Fact]
    public void AGenericEnvelope_IsLookedIntoThroughItsPayload()
    {
        Assert.Contains("Number", SensitiveDataShape.Of(typeof(Envelope<Card>)).Keys);
        Assert.Contains("Number", SensitiveDataShape.Of(typeof(Envelope<List<Card>>)).Keys);
        Assert.Empty(SensitiveDataShape.Of(typeof(Envelope<string>)));
        Assert.Empty(SensitiveDataShape.Of(typeof(string)));
        Assert.Empty(SensitiveDataShape.Of(typeof(int)));
        Assert.Empty(SensitiveDataShape.Of(typeof(List<int>)));
    }

    [Fact]
    public void ALoopOfClasses_AndAVeryDeepOne_AreLookedIntoOnlyAsFarAsTheyGo()
    {
        Assert.Contains("Phone", SensitiveDataShape.Of(typeof(Customer)).Keys);
        Assert.Contains("Bottom", SensitiveDataShape.Of(typeof(Deep)).Keys);
    }

    [Fact]
    public void AnAssembly_GivesTheSensitivePropertiesOfAllItsPublicClasses()
    {
        IReadOnlyDictionary<string, SensitiveDataAttribute> keys = SensitiveDataShape.Of(typeof(SensitiveDataShapeTests).Assembly);

        Assert.Contains("Phone", keys.Keys);
        Assert.Contains("Number", keys.Keys);
        Assert.Throws<ArgumentNullException>(() => SensitiveDataShape.Of((Type)null!));
        Assert.Throws<ArgumentNullException>(() => SensitiveDataShape.Of((Assembly)null!));
    }

    [Fact]
    public void TheRegistry_CollectsTypesAndAssemblies_FirstOneWins()
    {
        SensitiveDataRegistry registry = new();

        registry.Add(typeof(Card)).Add(typeof(SensitiveDataShapeTests).Assembly);

        Assert.True(registry.Keys["number"].PreserveLength);
        Assert.Contains("Phone", registry.Keys.Keys);
        Assert.Throws<ArgumentNullException>(() => registry.Add((Assembly)null!));
        Assert.Throws<ArgumentNullException>(() => registry.Add((Type)null!));
    }

    [Fact]
    public void MaskedByTheClass_TheAttributeSaysHowEachValueIsHidden()
    {
        LogMasker masker = new();

        string masked = masker.MaskText(
            """{"name":"Ayşe","phone":"5551234567","tc_no":"123","card":{"number":"4111111111111111","holder":"A"},"otherCards":[{"number":"5500000000000004"}],"cardsByName":{"main":{"number":"4000"}},"secrets":["a","b"],"age":42}""",
            typeof(Customer));

        Assert.Equal(
            """{"name":"Ayşe","phone":"*****","tc_no":"[hidden]","card":{"number":"************1111","holder":"A"},"otherCards":[{"number":"************0004"}],"cardsByName":{"main":{"number":"4000"}},"secrets":["*****","*****"],"age":42}""",
            masked);
    }

    [Fact]
    public void AMarkedPropertyThatHoldsAnObject_HidesEveryValueInIt_AndNumbersAreHiddenToo()
    {
        LogMasker masker = new();

        string masked = masker.MaskText("""{"phone":{"home":"1","work":"2","other":null},"tc_no":12345}""", typeof(Customer));

        Assert.Equal("""{"phone":{"home":"*****","work":"*****","other":null},"tc_no":"[hidden]"}""", masked);
    }

    [Fact]
    public void WithoutAClass_OnlyTheUsualKeysAndTheRegisteredContractsAreHidden()
    {
        SensitiveDataRegistry registry = new();
        registry.Add(typeof(Customer));

        string registered = new LogMasker(registry).MaskText("""{"phone":"555","password":"p","name":"n"}""");
        string plain = new LogMasker().MaskText("""{"phone":"555","password":"p","name":"n"}""");
        string withShape = new LogMasker(registry).MaskText("""{"number":"4111111111111111"}""", typeof(Card));

        Assert.Equal("""{"phone":"*****","password":"*****","name":"n"}""", registered);
        Assert.Equal("""{"phone":"555","password":"*****","name":"n"}""", plain);
        Assert.Equal("""{"number":"************1111"}""", withShape);
    }

    [Fact]
    public void TheClassOfTheTextWins_OverWhatWasRegistered()
    {
        SensitiveDataRegistry registry = new();
        registry.Add(typeof(Card));

        string masked = new LogMasker(registry).MaskText("""{"number":"4111111111111111","phone":"1"}""", typeof(Customer));

        Assert.Equal("""{"number":"************1111","phone":"*****"}""", masked);
    }

    [Fact]
    public void AnObjectThatCannotBeCopied_IsMaskedThroughItsJson_ByItsClass()
    {
        LogMasker masker = new(_ => throw new InvalidOperationException("no copy"), null);

        object masked = masker.MaskObject(new Card { Number = "4111111111111111", Holder = "A" });

        Assert.Contains("************1111", masked.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void BaseLogging_TheMaskerKnowsTheContractsOfTheAssembliesGiven()
    {
        ServiceCollection services = new();
        services.AddSingleton<ILoggerFactory>(new ListLoggerFactory());
        services.AddBaseLogging(typeof(SensitiveDataShapeTests).Assembly);
        using ServiceProvider provider = services.BuildServiceProvider();

        string masked = provider.GetRequiredService<ILogMasker>().MaskText("""{"phone":"555","name":"n"}""");

        Assert.Equal("""{"phone":"*****","name":"n"}""", masked);
        Assert.Same(provider.GetRequiredService<SensitiveDataRegistry>(), provider.GetRequiredService<SensitiveDataRegistry>());
    }

    [Fact]
    public void AMaskerThatKnowsNoShape_FallsBackToTheTextOverload()
    {
        ILogMasker plain = new PlainMasker();

        Assert.Equal("text", plain.MaskText("text", typeof(Card)));
    }

    private sealed class PlainMasker : ILogMasker
    {
        public string MaskText(string? text) => text ?? string.Empty;

        public object MaskObject<T>(T value) => value!;
    }
}
