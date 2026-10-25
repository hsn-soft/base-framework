using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.Reflection;
using HsnSoft.Base.Text;

namespace HsnSoft.Base.Core.Tests;

public class BuildingBlocksTests
{
    private sealed class WithPrivateSetter : IMultiTenant
    {
        public Guid TenantId { get; private set; }
    }

    private sealed class WithoutSetter : IMultiTenant
    {
        public Guid TenantId => Guid.Empty;
    }

    [Fact]
    public void TheTenantAssigner_WritesAPrivateSetter_OnlyWhenThereIsNoTenantYet()
    {
        Guid first = Guid.NewGuid();
        WithPrivateSetter entity = new();

        TenantAssigner.Assign(entity, first);
        TenantAssigner.Assign(entity, Guid.NewGuid());

        Assert.Equal(first, entity.TenantId);
    }

    [Fact]
    public void TheTenantAssigner_SaysWhenThereIsNoSetter_AndRejectsNull()
    {
        Assert.Throws<InvalidOperationException>(() => TenantAssigner.Assign(new WithoutSetter(), Guid.NewGuid()));
        Assert.Throws<ArgumentNullException>(() => TenantAssigner.Assign(null!, Guid.NewGuid()));
    }

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
