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
}
