namespace HsnSoft.Base.Core.Tests;

public class NotDefaultTests
{
    [Theory]
    [InlineData(null, true)]
    [InlineData("6f0e1d52-1d9e-4f0b-8f0a-2b8d6f1a9c11", true)]
    [InlineData("00000000-0000-0000-0000-000000000000", false)]
    public void AGuid_IsValidUnlessItIsTheEmptyOne(string? value, bool valid)
        => Assert.Equal(valid, new NotDefaultAttribute().IsValid(value is null ? null : Guid.Parse(value)));

    [Fact]
    public void ANumber_AndAnEnum_AreValidUnlessTheyAreTheDefault()
    {
        NotDefaultAttribute attribute = new();

        Assert.False(attribute.IsValid(0));
        Assert.True(attribute.IsValid(3));
        Assert.False(attribute.IsValid(DayOfWeek.Sunday));
        Assert.True(attribute.IsValid(DayOfWeek.Monday));
    }
}
