using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using FluentAssertions;
using HsnSoft.Base.Localization;
using HsnSoft.Base.Reflection;
using Microsoft.Extensions.Localization;
using Moq;

namespace HsnSoft.Base.Test.Unit.Core;

public class TypeHelperTests
{
    private enum Sample { A }

    [Fact]
    public void IsNonNullablePrimitiveType_Works()
    {
        TypeHelper.IsNonNullablePrimitiveType(typeof(int)).Should().BeTrue();
        TypeHelper.IsNonNullablePrimitiveType(typeof(string)).Should().BeFalse();
    }

    [Fact]
    public void IsFunc_Works()
    {
        TypeHelper.IsFunc(null).Should().BeFalse();
        TypeHelper.IsFunc("s").Should().BeFalse();
        TypeHelper.IsFunc((Func<int>)(() => 1)).Should().BeTrue();
        TypeHelper.IsFunc((Func<int, int>)(x => x)).Should().BeFalse();
        TypeHelper.IsFunc(new List<int>()).Should().BeFalse();
        TypeHelper.IsFunc<int>((Func<int>)(() => 1)).Should().BeTrue();
        TypeHelper.IsFunc<int>(null).Should().BeFalse();
        TypeHelper.IsFunc<string>((Func<int>)(() => 1)).Should().BeFalse();
    }

    [Fact]
    public void IsPrimitiveExtended_Works()
    {
        TypeHelper.IsPrimitiveExtended(typeof(int)).Should().BeTrue();
        TypeHelper.IsPrimitiveExtended(typeof(string)).Should().BeTrue();
        TypeHelper.IsPrimitiveExtended(typeof(int?)).Should().BeTrue();
        TypeHelper.IsPrimitiveExtended(typeof(int?), false).Should().BeFalse();
        TypeHelper.IsPrimitiveExtended(typeof(Sample)).Should().BeFalse();
        TypeHelper.IsPrimitiveExtended(typeof(Sample), true, true).Should().BeTrue();
        TypeHelper.IsPrimitiveExtended(typeof(Sample?), true, true).Should().BeTrue();
        TypeHelper.IsPrimitiveExtended(typeof(object)).Should().BeFalse();
        TypeHelper.IsPrimitiveExtended(typeof(List<int>)).Should().BeFalse();
    }

    [Fact]
    public void Nullable_Helpers_Work()
    {
        TypeHelper.IsNullable(typeof(int?)).Should().BeTrue();
        TypeHelper.IsNullable(typeof(int)).Should().BeFalse();
        typeof(int?).GetFirstGenericArgumentIfNullable().Should().Be(typeof(int));
        typeof(int).GetFirstGenericArgumentIfNullable().Should().Be(typeof(int));
        typeof(List<int>).GetFirstGenericArgumentIfNullable().Should().Be(typeof(List<int>));
        TypeHelper.StripNullable(typeof(int?)).Should().Be(typeof(int));
        TypeHelper.StripNullable(typeof(int)).Should().Be(typeof(int));
    }

    [Fact]
    public void IsEnumerable_Works()
    {
        TypeHelper.IsEnumerable(typeof(List<int>), out var item).Should().BeTrue();
        item.Should().Be(typeof(int));
        TypeHelper.IsEnumerable(typeof(ArrayList), out item).Should().BeTrue();
        item.Should().Be(typeof(object));
        TypeHelper.IsEnumerable(typeof(int), out item).Should().BeFalse();
        item.Should().BeNull();
        TypeHelper.IsEnumerable(typeof(string), out item, false).Should().BeFalse();
        item.Should().BeNull();
        TypeHelper.IsEnumerable(typeof(string), out item).Should().BeTrue();
    }

    [Fact]
    public void IsDictionary_Works()
    {
        TypeHelper.IsDictionary(typeof(Dictionary<string, int>), out var k, out var v).Should().BeTrue();
        k.Should().Be(typeof(string));
        v.Should().Be(typeof(int));
        TypeHelper.IsDictionary(typeof(Hashtable), out k, out v).Should().BeTrue();
        k.Should().Be(typeof(object));
        v.Should().Be(typeof(object));
        TypeHelper.IsDictionary(typeof(int), out k, out v).Should().BeFalse();
        k.Should().BeNull();
        v.Should().BeNull();
    }

    [Fact]
    public void DefaultValue_Works()
    {
        TypeHelper.GetDefaultValue<int>().Should().Be(0);
        TypeHelper.GetDefaultValue(typeof(int)).Should().Be(0);
        TypeHelper.GetDefaultValue(typeof(string)).Should().BeNull();
        TypeHelper.IsDefaultValue(null).Should().BeTrue();
        TypeHelper.IsDefaultValue(0).Should().BeTrue();
        TypeHelper.IsDefaultValue(1).Should().BeFalse();
        TypeHelper.IsDefaultValue("x").Should().BeFalse();
    }

    [Fact]
    public void GetFullNameHandlingNullableAndGenerics_Works()
    {
        TypeHelper.GetFullNameHandlingNullableAndGenerics(typeof(int?)).Should().Be("System.Int32?");
        TypeHelper.GetFullNameHandlingNullableAndGenerics(typeof(Dictionary<string, int>)).Should().Be("System.Collections.Generic.Dictionary<System.String,System.Int32>");
        TypeHelper.GetFullNameHandlingNullableAndGenerics(typeof(string)).Should().Be("System.String");
        FluentActions.Invoking(() => TypeHelper.GetFullNameHandlingNullableAndGenerics(null)).Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData(typeof(string), "string")]
    [InlineData(typeof(int), "number")]
    [InlineData(typeof(long), "number")]
    [InlineData(typeof(bool), "boolean")]
    [InlineData(typeof(char), "string")]
    [InlineData(typeof(double), "number")]
    [InlineData(typeof(float), "number")]
    [InlineData(typeof(decimal), "number")]
    [InlineData(typeof(DateTime), "string")]
    [InlineData(typeof(DateTimeOffset), "string")]
    [InlineData(typeof(TimeSpan), "string")]
    [InlineData(typeof(Guid), "string")]
    [InlineData(typeof(byte), "number")]
    [InlineData(typeof(sbyte), "number")]
    [InlineData(typeof(short), "number")]
    [InlineData(typeof(ushort), "number")]
    [InlineData(typeof(uint), "number")]
    [InlineData(typeof(ulong), "number")]
    [InlineData(typeof(IntPtr), "number")]
    [InlineData(typeof(UIntPtr), "number")]
    [InlineData(typeof(object), "object")]
    [InlineData(typeof(int?), "number?")]
    [InlineData(typeof(List<string>), "System.Collections.Generic.List<string>")]
    [InlineData(typeof(Stream), "System.IO.Stream")]
    public void GetSimplifiedName_Works(Type type, string expected)
        => TypeHelper.GetSimplifiedName(type).Should().Be(expected);

    [Fact]
    public void GetSimplifiedName_Null_Throws()
        => FluentActions.Invoking(() => TypeHelper.GetSimplifiedName(null)).Should().Throw<ArgumentNullException>();

    [Fact]
    public void ConvertFromString_Works()
    {
        TypeHelper.ConvertFromString<int>("5").Should().Be(5);
        TypeHelper.ConvertFromString(typeof(int), null).Should().BeNull();
        using (CultureHelper.Use("tr-TR"))
        {
            TypeHelper.ConvertFromString<double>("1,5").Should().Be(1.5d);
            TypeHelper.ConvertFromString(typeof(double?), "2.5").Should().Be(2.5d);
        }
    }

    [Fact]
    public void IsFloatingType_Works()
    {
        TypeHelper.IsFloatingType(typeof(double)).Should().BeTrue();
        TypeHelper.IsFloatingType(typeof(decimal?)).Should().BeTrue();
        TypeHelper.IsFloatingType(typeof(decimal?), false).Should().BeFalse();
        TypeHelper.IsFloatingType(typeof(int?)).Should().BeFalse();
        TypeHelper.IsFloatingType(typeof(int)).Should().BeFalse();
    }

    [Fact]
    public void ConvertFrom_Works()
    {
        TypeHelper.ConvertFrom<int>("7").Should().Be(7);
        TypeHelper.ConvertFrom(typeof(Guid), "d3b07384-d9a1-4c9e-9f0e-111111111111").Should().BeOfType<Guid>();
    }

    private enum Plain { One, Two }

    private enum Displayed
    {
        [Display(Name = "Uno")] One,
        [Display(Name = "Missing", ResourceType = typeof(Res))] Two,
        [Display(Name = "Title", ResourceType = typeof(Res))] Three,
        [Display(Name = " ")] Four,
        Five,
        [Display(Name = "Fb")] Six,
        [Display(Name = "None")] Seven
    }

    public static class Res
    {
        public static string Title => "Resource Title";
    }

    private static void ConfigureLocalizer(IStringLocalizer localizer)
    {
        var factory = new Mock<IStringLocalizerFactory>();
        factory.Setup(f => f.Create(typeof(Res))).Returns(localizer);
        EnumHelper.Configure(factory.Object, typeof(Res));
    }

    [Fact]
    public void EnumHelper_Basics_Work()
    {
        EnumHelper.GetValues<Plain>().Should().Equal(Plain.One, Plain.Two);
        EnumHelper.GetNames<Plain>().Should().Equal("One", "Two");
        EnumHelper.Parse<Plain>("two").Should().Be(Plain.Two);
        EnumHelper.GetDisplayValue((Plain)5).Should().Be("5");
        EnumHelper.GetDisplayValue(Plain.One).Should().Be("One");
    }

    [Fact]
    public void EnumHelper_DisplayValues_Work()
    {
        // Phase 1: no localizer configured.
        ConfigureLocalizer(null);
        EnumHelper.GetDisplayValue(Displayed.One).Should().Be("Uno");
        EnumHelper.GetDisplayValue(Displayed.Two).Should().Be("Missing");
        EnumHelper.GetDisplayValue(Displayed.Three).Should().Be("Resource Title");
        EnumHelper.GetDisplayValue(Displayed.Four).Should().Be("Four");
        EnumHelper.GetDisplayValue(Displayed.Five).Should().Be("Five");
        EnumHelper.GetDisplayValues<Plain>().Should().Equal("One", "Two");

        // Phase 2: localizer configured.
        var localizer = new Mock<IStringLocalizer>();
        localizer.Setup(l => l[It.IsAny<string>()]).Returns<string>(k => new LocalizedString(k, k));
        localizer.Setup(l => l["Enum:Displayed:Uno"]).Returns(new LocalizedString("Enum:Displayed:Uno", "Localized Uno"));
        localizer.Setup(l => l["Enum:Fb"]).Returns(new LocalizedString("Enum:Fb", "Fallback"));
        localizer.Setup(l => l["Enum:None"]).Returns(new LocalizedString("Enum:None", "Enum:Displayed:None"));
        ConfigureLocalizer(localizer.Object);

        EnumHelper.GetDisplayValue(Displayed.One).Should().Be("Localized Uno");
        EnumHelper.GetDisplayValue(Displayed.Six).Should().Be("Fallback");
        EnumHelper.GetDisplayValue(Displayed.Seven).Should().Be("None");

        ConfigureLocalizer(null);
    }

    [Fact]
    public void CultureHelper_Works()
    {
        var before = CultureInfo.CurrentCulture;
        using (CultureHelper.Use("tr-TR", "en-US"))
        {
            CultureInfo.CurrentCulture.Name.Should().Be("tr-TR");
            CultureInfo.CurrentUICulture.Name.Should().Be("en-US");
        }

        using (CultureHelper.Use("de-DE"))
        {
            CultureInfo.CurrentUICulture.Name.Should().Be("de-DE");
        }

        using (CultureHelper.Use(new CultureInfo("fr-FR")))
        {
            CultureInfo.CurrentCulture.Name.Should().Be("fr-FR");
        }

        CultureInfo.CurrentCulture.Should().Be(before);
        using (CultureHelper.Use("ar-SA"))
        {
            CultureHelper.IsRtl.Should().BeTrue();
        }

        CultureHelper.IsValidCultureCode("en-US").Should().BeTrue();
        CultureHelper.IsValidCultureCode("not a culture !!").Should().BeFalse();
        CultureHelper.IsValidCultureCode(" ").Should().BeFalse();
        CultureHelper.IsValidCultureCode(null).Should().BeFalse();
        CultureHelper.GetBaseCultureName("en-US").Should().Be("en");
        FluentActions.Invoking(() => CultureHelper.Use((string)null)).Should().Throw<ArgumentNullException>();
    }
}
