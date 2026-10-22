#pragma warning disable SYSLIB0051
using System.Runtime.Serialization;
using FluentAssertions;
using Microsoft.Extensions.Logging;

namespace HsnSoft.Base.Test.Unit.Core;

public class BasicTypesTests
{
    private static SerializationInfo InfoOf(Exception ex)
    {
        var info = new SerializationInfo(ex.GetType(), new FormatterConverter());
        ex.GetObjectData(info, new StreamingContext());
        return info;
    }

    [Fact]
    public async Task DisposeHelpers_Work()
    {
        var called = 0;
        new DisposeAction(() => called++).Dispose();
        called.Should().Be(1);
        FluentActions.Invoking(() => new DisposeAction(null)).Should().Throw<ArgumentNullException>();

        await new AsyncDisposeFunc(() => { called++; return Task.CompletedTask; }).DisposeAsync();
        called.Should().Be(2);
        FluentActions.Invoking(() => new AsyncDisposeFunc(null)).Should().Throw<ArgumentNullException>();

        NullDisposable.Instance.Dispose();
        NullDisposable.Instance.Should().BeSameAs(NullDisposable.Instance);
        await NullAsyncDisposable.Instance.DisposeAsync();
        NullAsyncDisposable.Instance.Should().BeSameAs(NullAsyncDisposable.Instance);
    }

    [Fact]
    public void BaseException_Constructors_Work()
    {
        new BaseException().Should().NotBeNull();
        new BaseException("m").Message.Should().Be("m");
        var inner = new Exception("i");
        new BaseException("m", inner).InnerException.Should().BeSameAs(inner);
        new BaseException(InfoOf(new BaseException("ser")), new StreamingContext()).Message.Should().Be("ser");
    }

    [Fact]
    public void BaseHttpException_Works()
    {
        var ex = new BaseHttpException(404, "nf");
        ex.HttpStatusCode.Should().Be(404);
        ex.Message.Should().Be("nf");
        new BaseHttpException(500).Message.Should().BeEmpty();
    }

    [Fact]
    public void BusinessException_Works()
    {
        var inner = new Exception();
        var ex = new BusinessException("code", "msg", inner, LogLevel.Error);
        ex.ErrorCode.Should().Be("code");
        ex.Message.Should().Be("msg");
        ex.InnerException.Should().BeSameAs(inner);
        ex.LogLevel.Should().Be(LogLevel.Error);
        ex.WithData("k", 1).Should().BeSameAs(ex);
        ex.Data["k"].Should().Be(1);

        var def = new BusinessException();
        def.Message.Should().BeEmpty();
        def.LogLevel.Should().Be(LogLevel.Warning);
        def.ErrorCode.Should().BeNull();

        new BusinessException(InfoOf(ex), new StreamingContext()).Message.Should().Be("msg");
    }

    [Fact]
    public void DomainException_Works()
    {
        new DomainException().Should().NotBeNull();
        new DomainException("m").Message.Should().Be("m");
        new DomainException(null).Message.Should().NotBeNull();
        var inner = new Exception();
        new DomainException("m", inner).InnerException.Should().BeSameAs(inner);
        new DomainException(null, inner).Message.Should().BeEmpty();
        var ex = new DomainException("x");
        ex.WithData("a", 2).Should().BeSameAs(ex);
        ex.Data["a"].Should().Be(2);
    }

    [Fact]
    public void NameValue_Works()
    {
        new NameValue().Name.Should().BeNull();
        var nv = new NameValue("n", "v");
        nv.Name.Should().Be("n");
        nv.Value.Should().Be("v");
        new NameValue<int>().Value.Should().Be(0);
        var g = new NameValue<int>("n", 5);
        g.Name.Should().Be("n");
        g.Value.Should().Be(5);
    }

    [Fact]
    public void NamedTypeSelector_Works()
    {
        var s = new NamedTypeSelector("n", t => t == typeof(int));
        s.Name.Should().Be("n");
        s.Predicate(typeof(int)).Should().BeTrue();

        IList<NamedTypeSelector> list = new List<NamedTypeSelector>();
        list.Add("bases", typeof(Exception));
        list.Should().ContainSingle();
        list[0].Predicate(typeof(Exception)).Should().BeTrue();
        list[0].Predicate(typeof(object)).Should().BeTrue();
        list[0].Predicate(typeof(string)).Should().BeFalse();
        FluentActions.Invoking(() => NamedTypeSelectorListExtensions.Add(null, "n", typeof(int))).Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => list.Add(null, typeof(int))).Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => list.Add("n", null)).Should().Throw<ArgumentNullException>();
    }

    private class Target
    {
        public string Name { get; set; }
        public string Locked { get; private set; } = "orig";
        public string ReadOnly { get; } = "ro";
        [Obsolete] public string Ignored { get; set; }
        public string Field = "f";
    }

    [Fact]
    public void ObjectHelper_TrySetProperty_Works()
    {
        var t = new Target();
        ObjectHelper.TrySetProperty(t, x => x.Name, () => "a");
        t.Name.Should().Be("a");
        ObjectHelper.TrySetProperty(t, x => x.Name, x => x.Name + "b");
        t.Name.Should().Be("ab");
        ObjectHelper.TrySetProperty(t, x => x.Locked, () => "changed");
        t.Locked.Should().Be("changed");
        ObjectHelper.TrySetProperty(t, x => x.ReadOnly, () => "x");
        t.ReadOnly.Should().Be("ro");
        ObjectHelper.TrySetProperty(t, x => x.Ignored, () => "x", typeof(ObsoleteAttribute));
        t.Ignored.Should().BeNull();
        ObjectHelper.TrySetProperty(t, x => x.Ignored, () => "x", typeof(SerializableAttribute));
        t.Ignored.Should().Be("x");
        ObjectHelper.TrySetProperty(t, x => x.Name.Length, () => 3);
        ObjectHelper.TrySetProperty(t, x => x.Name + "z", () => "q");
        t.Name.Should().Be("ab");
        ObjectHelper.TrySetProperty(t, x => x.Field, () => "y");
        t.Field.Should().Be("f");
    }

    [Fact]
    public void RandomHelper_Works()
    {
        RandomHelper.GetRandom(5, 6).Should().Be(5);
        RandomHelper.GetRandom(1).Should().Be(0);
        RandomHelper.GetRandom().Should().BeGreaterThanOrEqualTo(0);
        RandomHelper.GetRandomOf(7).Should().Be(7);
        RandomHelper.GetRandomOfList(new List<int> { 8 }).Should().Be(8);
        RandomHelper.GenerateRandomizedList(new[] { 1, 2, 3 }).Should().BeEquivalentTo(new[] { 1, 2, 3 });
        FluentActions.Invoking(() => RandomHelper.GetRandomOf<int>()).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => RandomHelper.GetRandomOfList<int>(null)).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => RandomHelper.GenerateRandomizedList<int>(null)).Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CheckSafe_ReturnsFalseOnInvalid()
    {
        CheckSafe.NotNull(new object(), "p").Should().BeTrue();
        CheckSafe.NotNull<object>(null, "p").Should().BeFalse();
        CheckSafe.NotNull(new object(), "p", "m").Should().BeTrue();
        CheckSafe.NotNull<object>(null, "p", "m").Should().BeFalse();
        CheckSafe.NotNull("a", "p").Should().BeTrue();
        CheckSafe.NotNull((string)null, "p").Should().BeFalse();
        CheckSafe.NotNull("a", "p", 5, 1).Should().BeTrue();
        CheckSafe.NotNull((string)null, "p", 5, 1).Should().BeFalse();
        CheckSafe.NotNullOrWhiteSpace("a", "p").Should().BeTrue();
        CheckSafe.NotNullOrWhiteSpace(" ", "p").Should().BeFalse();
        CheckSafe.NotNullOrEmpty("a", "p").Should().BeTrue();
        CheckSafe.NotNullOrEmpty("", "p").Should().BeFalse();
        CheckSafe.NotNullOrEmpty<int>(new List<int> { 1 }, "p").Should().BeTrue();
        CheckSafe.NotNullOrEmpty<int>(new List<int>(), "p").Should().BeFalse();
        CheckSafe.Length("ab", "p", 5).Should().BeTrue();
        CheckSafe.Length("abcdef", "p", 5).Should().BeFalse();
        CheckSafe.Range((short)2, "p", (short)1, (short)3).Should().BeTrue();
        CheckSafe.Range((short)5, "p", (short)1, (short)3).Should().BeFalse();
        CheckSafe.Range(2, "p", 1, 3).Should().BeTrue();
        CheckSafe.Range(5, "p", 1, 3).Should().BeFalse();
        CheckSafe.Range(2L, "p", 1L, 3L).Should().BeTrue();
        CheckSafe.Range(5L, "p", 1L, 3L).Should().BeFalse();
        CheckSafe.Range(2f, "p", 1f, 3f).Should().BeTrue();
        CheckSafe.Range(5f, "p", 1f, 3f).Should().BeFalse();
        CheckSafe.Range(2d, "p", 1d, 3d).Should().BeTrue();
        CheckSafe.Range(5d, "p", 1d, 3d).Should().BeFalse();
        CheckSafe.Range(2m, "p", 1m, 3m).Should().BeTrue();
        CheckSafe.Range(5m, "p", 1m, 3m).Should().BeFalse();
        CheckSafe.NotDefaultOrNull<int>(3, "p").Should().BeTrue();
        CheckSafe.NotDefaultOrNull<int>(null, "p").Should().BeFalse();
    }
}
