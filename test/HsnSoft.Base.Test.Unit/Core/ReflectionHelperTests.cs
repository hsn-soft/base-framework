using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;
using FluentAssertions;
using HsnSoft.Base.Reflection;
using Microsoft.Extensions.Localization;
using Moq;

namespace HsnSoft.Base.Test.Unit.Core;

public class ReflectionHelperTests
{
    public class Inner { public string Name { get; set; } }
    public class Outer { public Inner Inner { get; set; } public int Number { get; set; } }

    public class Consts
    {
        public const string A = "a";
        public static readonly string NotConst = "x";
        public class Nested { public const string B = "b"; public class Deep { public const string C = "c"; } }
    }

    public class DeepNest
    {
        public const string L1 = "l1";
        public class N2 { public const string L2 = "l2"; public class N3 { public const string L3 = "l3"; public class N4 { public const string L4 = "l4"; public class N5 { public const string L5 = "l5"; public class N6 { public const string L6 = "l6"; public class N7 { public const string L7 = "l7"; public class N8 { public const string L8 = "l8"; public class N9 { public const string L9 = "l9"; } } } } } } } }
    }

    public interface IGen<T> { }
    public class GenBase<T> : IGen<T> { }
    public class GenImpl : GenBase<int> { }

    [AttributeUsage(AttributeTargets.All)]
    public class TagAttribute : Attribute { }

    [Tag]
    public class Tagged { public void NoTag() { } [Tag] public void WithTag() { } }

    [Fact]
    public void IsAssignableToGenericType_Works()
    {
        ReflectionHelper.IsAssignableToGenericType(typeof(GenBase<int>), typeof(GenBase<>)).Should().BeTrue();
        ReflectionHelper.IsAssignableToGenericType(typeof(GenImpl), typeof(IGen<>)).Should().BeTrue();
        ReflectionHelper.IsAssignableToGenericType(typeof(GenImpl), typeof(GenBase<>)).Should().BeTrue();
        ReflectionHelper.IsAssignableToGenericType(typeof(string), typeof(IGen<>)).Should().BeFalse();
    }

    [Fact]
    public void GetImplementedGenericTypes_Works()
    {
        ReflectionHelper.GetImplementedGenericTypes(typeof(GenImpl), typeof(IGen<>)).Should().Equal(typeof(IGen<int>));
        ReflectionHelper.GetImplementedGenericTypes(typeof(GenImpl), typeof(GenBase<>)).Should().Equal(typeof(GenBase<int>));
        ReflectionHelper.GetImplementedGenericTypes(typeof(string), typeof(IGen<>)).Should().BeEmpty();
    }

    [Fact]
    public void Attribute_Helpers_Work()
    {
        ReflectionHelper.GetSingleAttributeOrDefault<TagAttribute>(typeof(Tagged)).Should().NotBeNull();
        var def = new TagAttribute();
        ReflectionHelper.GetSingleAttributeOrDefault(typeof(string), def).Should().BeSameAs(def);

        var noTag = typeof(Tagged).GetMethod(nameof(Tagged.NoTag));
        var withTag = typeof(Tagged).GetMethod(nameof(Tagged.WithTag));
        ReflectionHelper.GetSingleAttributeOfMemberOrDeclaringTypeOrDefault<TagAttribute>(withTag).Should().NotBeNull();
        ReflectionHelper.GetSingleAttributeOfMemberOrDeclaringTypeOrDefault<TagAttribute>(noTag).Should().NotBeNull();
        ReflectionHelper.GetSingleAttributeOfMemberOrDeclaringTypeOrDefault<TagAttribute>(typeof(string).GetMethod("Trim", Type.EmptyTypes), def).Should().BeSameAs(def);
        ReflectionHelper.GetSingleAttributeOfMemberOrDeclaringTypeOrDefault<TagAttribute>(typeof(Tagged)).Should().NotBeNull();

        ReflectionHelper.GetAttributesOfMemberOrDeclaringType<TagAttribute>(withTag).Should().HaveCount(1);
        ReflectionHelper.GetAttributesOfMemberOrDeclaringType<TagAttribute>(typeof(Tagged)).Should().HaveCount(1);
    }

    [Fact]
    public void GetValueByPath_Works()
    {
        var o = new Outer { Inner = new Inner { Name = "n" }, Number = 3 };
        ReflectionHelper.GetValueByPath(o, typeof(Outer), "Inner.Name").Should().Be("n");
        ReflectionHelper.GetValueByPath(o, typeof(Outer), typeof(Outer).FullName + ".Number").Should().Be(3);
        ReflectionHelper.GetValueByPath(o, typeof(Outer), "Missing").Should().BeNull();
        ReflectionHelper.GetValueByPath(new Outer(), typeof(Outer), "Inner.Name").Should().BeNull();
    }

    [Fact]
    public void SetValueByPath_Works()
    {
        var o = new Outer { Inner = new Inner() };
        ReflectionHelper.SetValueByPath(o, typeof(Outer), "Number", 5);
        o.Number.Should().Be(5);
        ReflectionHelper.SetValueByPath(o, typeof(Outer), typeof(Outer).FullName + ".Number", 6);
        o.Number.Should().Be(6);
        ReflectionHelper.SetValueByPath(o, typeof(Outer), "Inner.Name", "z");
        o.Inner.Name.Should().Be("z");
    }

    [Fact]
    public void GetPublicConstantsRecursively_Works()
    {
        ReflectionHelper.GetPublicConstantsRecursively(typeof(Consts)).Should().BeEquivalentTo("a", "b", "c");
        var deep = ReflectionHelper.GetPublicConstantsRecursively(typeof(DeepNest));
        deep.Should().Contain("l8").And.NotContain("l9");
    }

    [Fact]
    public void AssemblyHelper_Works()
    {
        var dir = Path.GetDirectoryName(typeof(ReflectionHelperTests).Assembly.Location)!;
        AssemblyHelper.GetAssemblyFiles(dir, SearchOption.TopDirectoryOnly).Should().Contain(f => f.EndsWith("HsnSoft.Base.Core.dll"));
        AssemblyHelper.GetAllTypes(typeof(ReflectionHelperTests).Assembly).Should().Contain(typeof(ReflectionHelperTests));

        var temp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            File.Copy(typeof(ReflectionHelper).Assembly.Location, Path.Combine(temp, "HsnSoft.Base.Core.dll"));
            AssemblyHelper.LoadAssemblies(temp, SearchOption.TopDirectoryOnly).Should().ContainSingle();
        }
        finally
        {
            Directory.Delete(temp, true);
        }
    }

    [Fact]
    public void FilterBuilder_Works()
    {
        var b = new FilterBuilder<int>();
        b.And(null).Should().BeSameAs(b);
        b.Build().Compile()(1).Should().BeTrue();
        b.And(x => x > 1).And(x => x < 5);
        var f = b.Build().Compile();
        f(3).Should().BeTrue();
        f(1).Should().BeFalse();
        f(9).Should().BeFalse();
    }

    private class Color : Enumeration
    {
        public static readonly Color Red = new(1, "Red");
        public static readonly Color Blue = new(2, "Blue");
        public Color(int id, string name) : base(id, name) { }
    }

    private class Other : Enumeration
    {
        public Other(int id, string name) : base(id, name) { }
    }

    [Fact]
    public void Enumeration_Works()
    {
        Enumeration.GetAll<Color>().Should().Equal(Color.Red, Color.Blue);
        Color.Red.ToString().Should().Be("Red");
        Color.Red.Id.Should().Be(1);
        Color.Red.Name.Should().Be("Red");
        Color.Red.CompareTo(Color.Blue).Should().BeNegative();
        Color.Red.Equals(Color.Red).Should().BeTrue();
        Color.Red.Equals(Color.Blue).Should().BeFalse();
        Color.Red.Equals("Red").Should().BeFalse();
        Color.Red.Equals(new Other(1, "Red")).Should().BeFalse();
        Color.Red.GetHashCode().Should().Be(1);
        Enumeration.AbsoluteDifference(Color.Red, Color.Blue).Should().Be(1);
        Enumeration.FromValue<Color>(2).Should().BeSameAs(Color.Blue);
        Enumeration.FromDisplayName<Color>("Red").Should().BeSameAs(Color.Red);
        FluentActions.Invoking(() => Enumeration.FromValue<Color>(99)).Should().Throw<InvalidOperationException>();
        FluentActions.Invoking(() => Enumeration.FromDisplayName<Color>("x")).Should().Throw<InvalidOperationException>();
    }
}
