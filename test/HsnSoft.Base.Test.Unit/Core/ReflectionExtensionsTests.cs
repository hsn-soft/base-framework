using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;

namespace HsnSoft.Base.Test.Unit.Core;

public class ReflectionExtensionsTests
{
    [AttributeUsage(AttributeTargets.All)]
    private class MarkAttribute : Attribute { }

    [Mark]
    private class Marked { }

    private class DerivedFromMarked : Marked { }

    private class Unmarked { }

    [Fact]
    public void GetSingleAttributeOrNull_Works()
    {
        typeof(Marked).GetSingleAttributeOrNull<MarkAttribute>().Should().NotBeNull();
        typeof(Unmarked).GetSingleAttributeOrNull<MarkAttribute>().Should().BeNull();
        FluentActions.Invoking(() => ((MemberInfo)null).GetSingleAttributeOrNull<MarkAttribute>()).Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetSingleAttributeOfTypeOrBaseTypesOrNull_WalksBaseTypes()
    {
        typeof(Marked).GetSingleAttributeOfTypeOrBaseTypesOrNull<MarkAttribute>().Should().NotBeNull();
        typeof(DerivedFromMarked).GetSingleAttributeOfTypeOrBaseTypesOrNull<MarkAttribute>().Should().NotBeNull();
        typeof(Unmarked).GetSingleAttributeOfTypeOrBaseTypesOrNull<MarkAttribute>().Should().BeNull();
    }

    [Fact]
    public void GetFileVersion_ReturnsAssemblyFileVersion()
        => typeof(ReflectionExtensionsTests).Assembly.GetFileVersion().Should().NotBeNullOrEmpty();

    [Fact]
    public void JsonSerializerOptionsHelper_Create_RemovesAndAddsConverters()
    {
        var removed = new JsonStringEnumConverter();
        var added = new JsonStringEnumConverter(JsonNamingPolicy.CamelCase);
        var baseOptions = new JsonSerializerOptions();
        baseOptions.Converters.Add(removed);

        var o1 = JsonSerializerOptionsHelper.Create(baseOptions, removed, added);
        o1.Converters.Should().NotContain(removed).And.Contain(added);

        var o2 = JsonSerializerOptionsHelper.Create(baseOptions, c => c == removed, added);
        o2.Converters.Should().NotContain(removed).And.Contain(added);
        baseOptions.Converters.Should().Contain(removed);
    }
}
