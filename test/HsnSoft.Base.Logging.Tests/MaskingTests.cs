using System.Text.Json;
using HsnSoft.Base.Logging.Masking;

namespace HsnSoft.Base.Logging.Tests;

public class MaskingHelperTests
{
    [Fact]
    public void Attribute_HidesTheValueWithAFixedMask()
    {
        Assert.Equal("*****", MaskingHelper.MaskByAttribute("hunter2", new SensitiveDataAttribute()));
    }

    [Fact]
    public void Attribute_CanShowTheStartAndTheEnd()
    {
        SensitiveDataAttribute attribute = new() { ShowFirst = 1, ShowLast = 2 };

        Assert.Equal("h*****r2", MaskingHelper.MaskByAttribute("hunter2", attribute));
    }

    [Fact]
    public void Attribute_CanKeepTheLength()
    {
        SensitiveDataAttribute attribute = new() { ShowFirst = 2, ShowLast = 1, PreserveLength = true };

        Assert.Equal("hu****2", MaskingHelper.MaskByAttribute("hunter2", attribute));
    }

    [Fact]
    public void Attribute_CanUseAnotherMaskCharacter_OrReplaceTheWholeValue()
    {
        Assert.Equal("#####", MaskingHelper.MaskByAttribute("hunter2", new SensitiveDataAttribute { Mask = "#" }));
        Assert.Equal("*****", MaskingHelper.MaskByAttribute("hunter2", new SensitiveDataAttribute { Mask = " " }));
        Assert.Equal("[hidden]", MaskingHelper.MaskByAttribute("hunter2", new SensitiveDataAttribute { SubstituteText = "[hidden]" }));
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(8, 0)]
    [InlineData(0, 8)]
    [InlineData(4, 4)]
    public void Attribute_WithAnImpossibleRange_HidesEverything(int first, int last)
    {
        SensitiveDataAttribute attribute = new() { ShowFirst = first, ShowLast = last };

        Assert.Equal("*****", MaskingHelper.MaskByAttribute("hunter2", attribute));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void EmptyValue_IsReturnedAsItIs(string? value)
    {
        Assert.Equal(value, MaskingHelper.MaskByAttribute(value, new SensitiveDataAttribute()));
        Assert.Equal(value, MaskingHelper.MaskByKeyName(value, "password"));
    }

    [Theory]
    [InlineData("password")]
    [InlineData("Password")]
    [InlineData("access_token")]
    [InlineData("ApiKey")]
    [InlineData("authorization")]
    public void SensitiveKeyName_HidesTheValue(string key)
    {
        Assert.Equal("*****", MaskingHelper.MaskByKeyName("value", key));
    }

    [Theory]
    [InlineData("name")]
    [InlineData("")]
    [InlineData(null)]
    public void OtherKeyName_KeepsTheValue(string? key)
    {
        Assert.Equal("value", MaskingHelper.MaskByKeyName("value", key));
    }

    [Fact]
    public void Attribute_HasSafeDefaults()
    {
        SensitiveDataAttribute attribute = new();

        Assert.False(attribute.PreserveLength);
        Assert.Equal(0, attribute.ShowFirst);
        Assert.Equal(0, attribute.ShowLast);
        Assert.Null(attribute.SubstituteText);
        Assert.Equal("*", attribute.Mask);
        Assert.Equal("*", MaskStrings.Default);
    }
}

public class JsonDataMaskingTests
{
    public sealed class Profile
    {
        public string? Nick { get; set; }

        [SensitiveData]
        public string? Phone { get; set; }
    }

    public struct Wrapper
    {
        public Profile? Inner { get; set; }
    }

    public sealed class StringBag : IEnumerable<string>
    {
        private readonly List<string> _items = ["b1", "b2"];

        public IEnumerator<string> GetEnumerator() => _items.GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public sealed class Account
    {
        public string WriteOnly
        {
            set => WrittenValue = value;
        }

        public string? WrittenValue { get; private set; }

        [SensitiveData]
        public List<int> PinList { get; set; } = [];

        [SensitiveData]
        public string[] ReadOnlyArray { get; } = ["fixed"];

        [SensitiveData]
        public StringBag Bag { get; set; } = new();

        [SensitiveData(ShowFirst = 1, ShowLast = 2)]
        public string? Password { get; set; }

        public string? Name { get; set; }

        public Profile? Profile { get; set; }

        public List<Profile> Profiles { get; set; } = [];

        public Dictionary<string, Profile?> ByKey { get; set; } = [];

        [SensitiveData]
        public Dictionary<string, string> Secrets { get; set; } = [];

        [SensitiveData]
        public string[] Tokens { get; set; } = [];

        [SensitiveData]
        public List<string> TokenList { get; set; } = [];

        [SensitiveData]
        public IReadOnlyList<string> ReadOnlyTokens { get; set; } = [];

        [SensitiveData]
        public HashSet<string> TokenSet { get; set; } = [];

        [SensitiveData]
        public int Pin { get; set; }

        [SensitiveData]
        public string ReadOnlyText => "cannot be replaced";

        [SensitiveData]
        public string? Missing { get; set; }

        public string Thrower => throw new InvalidOperationException("a getter that fails");

        public Account? Self { get; set; }

        public int[] Numbers { get; set; } = [];

        public List<Profile?> Maybe { get; set; } = [];

        public Dictionary<string, int> Counts { get; set; } = [];
    }

    private static Account Sample()
    {
        Account account = new()
        {
            Password = "hunter2",
            Name = "alice",
            Profile = new Profile { Nick = "al", Phone = "5551234" },
            Profiles = [new Profile { Nick = "a", Phone = "111" }, new Profile { Nick = "b", Phone = "222" }],
            ByKey = new Dictionary<string, Profile?> { ["x"] = new Profile { Phone = "333" }, ["none"] = null },
            Secrets = new Dictionary<string, string> { ["key"] = "value" },
            Tokens = ["t1", "t2"],
            TokenList = ["l1"],
            ReadOnlyTokens = ["r1"],
            TokenSet = ["s1"],
            Pin = 1234,
            Numbers = [1, 2],
            Maybe = [null, new Profile { Phone = "444" }],
            Counts = new Dictionary<string, int> { ["a"] = 1 },
            PinList = [1, 2],
            WriteOnly = "written"
        };
        account.Self = account;
        return account;
    }

    [Fact]
    public void SensitiveProperties_AreMasked_AndTheOriginalIsLeftAlone()
    {
        Account original = Sample();

        Account masked = JsonDataMasking.MaskSensitiveData(original);

        Assert.NotSame(original, masked);
        Assert.Equal("h*****r2", masked.Password);
        Assert.Equal("alice", masked.Name);
        Assert.Equal("*****", masked.Profile!.Phone);
        Assert.Equal("al", masked.Profile.Nick);
        Assert.Equal("hunter2", original.Password);
        Assert.Equal("5551234", original.Profile!.Phone);
    }

    [Fact]
    public void NestedObjects_AreMaskedInsideCollectionsAndDictionaries()
    {
        Account masked = JsonDataMasking.MaskSensitiveData(Sample());

        Assert.All(masked.Profiles, profile => Assert.Equal("*****", profile.Phone));
        Assert.Equal("*****", masked.ByKey["x"]!.Phone);
        Assert.Null(masked.ByKey["none"]);
        Assert.Equal("*****", masked.Maybe[1]!.Phone);
        Assert.Null(masked.Maybe[0]);
    }

    [Fact]
    public void SensitiveCollectionsOfText_AreMasked_ElementByElement()
    {
        Account masked = JsonDataMasking.MaskSensitiveData(Sample());

        Assert.Equal("*****", masked.Secrets["key"]);
        Assert.Equal(["*****", "*****"], masked.Tokens);
        Assert.Equal(["*****"], masked.TokenList);
        Assert.Equal(["*****"], masked.ReadOnlyTokens);
    }

    [Fact]
    public void WhatCannotBeReplaced_IsLeftAsItIs()
    {
        Account masked = JsonDataMasking.MaskSensitiveData(Sample());

        Assert.Equal(["s1"], masked.TokenSet);
        Assert.Equal([1, 2], masked.PinList);
        Assert.Equal(["fixed"], masked.ReadOnlyArray);
        Assert.Equal(["b1", "b2"], masked.Bag);
        Assert.Equal("written", masked.WrittenValue);
        Assert.Equal(1234, masked.Pin);
        Assert.Equal("cannot be replaced", masked.ReadOnlyText);
        Assert.Null(masked.Missing);
        Assert.Equal([1, 2], masked.Numbers);
        Assert.Equal(1, masked.Counts["a"]);
    }

    [Fact]
    public void CircularReferences_DoNotLoop()
    {
        Account masked = JsonDataMasking.MaskSensitiveData(Sample());

        Assert.Same(masked, masked.Self);
    }

    [Fact]
    public void Collections_AreMaskedAtTheTopLevelToo()
    {
        List<Profile> profiles = [new Profile { Phone = "1" }, new Profile { Phone = "2" }];
        Dictionary<string, Profile?> byName = new() { ["a"] = new Profile { Phone = "3" }, ["none"] = null };
        Dictionary<string, object> mixed = new() { ["number"] = 7, ["text"] = "plain", ["profile"] = new Profile { Phone = "4" } };

        List<Profile> maskedList = JsonDataMasking.MaskSensitiveData(profiles);
        Dictionary<string, Profile?> maskedDictionary = JsonDataMasking.MaskSensitiveData(byName);
        Dictionary<string, object> maskedMixed = JsonDataMasking.MaskSensitiveData(mixed);

        Assert.All(maskedList, profile => Assert.Equal("*****", profile.Phone));
        Assert.Equal("*****", maskedDictionary["a"]!.Phone);
        Assert.Null(maskedDictionary["none"]);
        Assert.Equal(7, maskedMixed["number"]);
        Assert.Equal("plain", maskedMixed["text"]);
        Assert.Equal("*****", ((Profile)maskedMixed["profile"]).Phone);
    }

    [Fact]
    public void SimpleValues_AndStructs_AreHandled()
    {
        Assert.Equal("text", JsonDataMasking.MaskSensitiveData("text"));
        Assert.Equal(42, JsonDataMasking.MaskSensitiveData(42));
        Wrapper masked = JsonDataMasking.MaskSensitiveData(new Wrapper { Inner = new Profile { Phone = "1" } });
        Assert.Equal("*****", masked.Inner!.Phone);
    }

    [Fact]
    public void AnonymousObjects_AreMaskedInsideTheirNestedObjects()
    {
        var value = new { Name = "x", Profile = new Profile { Phone = "1" } };

        var masked = JsonDataMasking.MaskSensitiveData(value);

        Assert.Equal("*****", masked.Profile.Phone);
    }

    [Fact]
    public void NullData_IsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => JsonDataMasking.MaskSensitiveData<Account>(null!));
    }
}

public class LogMaskerTests
{
    private static readonly LogMasker s_masker = new();

    private sealed class WithDelegate
    {
        public Action? Callback { get; set; } = () => { };

        public string? Token { get; set; } = "t";
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NoText_IsEmpty(string? text)
    {
        Assert.Equal(string.Empty, s_masker.MaskText(text));
    }

    [Fact]
    public void SensitiveKeys_AreMasked_InObjectsArraysAndNestedObjects()
    {
        string masked = s_masker.MaskText("""{"user":"alice","password":"hunter2","nested":{"token":"abc","items":[{"apikey":"k"},{"name":"n"}]},"list":[1,2]}""");

        using JsonDocument document = JsonDocument.Parse(masked);
        JsonElement root = document.RootElement;
        Assert.Equal("alice", root.GetProperty("user").GetString());
        Assert.Equal("*****", root.GetProperty("password").GetString());
        Assert.Equal("*****", root.GetProperty("nested").GetProperty("token").GetString());
        Assert.Equal("*****", root.GetProperty("nested").GetProperty("items")[0].GetProperty("apikey").GetString());
        Assert.Equal("n", root.GetProperty("nested").GetProperty("items")[1].GetProperty("name").GetString());
        Assert.Equal(2, root.GetProperty("list").GetArrayLength());
    }

    [Fact]
    public void SensitiveKeys_AreMasked_WhateverTheTypeOfTheirValue()
    {
        string masked = s_masker.MaskText("""{"password":12345,"token":true,"secret":1.5,"pwd":null,"count":3}""");

        using JsonDocument document = JsonDocument.Parse(masked);
        JsonElement root = document.RootElement;
        Assert.Equal("*****", root.GetProperty("password").GetString());
        Assert.Equal("*****", root.GetProperty("token").GetString());
        Assert.Equal("*****", root.GetProperty("secret").GetString());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("pwd").ValueKind);
        Assert.Equal(3, root.GetProperty("count").GetInt32());
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("null")]
    [InlineData("{broken")]
    public void TextThatIsNotAnObject_IsReturnedAsItIs(string text)
    {
        Assert.Equal(text, s_masker.MaskText(text));
    }

    [Fact]
    public void Object_IsMaskedByItsAttributes()
    {
        object masked = s_masker.MaskObject(new JsonDataMaskingTests.Profile { Nick = "n", Phone = "123" });

        JsonDataMaskingTests.Profile profile = Assert.IsType<JsonDataMaskingTests.Profile>(masked);
        Assert.Equal("*****", profile.Phone);
    }

    [Fact]
    public void NullObject_IsAnEmptyObject()
    {
        object masked = s_masker.MaskObject<string?>(null);

        Assert.Empty(masked.GetType().GetProperties());
    }

    [Fact]
    public void WhenTheAttributesCannotBeApplied_TheKeyNamesAreUsed()
    {
        LogMasker masker = new(_ => throw new InvalidOperationException("cannot clone"));

        object masked = masker.MaskObject(new { User = "alice", Password = "hunter2" });

        JsonElement element = Assert.IsType<JsonElement>(masked);
        Assert.Equal("alice", element.GetProperty("User").GetString());
        Assert.Equal("*****", element.GetProperty("Password").GetString());
    }

    [Fact]
    public void WhenNothingCanMaskIt_NothingOfItIsReturned()
    {
        LogMasker masker = new(_ => throw new InvalidOperationException("cannot clone"));

        object masked = masker.MaskObject(new WithDelegate());

        string text = Assert.IsType<string>(masked);
        Assert.Contains("WithDelegate", text, StringComparison.Ordinal);
        Assert.DoesNotContain("\"t\"", text, StringComparison.Ordinal);
    }
}
