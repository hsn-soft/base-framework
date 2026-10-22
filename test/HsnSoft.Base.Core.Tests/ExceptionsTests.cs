namespace HsnSoft.Base.Core.Tests;

public class ExceptionsTests
{
    [Fact]
    public void NotFoundException_KeepsTheMessage() => Assert.Equal("missing", new NotFoundException("missing").Message);

    [Fact]
    public void BusinessRuleException_KeepsTheMessage() => Assert.Equal("rule", new BusinessRuleException("rule").Message);
}
