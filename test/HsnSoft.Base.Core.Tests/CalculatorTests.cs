namespace HsnSoft.Base.Core.Tests;

public class CalculatorTests
{
    [Theory]
    [InlineData(1, 2, 3)]
    [InlineData(-1, -2, -3)]
    [InlineData(-5, 5, 0)]
    [InlineData(0.1, 0.2, 0.3)] // exact, unlike double
    public void Add_ReturnsSum(decimal left, decimal right, decimal expected)
        => Assert.Equal(expected, Calculator.Add(left, right));

    [Theory]
    [InlineData(5, 3, 2)]
    [InlineData(3, 5, -2)]
    [InlineData(0.3, 0.1, 0.2)]
    public void Subtract_ReturnsDifference(decimal left, decimal right, decimal expected)
        => Assert.Equal(expected, Calculator.Subtract(left, right));

    [Theory]
    [InlineData(3, 4, 12)]
    [InlineData(-3, 4, -12)]
    [InlineData(-3, -4, 12)]
    [InlineData(7, 0, 0)]
    [InlineData(0.5, 0.5, 0.25)]
    public void Multiply_ReturnsProduct(decimal left, decimal right, decimal expected)
        => Assert.Equal(expected, Calculator.Multiply(left, right));

    [Theory]
    [InlineData(10, 2, 5)]
    [InlineData(1, 4, 0.25)]
    [InlineData(-9, 3, -3)]
    [InlineData(0, 5, 0)]
    public void Divide_ReturnsQuotient(decimal dividend, decimal divisor, decimal expected)
        => Assert.Equal(expected, Calculator.Divide(dividend, divisor));

    [Theory]
    [InlineData(1)]
    [InlineData(0)]
    [InlineData(-7)]
    public void Divide_ByZero_Throws(decimal dividend)
        => Assert.Throws<DivideByZeroException>(() => Calculator.Divide(dividend, 0m));

    [Fact]
    public void Add_OverflowingResult_Throws()
        => Assert.Throws<OverflowException>(() => Calculator.Add(decimal.MaxValue, 1m));

    [Fact]
    public void Subtract_OverflowingResult_Throws()
        => Assert.Throws<OverflowException>(() => Calculator.Subtract(decimal.MinValue, 1m));

    [Fact]
    public void Multiply_OverflowingResult_Throws()
        => Assert.Throws<OverflowException>(() => Calculator.Multiply(decimal.MaxValue, 2m));
}
