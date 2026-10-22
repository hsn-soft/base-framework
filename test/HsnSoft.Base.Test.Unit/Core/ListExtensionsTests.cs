using System.Linq.Expressions;
using FluentAssertions;

namespace HsnSoft.Base.Test.Unit.Core;

public class ListExtensionsTests
{
    private static IList<int> L(params int[] items) => new List<int>(items);

    [Fact]
    public void InsertRange_And_FindIndex_Work()
    {
        var l = L(1, 5);
        l.InsertRange(1, new[] { 2, 3 });
        l.Should().Equal(1, 2, 3, 5);
        l.FindIndex(x => x == 3).Should().Be(2);
        l.FindIndex(x => x == 99).Should().Be(-1);
    }

    [Fact]
    public void AddFirst_AddLast_Work()
    {
        var l = L(2);
        l.AddFirst(1);
        l.AddLast(3);
        l.Should().Equal(1, 2, 3);
    }

    [Fact]
    public void InsertAfter_Works()
    {
        var l = L(1, 3);
        l.InsertAfter(1, 2);
        l.Should().Equal(1, 2, 3);
        l.InsertAfter(99, 0);
        l.Should().Equal(0, 1, 2, 3);
        l.InsertAfter((Predicate<int>)(x => x == 3), 4);
        l.Should().Equal(0, 1, 2, 3, 4);
        l.InsertAfter((Predicate<int>)(x => x == 99), -1);
        l.Should().Equal(-1, 0, 1, 2, 3, 4);
    }

    [Fact]
    public void InsertBefore_Works()
    {
        var l = L(2, 3);
        l.InsertBefore(2, 1);
        l.Should().Equal(1, 2, 3);
        l.InsertBefore(99, 4);
        l.Should().Equal(1, 2, 3, 4);
        l.InsertBefore((Predicate<int>)(x => x == 1), 0);
        l.Should().Equal(0, 1, 2, 3, 4);
        l.InsertBefore((Predicate<int>)(x => x == 99), 5);
        l.Should().Equal(0, 1, 2, 3, 4, 5);
    }

    [Fact]
    public void ReplaceWhile_Works()
    {
        var l = L(1, 2, 1);
        l.ReplaceWhile(x => x == 1, 9);
        l.Should().Equal(9, 2, 9);
        l.ReplaceWhile(x => x == 9, x => x + 1);
        l.Should().Equal(10, 2, 10);
    }

    [Fact]
    public void ReplaceOne_Works()
    {
        var l = L(1, 2, 1);
        l.ReplaceOne(x => x == 1, 9);
        l.Should().Equal(9, 2, 1);
        l.ReplaceOne(x => x == 1, x => x + 10);
        l.Should().Equal(9, 2, 11);
        l.ReplaceOne(2, 3);
        l.Should().Equal(9, 3, 11);
        l.ReplaceOne(x => x == 99, 0);
        l.ReplaceOne(x => x == 99, x => 0);
        l.ReplaceOne(99, 0);
        l.Should().Equal(9, 3, 11);
    }

    [Fact]
    public void MoveItem_Works()
    {
        var l = new List<int> { 1, 2, 3 };
        l.MoveItem(x => x == 3, 0);
        l.Should().Equal(3, 1, 2);
        l.MoveItem(x => x == 3, 0);
        l.Should().Equal(3, 1, 2);
        FluentActions.Invoking(() => l.MoveItem(x => x == 1, 5)).Should().Throw<IndexOutOfRangeException>();
    }

    [Fact]
    public void GetOrAdd_Works()
    {
        IList<string> l = new List<string> { "a" };
        l.GetOrAdd(x => x == "a", () => "z").Should().Be("a");
        l.GetOrAdd(x => x == "b", () => "b").Should().Be("b");
        l.Should().Equal("a", "b");
        FluentActions.Invoking(() => BaseListExtensions.GetOrAdd((IList<string>)null, x => true, () => "x")).Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void SortByDependencies_SortsTopologically()
    {
        var deps = new Dictionary<string, string[]> { ["a"] = new[] { "b", "c" }, ["b"] = new[] { "c" }, ["c"] = null };
        new[] { "a", "b", "c" }.SortByDependencies(x => deps[x]).Should().Equal("c", "b", "a");
        new[] { "a", "b", "c" }.SortByDependencies(x => deps[x], StringComparer.Ordinal).Should().Equal("c", "b", "a");
    }

    [Fact]
    public void SortByDependencies_Cycle_Throws()
    {
        var deps = new Dictionary<string, string[]> { ["a"] = new[] { "b" }, ["b"] = new[] { "a" } };
        FluentActions.Invoking(() => new[] { "a", "b" }.SortByDependencies(x => deps[x])).Should().Throw<ArgumentException>();
    }

    [Fact]
    public async Task StreamExtensions_Work()
    {
        var data = new byte[] { 1, 2, 3 };
        var s = new MemoryStream(data);
        s.Position = 2;
        s.GetAllBytes().Should().Equal(data);
        s.Position = 2;
        (await s.GetAllBytesAsync()).Should().Equal(data);

        var dest = new MemoryStream();
        s.Position = 2;
        await BaseStreamExtensions.CopyToAsync(s, dest, CancellationToken.None);
        dest.ToArray().Should().Equal(data);

        var nonSeekable = new NonSeekableStream(data);
        nonSeekable.GetAllBytes().Should().Equal(data);
        (await new NonSeekableStream(data).GetAllBytesAsync()).Should().Equal(data);
        var dest2 = new MemoryStream();
        await BaseStreamExtensions.CopyToAsync(new NonSeekableStream(data), dest2, CancellationToken.None);
        dest2.ToArray().Should().Equal(data);
    }

    private sealed class NonSeekableStream : MemoryStream
    {
        public NonSeekableStream(byte[] buffer) : base(buffer) { }
        public override bool CanSeek => false;
    }

    [Fact]
    public void PredicateBuilder_Works()
    {
        Expression<Func<int, bool>> gt1 = x => x > 1;
        Expression<Func<int, bool>> lt5 = x => x < 5;
        Expression<Func<int, bool>> eq10 = x => x == 10;

        gt1.And(lt5).Compile()(3).Should().BeTrue();
        gt1.And(lt5).Compile()(7).Should().BeFalse();
        lt5.Or(eq10).Compile()(10).Should().BeTrue();
        lt5.Or(eq10).Compile()(7).Should().BeFalse();
        gt1.Extend(lt5, PredicateOperator.And).Compile()(7).Should().BeFalse();
        gt1.Extend(eq10).Compile()(10).Should().BeTrue();

        var started = PredicateBuilder.New<int>();
        started.IsStarted.Should().BeFalse();
        started.UseDefaultExpression.Should().BeTrue();
        started.Compile()(1).Should().BeFalse();
        started.ToString().Should().NotBeNull();
        started.Or(gt1);
        started.IsStarted.Should().BeTrue();
        started.And(lt5).Compile()(3).Should().BeTrue();
        started.Or(eq10).Compile()(10).Should().BeTrue();
        FluentActions.Invoking(() => started.Start(gt1)).Should().Throw<Exception>();
        started.Extend(lt5).Should().NotBeNull();
        started.Extend(lt5, PredicateOperator.And).Should().NotBeNull();

        var all = PredicateBuilder.New<int>(true);
        ((Func<int, bool>)all)(5).Should().BeTrue();
        var andStarter = PredicateBuilder.New<int>(true);
        andStarter.And(gt1).Compile()(0).Should().BeFalse();

        var withExpr = PredicateBuilder.New(gt1);
        withExpr.IsStarted.Should().BeTrue();
        Expression<Func<int, bool>> asExpr = withExpr;
        asExpr.Should().BeSameAs(gt1);
        ((Func<int, bool>)withExpr)(2).Should().BeTrue();
        ExpressionStarter<int> fromExpr = gt1;
        fromExpr.IsStarted.Should().BeTrue();
        ExpressionStarter<int> nullStarter = (Expression<Func<int, bool>>)null;
        nullStarter.Should().BeNull();
        ((Expression<Func<int, bool>>)(ExpressionStarter<int>)null).Should().BeNull();
        ((Func<int, bool>)(ExpressionStarter<int>)null).Should().BeNull();
        var notStartedNoDefault = new ExpressionStarter<int>((Expression<Func<int, bool>>)null) { DefaultExpression = null };
        ((Func<int, bool>)notStartedNoDefault).Should().BeNull();
        notStartedNoDefault.ToString().Should().BeNull();

        withExpr.Body.Should().NotBeNull();
        withExpr.NodeType.Should().Be(ExpressionType.Lambda);
        withExpr.Parameters.Should().HaveCount(1);
        withExpr.Type.Should().Be(typeof(Func<int, bool>));
        withExpr.Name.Should().BeNull();
        withExpr.ReturnType.Should().Be(typeof(bool));
        withExpr.TailCall.Should().BeFalse();
        withExpr.CanReduce.Should().BeFalse();
        withExpr.Update(withExpr.Body, withExpr.Parameters).Should().NotBeNull();
        FluentActions.Invoking(() => withExpr.Compile(null)).Should().NotThrow();
        new ExpressionStarter<int>().IsStarted.Should().BeFalse();
    }
}
