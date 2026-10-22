using System.Text;
using FluentAssertions;

namespace HsnSoft.Base.Test.Unit.Core;

public class BaseStringExtensionsTests
{
    private enum Color { Red, Green }

    [Theory]
    [InlineData("abc", "abc/")]
    [InlineData("abc/", "abc/")]
    [InlineData("", "/")]
    public void EnsureEndsWith_AddsCharOnlyWhenMissing(string input, string expected)
        => input.EnsureEndsWith('/').Should().Be(expected);

    [Theory]
    [InlineData("abc", "/abc")]
    [InlineData("/abc", "/abc")]
    public void EnsureStartsWith_AddsCharOnlyWhenMissing(string input, string expected)
        => input.EnsureStartsWith('/').Should().Be(expected);

    [Fact]
    public void Ensure_NullString_Throws()
    {
        FluentActions.Invoking(() => ((string)null).EnsureEndsWith('a')).Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => ((string)null).EnsureStartsWith('a')).Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void IsNullOrEmpty_And_IsNullOrWhiteSpace_Work()
    {
        ((string)null).IsNullOrEmpty().Should().BeTrue();
        "".IsNullOrEmpty().Should().BeTrue();
        " ".IsNullOrEmpty().Should().BeFalse();
        ((string)null).IsNullOrWhiteSpace().Should().BeTrue();
        " ".IsNullOrWhiteSpace().Should().BeTrue();
        "a".IsNullOrWhiteSpace().Should().BeFalse();
    }

    [Fact]
    public void Left_Right_ReturnSubstrings_OrThrow()
    {
        "abcdef".Left(3).Should().Be("abc");
        "abcdef".Right(2).Should().Be("ef");
        "abc".Invoking(s => s.Left(4)).Should().Throw<ArgumentException>();
        "abc".Invoking(s => s.Right(4)).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => ((string)null).Left(1)).Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => ((string)null).Right(1)).Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void NormalizeLineEndings_ConvertsToEnvironmentNewLine()
        => "a\r\nb\rc\nd".NormalizeLineEndings().Should().Be($"a{Environment.NewLine}b{Environment.NewLine}c{Environment.NewLine}d");

    [Fact]
    public void NthIndexOf_FindsNthOccurrence()
    {
        "a-b-c".NthIndexOf('-', 1).Should().Be(1);
        "a-b-c".NthIndexOf('-', 2).Should().Be(3);
        "a-b-c".NthIndexOf('-', 3).Should().Be(-1);
        FluentActions.Invoking(() => ((string)null).NthIndexOf('a', 1)).Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RemovePostFix_Works()
    {
        "FooService".RemovePostFix("Service").Should().Be("Foo");
        "FooService".RemovePostFix("Manager", "Service").Should().Be("Foo");
        "Foo".RemovePostFix("Service").Should().Be("Foo");
        "FooSERVICE".RemovePostFix(StringComparison.OrdinalIgnoreCase, "service").Should().Be("Foo");
        ((string)null).RemovePostFix("a").Should().BeNull();
        "".RemovePostFix("a").Should().BeEmpty();
        "Foo".RemovePostFix().Should().Be("Foo");
        "Foo".RemovePostFix(null).Should().Be("Foo");
    }

    [Fact]
    public void RemovePreFix_Works()
    {
        "IFoo".RemovePreFix("I").Should().Be("Foo");
        "IFoo".RemovePreFix("X", "I").Should().Be("Foo");
        "Foo".RemovePreFix("I").Should().Be("Foo");
        "ifoo".RemovePreFix(StringComparison.OrdinalIgnoreCase, "I").Should().Be("foo");
        ((string)null).RemovePreFix("a").Should().BeNull();
        "".RemovePreFix("a").Should().BeEmpty();
        "Foo".RemovePreFix().Should().Be("Foo");
        "Foo".RemovePreFix(null).Should().Be("Foo");
    }

    [Fact]
    public void ReplaceFirst_ReplacesOnlyFirstOccurrence()
    {
        "a-a-a".ReplaceFirst("a", "b").Should().Be("b-a-a");
        "abc".ReplaceFirst("x", "y").Should().Be("abc");
        "ABC".ReplaceFirst("b", "-", StringComparison.OrdinalIgnoreCase).Should().Be("A-C");
        FluentActions.Invoking(() => ((string)null).ReplaceFirst("a", "b")).Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Split_Overloads_Work()
    {
        BaseStringExtensions.Split("a--b--c", "--").Should().Equal("a", "b", "c");
        BaseStringExtensions.Split("a--b----c", "--", StringSplitOptions.RemoveEmptyEntries).Should().Equal("a", "b", "c");
        $"a{Environment.NewLine}b".SplitToLines().Should().Equal("a", "b");
        $"a{Environment.NewLine}{Environment.NewLine}b".SplitToLines(StringSplitOptions.RemoveEmptyEntries).Should().Equal("a", "b");
    }

    [Fact]
    public void ToCamelCase_Works()
    {
        ((string)null).ToCamelCase().Should().BeNull();
        "  ".ToCamelCase().Should().Be("  ");
        "A".ToCamelCase().Should().Be("a");
        "A".ToCamelCase(true).Should().Be("a");
        "FooBar".ToCamelCase().Should().Be("fooBar");
        "FooBar".ToCamelCase(true).Should().Be("fooBar");
        "XYZ".ToCamelCase(handleAbbreviations: true).Should().Be("xyz");
        "XYZ".ToCamelCase(true, true).Should().Be("xyz");
        "XYZ".ToCamelCase().Should().Be("xYZ");
        "XyZ".ToCamelCase(handleAbbreviations: true).Should().Be("xyZ");
    }

    [Fact]
    public void ToPascalCase_Works()
    {
        ((string)null).ToPascalCase().Should().BeNull();
        "a".ToPascalCase().Should().Be("A");
        "a".ToPascalCase(true).Should().Be("A");
        "fooBar".ToPascalCase().Should().Be("FooBar");
        "fooBar".ToPascalCase(true).Should().Be("FooBar");
    }

    [Fact]
    public void ToSentenceCase_Works()
    {
        ((string)null).ToSentenceCase().Should().BeNull();
        "ThisIsSampleSentence".ToSentenceCase().Should().Be("This is sample sentence");
        "ThisIsSampleSentence".ToSentenceCase(true).Should().Be("This is sample sentence");
    }

    [Fact]
    public void ToKebabCase_Works()
    {
        ((string)null).ToKebabCase().Should().BeNull();
        "ThisIsSample".ToKebabCase().Should().Be("this-is-sample");
        "ThisIsSample".ToKebabCase(true).Should().Be("this-is-sample");
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(" ", " ")]
    [InlineData("ThisIsSampleSentence", "this_is_sample_sentence")]
    [InlineData("Foo_Bar", "foo_bar")]
    [InlineData("HTTPServer", "http_server")]
    [InlineData("Foo Bar", "foo_bar")]
    [InlineData("foo bar", "foo_bar")]
    [InlineData("Foo1Bar", "foo1bar")]
    [InlineData("foo-bar", "foo_bar")]
    [InlineData("-Foo", "foo")]
    [InlineData("Foo 1", "foo_1")]
    [InlineData("fooBar", "foo_bar")]
    public void ToSnakeCase_Works(string input, string expected)
        => input.ToSnakeCase().Should().Be(expected);

    [Fact]
    public void ToEnum_Works()
    {
        "Red".ToEnum<Color>().Should().Be(Color.Red);
        "green".ToEnum<Color>(true).Should().Be(Color.Green);
        FluentActions.Invoking(() => ((string)null).ToEnum<Color>()).Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => ((string)null).ToEnum<Color>(true)).Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToMd5_ReturnsUpperHex()
        => "hello".ToMd5().Should().Be("5D41402ABC4B2A76B9719D911017C592");

    [Fact]
    public void Truncate_Works()
    {
        ((string)null).Truncate(3).Should().BeNull();
        "abc".Truncate(5).Should().Be("abc");
        "abcdef".Truncate(3).Should().Be("abc");
        ((string)null).TruncateFromBeginning(3).Should().BeNull();
        "abc".TruncateFromBeginning(5).Should().Be("abc");
        "abcdef".TruncateFromBeginning(3).Should().Be("def");
    }

    [Fact]
    public void TruncateWithPostfix_Works()
    {
        ((string)null).TruncateWithPostfix(5).Should().BeNull();
        "".TruncateWithPostfix(5).Should().BeEmpty();
        "abc".TruncateWithPostfix(0).Should().BeEmpty();
        "abc".TruncateWithPostfix(5).Should().Be("abc");
        "abcdefgh".TruncateWithPostfix(6).Should().Be("abc...");
        "abcdefgh".TruncateWithPostfix(2).Should().Be("..");
        "abcdefgh".TruncateWithPostfix(6, "~").Should().Be("abcde~");
    }

    [Fact]
    public void GetBytes_Works()
    {
        "abc".GetBytes().Should().Equal(Encoding.UTF8.GetBytes("abc"));
        "abc".GetBytes(Encoding.ASCII).Should().Equal(Encoding.ASCII.GetBytes("abc"));
        FluentActions.Invoking(() => ((string)null).GetBytes(Encoding.UTF8)).Should().Throw<ArgumentNullException>();
        "abc".Invoking(s => s.GetBytes(null)).Should().Throw<ArgumentNullException>();
    }
}
