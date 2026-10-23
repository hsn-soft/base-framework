namespace HsnSoft.Base.EventBus.Tests;

public class EventNamesTests
{
    [Theory]
    [InlineData("", "Eto", "OrderPlacedEto", "OrderPlaced")]
    [InlineData("", "Eto", "OrderPlaced", "OrderPlaced")]
    [InlineData("Hhs", "Eto", "HhsOrderPlacedEto", "OrderPlaced")]
    [InlineData("Hhs", "", "HhsOrderPlacedEto", "OrderPlacedEto")]
    [InlineData("", "", "OrderPlacedEto", "OrderPlacedEto")]
    [InlineData("Hhs", "Eto", "OrderPlacedEto", "OrderPlaced")]
    public void Prefix_AndSuffix_AreRemovedWhenPresent(string prefix, string suffix, string typeName, string expected)
    {
        EventBusOptions options = new() { EventNamePrefix = prefix, EventNameSuffix = suffix };

        Assert.Equal(expected, EventNames.Trim(options, typeName));
    }

    [Fact]
    public void NameOfAType_IsItsTrimmedTypeName()
    {
        Assert.Equal("Sample", EventNames.For(new EventBusOptions(), typeof(SampleEto)));
    }

    [Fact]
    public void NullArguments_AreRejected()
    {
        Assert.Throws<ArgumentNullException>(() => EventNames.For(new EventBusOptions(), null!));
        Assert.Throws<ArgumentNullException>(() => EventNames.Trim(null!, "x"));
        Assert.Throws<ArgumentNullException>(() => EventNames.Trim(new EventBusOptions(), null!));
    }
}
