using FluentAssertions;

namespace HsnSoft.Base.Test.Unit.Core;

public class CheckTests
{
    [Fact]
    public void NotNull_Generic_Works()
    {
        var o = new object();
        Check.NotNull(o, "o").Should().BeSameAs(o);
        FluentActions.Invoking(() => Check.NotNull<object>(null, "o")).Should().Throw<ArgumentNullException>().WithParameterName("o");
        Check.NotNull(o, "o", "msg").Should().BeSameAs(o);
        FluentActions.Invoking(() => Check.NotNull<object>(null, "o", "msg")).Should().Throw<ArgumentNullException>().WithMessage("msg*");
    }

    [Fact]
    public void NotNull_String_ValidatesLength()
    {
        Check.NotNull("abc", "p").Should().Be("abc");
        FluentActions.Invoking(() => Check.NotNull((string)null, "p")).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => Check.NotNull((string)null, "p", 5)).Should().Throw<ArgumentException>().WithMessage("p can not be null!*");
        FluentActions.Invoking(() => Check.NotNull("abcd", "p", 3)).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => Check.NotNull("a", "p", 5, 2)).Should().Throw<ArgumentException>();
        Check.NotNull("ab", "p", 5, 2).Should().Be("ab");
    }

    [Fact]
    public void NotNullOrWhiteSpace_Validates()
    {
        Check.NotNullOrWhiteSpace("abc", "p").Should().Be("abc");
        FluentActions.Invoking(() => Check.NotNullOrWhiteSpace(" ", "p")).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => Check.NotNullOrWhiteSpace(null, "p")).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => Check.NotNullOrWhiteSpace("abcd", "p", 3)).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => Check.NotNullOrWhiteSpace("a", "p", 5, 2)).Should().Throw<ArgumentException>();
        Check.NotNullOrWhiteSpace("ab", "p", 5, 2).Should().Be("ab");
    }

    [Fact]
    public void NotNullOrEmpty_String_Validates()
    {
        Check.NotNullOrEmpty("abc", "p").Should().Be("abc");
        FluentActions.Invoking(() => Check.NotNullOrEmpty("", "p")).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => Check.NotNullOrEmpty("abcd", "p", 3)).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => Check.NotNullOrEmpty("a", "p", 5, 2)).Should().Throw<ArgumentException>();
        Check.NotNullOrEmpty("ab", "p", 5, 2).Should().Be("ab");
    }

    [Fact]
    public void NotNullOrEmpty_Collection_Validates()
    {
        var list = new List<int> { 1 };
        Check.NotNullOrEmpty<int>(list, "p").Should().BeSameAs(list);
        FluentActions.Invoking(() => Check.NotNullOrEmpty<int>(new List<int>(), "p")).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => Check.NotNullOrEmpty<int>((ICollection<int>)null, "p")).Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AssignableTo_Validates()
    {
        Check.AssignableTo<IDisposable>(typeof(MemoryStream), "t").Should().Be(typeof(MemoryStream));
        FluentActions.Invoking(() => Check.AssignableTo<IDisposable>(typeof(string), "t")).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => Check.AssignableTo<IDisposable>(null, "t")).Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Length_Validates()
    {
        Check.Length(null, "p", 5).Should().BeNull();
        Check.Length("abc", "p", 5).Should().Be("abc");
        FluentActions.Invoking(() => Check.Length("abcdef", "p", 5)).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => Check.Length(null, "p", 5, 1)).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => Check.Length("", "p", 5, 1)).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => Check.Length("a", "p", 5, 2)).Should().Throw<ArgumentException>();
        Check.Length("ab", "p", 5, 2).Should().Be("ab");
    }

    [Fact]
    public void Positive_Validates_AllNumericTypes()
    {
        Check.Positive((short)1, "p").Should().Be(1);
        Check.Positive(1, "p").Should().Be(1);
        Check.Positive(1L, "p").Should().Be(1L);
        Check.Positive(1f, "p").Should().Be(1f);
        Check.Positive(1d, "p").Should().Be(1d);
        Check.Positive(1m, "p").Should().Be(1m);

        FluentActions.Invoking(() => Check.Positive((short)0, "p")).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => Check.Positive((short)-1, "p")).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => Check.Positive(0, "p")).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => Check.Positive(-1, "p")).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => Check.Positive(0L, "p")).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => Check.Positive(-1L, "p")).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => Check.Positive(0f, "p")).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => Check.Positive(-1f, "p")).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => Check.Positive(0d, "p")).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => Check.Positive(-1d, "p")).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => Check.Positive(0m, "p")).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => Check.Positive(-1m, "p")).Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Range_Validates_AllNumericTypes()
    {
        Check.Range((short)2, "p", (short)1, (short)3).Should().Be(2);
        Check.Range((short)2, "p", (short)1).Should().Be(2);
        Check.Range(2, "p", 1, 3).Should().Be(2);
        Check.Range(2, "p", 1).Should().Be(2);
        Check.Range(2L, "p", 1, 3).Should().Be(2L);
        Check.Range(2L, "p", 1).Should().Be(2L);
        Check.Range(2f, "p", 1, 3).Should().Be(2f);
        Check.Range(2f, "p", 1).Should().Be(2f);
        Check.Range(2d, "p", 1, 3).Should().Be(2d);
        Check.Range(2d, "p", 1).Should().Be(2d);
        Check.Range(2m, "p", 1, 3).Should().Be(2m);
        Check.Range(2m, "p", 1).Should().Be(2m);

        FluentActions.Invoking(() => Check.Range((short)0, "p", (short)1, (short)3)).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => Check.Range((short)4, "p", (short)1, (short)3)).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => Check.Range(0, "p", 1, 3)).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => Check.Range(4, "p", 1, 3)).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => Check.Range(0L, "p", 1, 3)).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => Check.Range(4L, "p", 1, 3)).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => Check.Range(0f, "p", 1, 3)).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => Check.Range(4f, "p", 1, 3)).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => Check.Range(0d, "p", 1, 3)).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => Check.Range(4d, "p", 1, 3)).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => Check.Range(0m, "p", 1, 3)).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => Check.Range(4m, "p", 1, 3)).Should().Throw<ArgumentException>();
    }

    [Fact]
    public void NotDefaultOrNull_Validates()
    {
        Check.NotDefaultOrNull<int>(5, "p").Should().Be(5);
        FluentActions.Invoking(() => Check.NotDefaultOrNull<int>(null, "p")).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => Check.NotDefaultOrNull<int>(0, "p")).Should().Throw<ArgumentException>();
    }
}
