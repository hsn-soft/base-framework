using FluentAssertions;

namespace HsnSoft.Base.Test.Unit.Repositories;

[Trait("category", "integration")]
public class IntegrationTests
{
    [Fact, Trait("priority", "high")]
    public void HighPriorityTest()
    {
        true.Should().BeTrue();
    }

    [Fact, Trait("priority", "low")]
    public void LowPriorityTest()
    {
        true.Should().BeTrue();
    }
}