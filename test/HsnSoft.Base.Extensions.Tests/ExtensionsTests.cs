using System.Net;
using HsnSoft.Base.Collections;
using HsnSoft.Base.Net;
using HsnSoft.Base.Text;

namespace HsnSoft.Base.Extensions.Tests;

public class ExtensionsTests
{
    [Theory]
    [InlineData("hello", "aGVsbG8=")]
    [InlineData("", "")]
    [InlineData("çiğ??>", "w6dpxJ8/Pz4=")]
    public void Base64_RoundTripsUtf8Text(string text, string encoded)
    {
        Assert.Equal(encoded, Base64Text.Encode(text));
        Assert.Equal(text, Base64Text.Decode(encoded));
        Assert.True(string.IsNullOrEmpty(encoded) || Base64Text.TryDecode(encoded, out string back) && back == text);
    }

    [Fact]
    public void TheUrlSafeBase64_HasNoPlusSlashOrPadding_AndRoundTrips()
    {
        string text = "??>>~~ çiğ ??>>";

        string encoded = Base64Text.EncodeUrl(text);

        Assert.DoesNotContain('+', encoded);
        Assert.DoesNotContain('/', encoded);
        Assert.DoesNotContain('=', encoded);
        Assert.Equal(text, Base64Text.DecodeUrl(encoded));
        Assert.True(Base64Text.TryDecodeUrl(encoded, out string back));
        Assert.Equal(text, back);
    }

    [Fact]
    public void ABadValue_IsAFormatException_OrFalseForTheTryVariants()
    {
        Assert.Throws<FormatException>(() => Base64Text.Decode("not base64!"));
        Assert.Throws<FormatException>(() => Base64Text.DecodeUrl("not base64!"));
        Assert.False(Base64Text.TryDecode("not base64!", out string text));
        Assert.Equal(string.Empty, text);
        Assert.False(Base64Text.TryDecodeUrl("not base64!", out _));
        Assert.False(Base64Text.TryDecode(null, out _));
        Assert.False(Base64Text.TryDecodeUrl("", out _));
        Assert.Throws<ArgumentNullException>(() => Base64Text.Encode(null!));
        Assert.Throws<ArgumentNullException>(() => Base64Text.Decode(null!));
        Assert.Throws<ArgumentNullException>(() => Base64Text.EncodeUrl(null!));
        Assert.Throws<ArgumentNullException>(() => Base64Text.DecodeUrl(null!));
    }

    [Fact]
    public void JoinAsString_JoinsAnyItems()
    {
        int[] numbers = [1, 2, 3];
        Assert.Equal("1, 2, 3", numbers.JoinAsString(", "));
        Assert.Equal(string.Empty, Array.Empty<string>().JoinAsString(","));
        Assert.Throws<ArgumentNullException>(() => ((IEnumerable<int>)null!).JoinAsString(","));
        Assert.Throws<ArgumentNullException>(() => numbers.JoinAsString(null!));
    }

    [Fact]
    public void WhereIf_FiltersOnlyWhenTheConditionHolds()
    {
        int[] numbers = [1, 2, 3, 4];

        Assert.Equal([2, 4], numbers.WhereIf(true, number => number % 2 == 0));
        Assert.Equal(numbers, numbers.WhereIf(false, number => number % 2 == 0));
        Assert.Throws<ArgumentNullException>(() => ((IEnumerable<int>)null!).WhereIf(true, number => true));
        Assert.Throws<ArgumentNullException>(() => numbers.WhereIf(true, null!));
    }

    [Fact]
    public void IsNullOrEmpty_IsTrueForNullAndForNoItems()
    {
        Assert.True(((List<int>?)null).IsNullOrEmpty());
        Assert.True(new List<int>().IsNullOrEmpty());
        Assert.False(new List<int> { 1 }.IsNullOrEmpty());
    }

    [Fact]
    public void GetOrAdd_OfADictionary_MakesTheValueOnce()
    {
        Dictionary<string, int> values = [];
        int made = 0;

        int first = values.GetOrAdd("a", key => ++made);
        int second = values.GetOrAdd("a", key => ++made);

        Assert.Equal((1, 1, 1), (first, second, made));
        Assert.Equal(1, values["a"]);
        Assert.Throws<ArgumentNullException>(() => ((IDictionary<string, int>)null!).GetOrAdd("a", key => 1));
        Assert.Throws<ArgumentNullException>(() => values.GetOrAdd("a", null!));
    }

    [Fact]
    public void GetOrAdd_OfACollection_FindsTheItemOrAddsOne()
    {
        List<string> names = ["ann", "bob"];

        string found = names.GetOrAdd(name => name.StartsWith('b'), () => "never");
        string added = names.GetOrAdd(name => name.StartsWith('c'), () => "cat");

        Assert.Equal("bob", found);
        Assert.Equal("cat", added);
        Assert.Equal(["ann", "bob", "cat"], names);
        Assert.Throws<ArgumentNullException>(() => ((ICollection<string>)null!).GetOrAdd(name => true, () => "x"));
        Assert.Throws<ArgumentNullException>(() => names.GetOrAdd(null!, () => "x"));
        Assert.Throws<ArgumentNullException>(() => names.GetOrAdd(name => true, null!));
    }

    [Theory]
    [InlineData("8.8.8.8", true)]
    [InlineData("2606:4700:4700::1111", true)]
    [InlineData("127.0.0.1", false)]
    [InlineData("10.0.0.1", false)]
    [InlineData("169.254.169.254", false)]
    [InlineData("192.168.0.1", false)]
    [InlineData("::1", false)]
    [InlineData("fd00::1", false)]
    [InlineData("::ffff:10.0.0.1", false)]
    [InlineData("224.0.0.1", false)]
    public void AnAddress_IsPublic_OnlyWhenItIsOfTheInternet(string address, bool expected)
    {
        Assert.Equal(expected, PublicAddress.IsPublic(IPAddress.Parse(address)));
        Assert.Throws<ArgumentNullException>(() => PublicAddress.IsPublic(null!));
    }

    [Theory]
    [InlineData("files.example.com", "files.example.com", true)]
    [InlineData("FILES.example.com", "files.EXAMPLE.com", true)]
    [InlineData("eu.cdn.example.com", "*.example.com", true)]
    [InlineData("example.com", "*.example.com", false)]
    [InlineData("evilexample.com", "*.example.com", false)]
    [InlineData("other.com", "files.example.com", false)]
    public void AHostPattern_IsAnExactNameOrTheNamesUnderADomain(string host, string pattern, bool expected)
    {
        Assert.Equal(expected, HostPattern.Matches(host, pattern));
        Assert.Throws<ArgumentNullException>(() => HostPattern.Matches(null!, pattern));
        Assert.Throws<ArgumentNullException>(() => HostPattern.Matches(host, null!));
    }
}
