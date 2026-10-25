using HsnSoft.Base.Localization;

namespace HsnSoft.Base.Core.Tests;

public class GuardTests
{
    private static DomainException Fails(Action check) => Assert.Throws<DomainException>(check);

    private static void Says(DomainException error, string key, params object?[] args)
    {
        Assert.Equal(key, error.MessageKey);
        Assert.Equal<object?>(args, [.. error.MessageArgs]);
        Assert.False(string.IsNullOrWhiteSpace(error.Message));
    }

    [Fact]
    public void NotNull_ReturnsTheValue_AndNamesTheParameterWhenItIsNull()
    {
        Assert.Equal("x", Guard.NotNull("x", "Name"));
        Says(Fails(() => Guard.NotNull<string>(null, "Name")), "Validation:Required", "Name");
    }

    [Fact]
    public void NotDefault_RejectsNullAndTheDefault()
    {
        Guid id = Guid.NewGuid();

        Assert.Equal(id, Guard.NotDefault<Guid>(id, "Id"));
        Assert.Equal(3, Guard.NotDefault<int>(3, "Count"));
        Says(Fails(() => Guard.NotDefault<Guid>(null, "Id")), "Validation:Required", "Id");
        Says(Fails(() => Guard.NotDefault<Guid>(Guid.Empty, "Id")), "Validation:Required", "Id");
    }

    [Fact]
    public void NotNullOrEmpty_NeedsAnItem()
    {
        List<int> items = [1];

        Assert.Same(items, Guard.NotNullOrEmpty(items, "Lines"));
        Says(Fails(() => Guard.NotNullOrEmpty<List<int>>(null, "Lines")), "Validation:IsNotEmpty", "Lines");
        Says(Fails(() => Guard.NotNullOrEmpty(new List<int>(), "Lines")), "Validation:IsNotEmpty", "Lines");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NotNullOrWhiteSpace_RejectsWhatIsBlank(string? value)
        => Says(Fails(() => Guard.NotNullOrWhiteSpace(value, "Name", 10)), "Validation:Required", "Name");

    [Fact]
    public void NotNullOrWhiteSpace_ReturnsTheTrimmedText_AndChecksItsLength()
    {
        Assert.Equal("ada", Guard.NotNullOrWhiteSpace("  ada ", "Name", 3));
        Says(Fails(() => Guard.NotNullOrWhiteSpace("adaa", "Name", 3)), "Validation:MaxLength", "Name", 3);
        Says(Fails(() => Guard.NotNullOrWhiteSpace("a", "Name", 10, 2)), "Validation:MinLength", "Name", 2);
        Assert.Equal("a", Guard.NotNullOrWhiteSpace("a", "Name"));
    }

    [Fact]
    public void Length_AllowsNullWhenThereIsNoMinimum_AndKeepsTheTextAsItIs()
    {
        Assert.Null(Guard.Length(null, "Note", 5));
        Assert.Equal(" abc ", Guard.Length(" abc ", "Note", 5));
        Says(Fails(() => Guard.Length("abcdef", "Note", 5)), "Validation:MaxLength", "Note", 5);
        Says(Fails(() => Guard.Length(null, "Password", 10, 8)), "Validation:MinLength", "Password", 8);
        Says(Fails(() => Guard.Length("short", "Password", 10, 8)), "Validation:MinLength", "Password", 8);
    }

    [Fact]
    public void Range_AllowsTheLimits_AndWorksForAnyNumber()
    {
        Assert.Equal(1, Guard.Range(1, "Quantity", 1, 5));
        Assert.Equal(5, Guard.Range(5, "Quantity", 1, 5));
        Assert.Equal(2.5m, Guard.Range(2.5m, "Price", 0m, 10m));
        Says(Fails(() => Guard.Range(0, "Quantity", 1, 5)), "Validation:Range", "Quantity", 1, 5);
        Says(Fails(() => Guard.Range(6L, "Quantity", 1L, 5L)), "Validation:Range", "Quantity", 1L, 5L);
    }

    [Fact]
    public void NotNegative_AllowsZero()
    {
        Assert.Equal(0, Guard.NotNegative(0, "Stock"));
        Assert.Equal(1.5m, Guard.NotNegative(1.5m, "Price"));
        Says(Fails(() => Guard.NotNegative(-1, "Stock")), "Validation:NotNegative", "Stock");
        Says(Fails(() => Guard.NotNegative(-0.01m, "Price")), "Validation:NotNegative", "Price");
    }

    [Fact]
    public void Get_GivesTheTextOrTheKey()
    {
        FixedLocalizer localizer = new();

        Assert.Equal("Hello Ada", localizer.Get("Hello", "Ada"));
        Assert.Equal("No:Such", localizer.Get("No:Such"));
        Assert.Throws<ArgumentNullException>(() => ((IMessageLocalizer)null!).Get("k"));
    }

    private sealed class FixedLocalizer : IMessageLocalizer
    {
        public string? Find(string key, params object?[] args) => key == "Hello" ? $"Hello {args[0]}" : null;
    }
}
