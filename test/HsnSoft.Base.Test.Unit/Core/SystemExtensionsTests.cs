using FluentAssertions;
using HsnSoft.Base.Logging.Abstracts;
using Microsoft.Extensions.Logging;

namespace HsnSoft.Base.Test.Unit.Core;

public class SystemExtensionsTests
{
    private class BaseType { }
    private class MidType : BaseType { }
    private class LeafType : MidType { }

    private class LoggedException : Exception, IHasLogLevel
    {
        public LogLevel LogLevel { get; set; } = LogLevel.Warning;
    }

    [Fact]
    public void IsBetween_IsInclusive()
    {
        5.IsBetween(1, 10).Should().BeTrue();
        1.IsBetween(1, 10).Should().BeTrue();
        10.IsBetween(1, 10).Should().BeTrue();
        0.IsBetween(1, 10).Should().BeFalse();
        11.IsBetween(1, 10).Should().BeFalse();
    }

    [Fact]
    public void ClearTime_RemovesTimeOfDay()
        => new DateTime(2020, 1, 2, 3, 4, 5, 6).ClearTime().Should().Be(new DateTime(2020, 1, 2));

    [Theory]
    [InlineData(DayOfWeek.Saturday, true)]
    [InlineData(DayOfWeek.Sunday, true)]
    [InlineData(DayOfWeek.Monday, false)]
    public void IsWeekend_Works(DayOfWeek day, bool expected) => day.IsWeekend().Should().Be(expected);

    [Theory]
    [InlineData(DayOfWeek.Monday, true)]
    [InlineData(DayOfWeek.Friday, true)]
    [InlineData(DayOfWeek.Saturday, false)]
    [InlineData(DayOfWeek.Sunday, false)]
    public void IsWeekday_Works(DayOfWeek day, bool expected) => day.IsWeekday().Should().Be(expected);

    [Fact]
    public void InvokeSafely_HandlesNullAndInvokesHandlers()
    {
        EventHandler nullHandler = null;
        nullHandler.InvokeSafely(this);
        nullHandler.InvokeSafely(this, EventArgs.Empty);
        EventHandler<EventArgs> nullGeneric = null;
        nullGeneric.InvokeSafely(this, EventArgs.Empty);

        var calls = 0;
        EventHandler handler = (_, _) => calls++;
        handler.InvokeSafely(this);
        handler.InvokeSafely(this, EventArgs.Empty);
        EventHandler<EventArgs> generic = (_, _) => calls++;
        generic.InvokeSafely(this, EventArgs.Empty);
        calls.Should().Be(3);
    }

    [Fact]
    public void ReThrow_PreservesException()
    {
        var ex = new InvalidOperationException("x");
        FluentActions.Invoking(() => ex.ReThrow()).Should().Throw<InvalidOperationException>().WithMessage("x");
    }

    [Fact]
    public void GetLogLevel_UsesIHasLogLevelOrDefault()
    {
        new Exception().GetLogLevel().Should().Be(LogLevel.Error);
        new Exception().GetLogLevel(LogLevel.Critical).Should().Be(LogLevel.Critical);
        new LoggedException().GetLogLevel().Should().Be(LogLevel.Warning);
    }

    [Fact]
    public void GetMessages_CollectsInnerMessages()
    {
        ((Exception)null).GetMessages().Should().BeEmpty();
        var ex = new Exception("outer", new Exception("inner", new Exception(" ")));
        ex.GetMessages().Should().Equal("outer", "inner");
    }

    [Fact]
    public void GetDataList_And_GetDictionaryDataList_ReturnStringData()
    {
        ((Exception)null).GetDataList().Should().BeEmpty();
        ((Exception)null).GetDictionaryDataList().Should().BeEmpty();
        new Exception().GetDataList().Should().BeEmpty();
        new Exception().GetDictionaryDataList().Should().BeEmpty();

        var ex = new Exception();
        ex.Data["a"] = "1";
        ex.Data["b"] = " ";
        ex.Data["c"] = 5;
        ex.GetDataList().Should().Equal("a:1");
        ex.GetDictionaryDataList().Should().ContainSingle().Which.Should().Be(new KeyValuePair<string, string>("a", "1"));
    }

    [Fact]
    public void As_To_Work()
    {
        object o = "abc";
        BaseObjectExtensions.As<string>(o).Should().Be("abc");
        "5".To<int>().Should().Be(5);
        var g = Guid.NewGuid();
        g.ToString().To<Guid>().Should().Be(g);
    }

    [Fact]
    public void IsIn_Works()
    {
        3.IsIn(1, 2, 3).Should().BeTrue();
        4.IsIn(1, 2, 3).Should().BeFalse();
        3.IsIn((IEnumerable<int>)new[] { 1, 2, 3 }).Should().BeTrue();
        4.IsIn((IEnumerable<int>)new[] { 1, 2, 3 }).Should().BeFalse();
    }

    [Fact]
    public void If_AppliesOnlyWhenConditionTrue()
    {
        1.If(true, x => x + 1).Should().Be(2);
        1.If(false, x => x + 1).Should().Be(1);

        var list = new List<int>();
        list.If(true, l => l.Add(1)).Should().BeSameAs(list);
        list.If(false, l => l.Add(2));
        list.Should().Equal(1);
    }

    [Fact]
    public void TypeExtensions_Work()
    {
        typeof(string).GetFullNameWithAssemblyName().Should().StartWith("System.String, ");
        typeof(string).IsAssignableTo<object>().Should().BeTrue();
        typeof(object).IsAssignableTo<string>().Should().BeFalse();
        BaseTypeExtensions.IsAssignableTo(typeof(string), typeof(object)).Should().BeTrue();
        FluentActions.Invoking(() => BaseTypeExtensions.IsAssignableTo(null, typeof(object))).Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => BaseTypeExtensions.IsAssignableTo<object>(null)).Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => BaseTypeExtensions.IsAssignableTo(typeof(string), null)).Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetBaseClasses_Works()
    {
        typeof(LeafType).GetBaseClasses().Should().Equal(typeof(object), typeof(BaseType), typeof(MidType));
        typeof(LeafType).GetBaseClasses(false).Should().Equal(typeof(BaseType), typeof(MidType));
        typeof(LeafType).GetBaseClasses(typeof(BaseType)).Should().Equal(typeof(MidType));
        typeof(LeafType).GetBaseClasses(typeof(BaseType), false).Should().Equal(typeof(MidType));
        FluentActions.Invoking(() => ((Type)null).GetBaseClasses()).Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => ((Type)null).GetBaseClasses(typeof(object))).Should().Throw<ArgumentNullException>();
    }
}
