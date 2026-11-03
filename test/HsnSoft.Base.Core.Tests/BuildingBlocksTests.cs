using HsnSoft.Base.Reflection;
using HsnSoft.Base.Text;

namespace HsnSoft.Base.Core.Tests;

public class BuildingBlocksTests
{
    [Theory]
    [InlineData("  Müller ", "MULLER", "muller")]
    [InlineData("Çiğdem", "CIGDEM", "cigdem")]
    [InlineData("ada", "ADA", "ada")]
    [InlineData("", "", "")]
    [InlineData(null, "", "")]
    public void Normalize_AndMinimize_TrimAndDropTheMarks(string? text, string normalized, string minimized)
    {
        Assert.Equal(normalized, StringHelper.Normalize(text));
        Assert.Equal(minimized, StringHelper.Minimize(text));
    }

    private sealed record Item(string Name, int Price);

    [Fact]
    public void TheFilterBuilder_LeavesOutWhatWasNotGiven_AndAndsTheRest()
    {
        string? name = "ap";
        int? maxPrice = 10;
        string? none = null;
        Func<Item, bool> filter = new FilterBuilder<Item>()
            .And(name is null ? null : item => item.Name.StartsWith(name, StringComparison.Ordinal))
            .And(maxPrice is null ? null : item => item.Price <= maxPrice)
            .And(none is null ? null : item => item.Name == none)
            .Build().Compile();

        Assert.True(filter(new Item("apple", 5)));
        Assert.False(filter(new Item("apple", 50)));
        Assert.False(filter(new Item("pear", 5)));
    }

    [Fact]
    public void AFilterWithNoPart_MatchesEverything()
        => Assert.True(new FilterBuilder<Item>().And(null).Build().Compile()(new Item("x", 1)));

    [Theory]
    [InlineData("Acme Corp. 2", false, "", "AcmeCorp.2")]
    [InlineData("a b-c.d_e", false, "_", "a_b-c.d_e")]
    [InlineData("ali@acme.com", false, "", "aliacme.com")]
    [InlineData("ali+tag@acme.com", true, "", "ali+tag@acme.com")]
    [InlineData("Şirket", false, "_", "_irket")]
    [InlineData("\u0141odz", false, "_", "_odz")]
    [InlineData("  ", false, "_", "")]
    [InlineData(null, false, "_", "")]
    public void ReplaceInvalidChars_KeepsOnlyAsciiLettersDigitsAndTheFewMarks(string? text, bool isEmail, string replacement, string expected)
        => Assert.Equal(expected, StringHelper.ReplaceInvalidChars(text, isEmail, replacement));

    [Theory]
    [InlineData("Çanakkale", '_', "canakkale")]
    [InlineData("Acme Corp.", '_', "acme_corp.")]
    [InlineData("  Hello  World!  ", '-', "hello--world")]
    [InlineData("ISPARTA ılık Łódź Øre Đ", '_', "isparta_ilik_lodz_ore_d")]
    [InlineData("---", '-', "")]
    [InlineData(null, '_', "")]
    public void SlugKeyNormalize_MakesAKeyOfAName(string? value, char separator, string expected)
        => Assert.Equal(expected, StringHelper.SlugKeyNormalize(value, separator));

    [Fact]
    public void SlugKeyNormalize_UsesAnUnderscoreByDefault()
        => Assert.Equal("a_b", StringHelper.SlugKeyNormalize("A B"));

    [Fact]
    public void TheFilterBuilder_OrsAPartIntoWhatWasAdded_AndAPartThatIsNullIsLeftOut()
    {
        Func<Item, bool> filter = new FilterBuilder<Item>()
            .Or(item => item.Name == "a")
            .Or(null)
            .Or(item => item.Price > 100)
            .Build().Compile();

        Assert.True(filter(new Item("a", 1)));
        Assert.True(filter(new Item("b", 500)));
        Assert.False(filter(new Item("b", 1)));
    }

    [Fact]
    public void AnyOf_PutsTheAlternativesTogether_ForAnd()
    {
        System.Linq.Expressions.Expression<Func<Item, bool>>? name = FilterBuilder.AnyOf<Item>(item => item.Name.Contains('x', StringComparison.Ordinal), null, item => item.Name.Contains('y', StringComparison.Ordinal));
        Func<Item, bool> filter = new FilterBuilder<Item>().And(name).And(item => item.Price < 10).Build().Compile();

        Assert.True(filter(new Item("xa", 1)));
        Assert.True(filter(new Item("ya", 1)));
        Assert.False(filter(new Item("za", 1)));
        Assert.False(filter(new Item("xa", 50)));
        Assert.Null(FilterBuilder.AnyOf<Item>(null, null));
        Assert.Null(FilterBuilder.AnyOf<Item>());
        Assert.Throws<ArgumentNullException>(() => FilterBuilder.AnyOf<Item>(null!));
    }
}
