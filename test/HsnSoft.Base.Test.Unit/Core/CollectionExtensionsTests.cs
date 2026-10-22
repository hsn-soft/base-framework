using System.Collections.Concurrent;
using FluentAssertions;

namespace HsnSoft.Base.Test.Unit.Core;

public class CollectionExtensionsTests
{
    [Fact]
    public void IsNullOrEmpty_Works()
    {
        ((ICollection<int>)null).IsNullOrEmpty().Should().BeTrue();
        ((ICollection<int>)new List<int>()).IsNullOrEmpty().Should().BeTrue();
        ((ICollection<int>)new List<int> { 1 }).IsNullOrEmpty().Should().BeFalse();
    }

    [Fact]
    public void AddIfNotContains_Item_Works()
    {
        ICollection<int> list = new List<int> { 1 };
        list.AddIfNotContains(1).Should().BeFalse();
        list.AddIfNotContains(2).Should().BeTrue();
        list.Should().Equal(1, 2);
        FluentActions.Invoking(() => ((ICollection<int>)null).AddIfNotContains(1)).Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddIfNotContains_Items_ReturnsAdded()
    {
        ICollection<int> list = new List<int> { 1 };
        list.AddIfNotContains(new[] { 1, 2, 3 }).Should().Equal(2, 3);
        list.Should().Equal(1, 2, 3);
        FluentActions.Invoking(() => ((ICollection<int>)null).AddIfNotContains(new[] { 1 })).Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddIfNotContains_Predicate_Works()
    {
        ICollection<int> list = new List<int> { 1 };
        list.AddIfNotContains(x => x == 1, () => 9).Should().BeFalse();
        list.AddIfNotContains(x => x == 2, () => 2).Should().BeTrue();
        list.Should().Equal(1, 2);
        FluentActions.Invoking(() => ((ICollection<int>)null).AddIfNotContains(x => true, () => 1)).Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => list.AddIfNotContains(null, () => 1)).Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => list.AddIfNotContains(x => true, null)).Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RemoveAll_Works()
    {
        ICollection<int> list = new List<int> { 1, 2, 3, 4 };
        list.RemoveAll(x => x % 2 == 0).Should().Equal(2, 4);
        list.Should().Equal(1, 3);
        list.RemoveAll(new[] { 1 });
        list.Should().Equal(3);
    }

    [Fact]
    public void DictionaryExtensions_Work()
    {
        var dict = new Dictionary<string, int> { ["a"] = 1 };
        dict.GetOrDefault("a").Should().Be(1);
        dict.GetOrDefault("x").Should().Be(0);

        IDictionary<string, int> idict = dict;
        idict.GetOrDefault("a").Should().Be(1);
        idict.GetOrDefault("x").Should().Be(0);

        IReadOnlyDictionary<string, int> ro = dict;
        ro.GetOrDefault("a").Should().Be(1);
        ro.GetOrDefault("x").Should().Be(0);

        var cd = new ConcurrentDictionary<string, int>();
        cd["a"] = 1;
        cd.GetOrDefault("a").Should().Be(1);
        cd.GetOrDefault("x").Should().Be(0);

        idict.GetOrAdd("b", k => k.Length + 1).Should().Be(2);
        idict.GetOrAdd("b", k => 99).Should().Be(2);
        idict.GetOrAdd("c", () => 3).Should().Be(3);
        cd.GetOrAdd("z", () => 7).Should().Be(7);
    }

    [Fact]
    public void DictionaryTryGetValue_Typed_Works()
    {
        IDictionary<string, object> d = new Dictionary<string, object> { ["a"] = 1, ["b"] = "s" };
        BaseDictionaryExtensions.TryGetValue<int>(d, "a", out var i).Should().BeTrue();
        i.Should().Be(1);
        BaseDictionaryExtensions.TryGetValue<int>(d, "b", out var j).Should().BeFalse();
        j.Should().Be(0);
        BaseDictionaryExtensions.TryGetValue<int>(d, "missing", out _).Should().BeFalse();
    }

    [Fact]
    public void ConvertToDynamicObject_Works()
    {
        dynamic o = new Dictionary<string, object> { ["Name"] = "x", ["Age"] = 3 }.ConvertToDynamicObject();
        ((string)o.Name).Should().Be("x");
        ((int)o.Age).Should().Be(3);
    }

    [Fact]
    public void EnumerableExtensions_Work()
    {
        new[] { "a", "b" }.JoinAsString(",").Should().Be("a,b");
        new[] { 1, 2 }.JoinAsString("-").Should().Be("1-2");
        new[] { 1, 2, 3 }.WhereIf(true, x => x > 1).Should().Equal(2, 3);
        new[] { 1, 2, 3 }.WhereIf(false, x => x > 1).Should().Equal(1, 2, 3);
        new[] { 5, 6, 7 }.WhereIf(true, (x, i) => i > 0).Should().Equal(6, 7);
        new[] { 5, 6, 7 }.WhereIf(false, (x, i) => i > 0).Should().Equal(5, 6, 7);
        new object[] { "1", 2 }.ConvertToGenericType<int>().Should().Equal(1, 2);
    }

    [Fact]
    public void Queryable_Extensions_Work()
    {
        var q = Enumerable.Range(1, 10).AsQueryable();
        q.PageBy(2, 3).Should().Equal(3, 4, 5);
        q.PageBy<int, IQueryable<int>>(0, 2).Should().Equal(1, 2);
        q.WhereIf(true, x => x > 8).Should().Equal(9, 10);
        q.WhereIf(false, x => x > 8).Should().HaveCount(10);
        q.WhereIf<int, IQueryable<int>>(true, x => x > 8).Should().Equal(9, 10);
        q.WhereIf<int, IQueryable<int>>(false, x => x > 8).Should().HaveCount(10);
        q.WhereIf(true, (x, i) => i < 2).Should().Equal(1, 2);
        q.WhereIf(false, (x, i) => i < 2).Should().HaveCount(10);
        q.WhereIf<int, IQueryable<int>>(true, (x, i) => i < 2).Should().Equal(1, 2);
        q.WhereIf<int, IQueryable<int>>(false, (x, i) => i < 2).Should().HaveCount(10);
        q.OrderByIf(true, "it desc").First().Should().Be(10);
        q.OrderByIf(false, "it desc").First().Should().Be(1);
        q.OrderByIf<int, IQueryable<int>>(true, "it desc").First().Should().Be(10);
        q.OrderByIf<int, IQueryable<int>>(false, "it desc").First().Should().Be(1);

        IQueryable<int> nullQ = null;
        FluentActions.Invoking(() => nullQ.PageBy(0, 1)).Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => nullQ.PageBy<int, IQueryable<int>>(0, 1)).Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => nullQ.WhereIf(true, x => true)).Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => nullQ.WhereIf<int, IQueryable<int>>(true, x => true)).Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => nullQ.WhereIf(true, (x, i) => true)).Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => nullQ.WhereIf<int, IQueryable<int>>(true, (x, i) => true)).Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => nullQ.OrderByIf(true, "it")).Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => nullQ.OrderByIf<int, IQueryable<int>>(true, "it")).Should().Throw<ArgumentNullException>();
    }
}
