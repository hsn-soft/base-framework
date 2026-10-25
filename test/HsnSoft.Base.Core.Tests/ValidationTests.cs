using System.ComponentModel.DataAnnotations;

namespace HsnSoft.Base.Core.Tests;

public class ValidationTests
{
    [Fact]
    public void TheCollector_KeepsEveryError_AndThrowsOnlyWhenThereIsOne()
    {
        ValidationErrors errors = new();
        errors.ThrowIfAny();
        Assert.False(errors.Any);

        errors.Add("Name", "Name is required.", "Validation:Required", "Name").AddIf(false, "Skip", "never").AddIf(true, "Level", "Level is wrong.");

        Assert.Equal(["Name", "Level"], errors.Items.Select(error => error.Field));
        ValidationFailedException failed = Assert.Throws<ValidationFailedException>(errors.ThrowIfAny);
        Assert.Equal("Validation failed: Name: Name is required.; Level: Level is wrong.", failed.Message);
        Assert.Equal(2, failed.Errors.Count);
    }

    [Fact]
    public void TheCollector_RefusesAnEmptyMessage()
    {
        Assert.Throws<ArgumentException>(() => new ValidationErrors().Add("Name", " "));
        Assert.Throws<ArgumentNullException>(() => new ValidationErrors().Add(null!, "x"));
    }

    [Fact]
    public void TheException_WithNoErrors_HasThePlainMessage()
    {
        Assert.Equal("Validation failed.", new ValidationFailedException([]).Message);
        Assert.Throws<ArgumentNullException>(() => new ValidationFailedException(null!));
    }

    [Fact]
    public void ARule_NamesItsText_AndTheKeyAndTheArgumentsComeBack()
    {
        ValidationResult result = ValidationRule.Failed("Email", "email_taken", "a@b.co", 3);

        Assert.Equal(["Email"], result.MemberNames);
        Assert.True(ValidationRule.TryDecode(result.ErrorMessage, out string key, out string[] args));
        Assert.Equal("email_taken", key);
        Assert.Equal(["a@b.co", "3"], args);
    }

    [Theory]
    [InlineData("a plain message")]
    [InlineData("")]
    [InlineData(null)]
    public void APlainMessage_IsNotARule(string? message)
    {
        Assert.False(ValidationRule.TryDecode(message, out string key, out string[] args));
        Assert.Equal(string.Empty, key);
        Assert.Empty(args);
    }

    [Fact]
    public void TheShortcuts_UseTheTextsOfTheFramework()
    {
        Assert.True(ValidationRule.TryDecode(ValidationRule.Required("Name").ErrorMessage, out string key, out string[] args));
        Assert.Equal("Validation:Required", key);
        Assert.Equal(["Name"], args);
        Assert.True(ValidationRule.TryDecode(ValidationRule.TooLong("Name", 5).ErrorMessage, out key, out args));
        Assert.Equal("Validation:MaxLength", key);
        Assert.Equal(["Name", "5"], args);
        Assert.True(ValidationRule.TryDecode(ValidationRule.Invalid("Name").ErrorMessage, out key, out args));
        Assert.Equal("Validation:InvalidValue", key);
        Assert.Throws<ArgumentException>(() => ValidationRule.Failed(" ", "k"));
        Assert.Throws<ArgumentException>(() => ValidationRule.Failed("m", " "));
    }
}
