using HsnSoft.Base;
using HsnSoft.Base.Validation.Localization;
using JetBrains.Annotations;
using Microsoft.Extensions.Localization;

namespace Hhs.Shared.Localization;

public static class LocalizedModelValidator
{
    private static IStringLocalizer s_localizer;

    public static void Configure(IStringLocalizerFactory localizerFactory, List<Type> resourceTypes)
        => s_localizer = localizerFactory.CreateMultiple(resourceTypes);

    public static T NotNull<T>(T value, [NotNull] string parameterName)
    {
        if (value != null) return value;

        string throwMessage = s_localizer != null ? s_localizer[ValidationResourceKeys.Required] : string.Empty;
        if (string.IsNullOrWhiteSpace(throwMessage) || throwMessage.Equals(ValidationResourceKeys.Required))
        {
            throwMessage = $"{parameterName} can not be null";
        }

        var ex = new DomainException(throwMessage);
        ex.WithData(s_localizer?[ValidationResourceKeys.ErrorReference], s_localizer?[parameterName].ToString());
        throw ex;
    }

    public static T NotDefaultOrNull<T>(T? value, [NotNull] string parameterName) where T : struct
    {
        if (value == null)
        {
            string throwMessage = s_localizer != null ? s_localizer[ValidationResourceKeys.Required] : string.Empty;
            if (string.IsNullOrWhiteSpace(throwMessage) || throwMessage.Equals(ValidationResourceKeys.Required))
            {
                throwMessage = $"{parameterName} is null";
            }

            var ex = new DomainException(throwMessage);
            ex.WithData(s_localizer?[ValidationResourceKeys.ErrorReference], s_localizer?[parameterName].ToString());
            throw ex;
        }

        if (value.Value.Equals(default(T)))
        {
            string throwMessage = s_localizer != null ? s_localizer[ValidationResourceKeys.IsNotEmpty] : string.Empty;
            if (string.IsNullOrWhiteSpace(throwMessage) || throwMessage.Equals(ValidationResourceKeys.IsNotEmpty))
            {
                throwMessage = $"{parameterName} has a default value";
            }

            var ex = new DomainException(throwMessage);
            ex.WithData(s_localizer?[ValidationResourceKeys.ErrorReference], s_localizer?[parameterName].ToString());
            throw ex;
        }

        return value.Value;
    }

    public static ICollection<T> NotNullOrEmpty<T>(ICollection<T> value, [NotNull] string parameterName)
    {
        if (value is { Count: > 0 }) return value;

        string throwMessage = s_localizer != null ? s_localizer[ValidationResourceKeys.IsNotEmpty] : string.Empty;
        if (string.IsNullOrWhiteSpace(throwMessage) || throwMessage.Equals(ValidationResourceKeys.IsNotEmpty))
        {
            throwMessage = $"{parameterName} can not be null or empty";
        }

        var ex = new DomainException(throwMessage);
        ex.WithData(s_localizer?[ValidationResourceKeys.ErrorReference], s_localizer?[parameterName].ToString());
        throw ex;
    }

    public static string NotNull(string value, [NotNull] string parameterName, int maxLength = int.MaxValue, int minLength = 0)
    {
        if (value == null)
        {
            string throwMessage = s_localizer != null ? s_localizer[ValidationResourceKeys.Required] : string.Empty;
            if (string.IsNullOrWhiteSpace(throwMessage) || throwMessage.Equals(ValidationResourceKeys.Required))
            {
                throwMessage = $"{parameterName} can not be null";
            }

            var ex = new DomainException(throwMessage);
            ex.WithData(s_localizer?[ValidationResourceKeys.ErrorReference], s_localizer?[parameterName].ToString());
            throw ex;
        }

        if (value.Length > maxLength)
        {
            string throwMessage = s_localizer != null ? s_localizer[ValidationResourceKeys.MaxLength, maxLength] : string.Empty;
            if (string.IsNullOrWhiteSpace(throwMessage) || throwMessage.Equals(ValidationResourceKeys.MaxLength))
            {
                throwMessage = $"{parameterName} length must be equal to or lower than {maxLength}";
            }

            var ex = new DomainException(throwMessage);
            ex.WithData(s_localizer?[ValidationResourceKeys.ErrorReference], s_localizer?[parameterName].ToString());
            throw ex;
        }

        if (minLength > 0 && value.Length < minLength)
        {
            string throwMessage = s_localizer != null ? s_localizer[ValidationResourceKeys.MinLength, minLength] : string.Empty;
            if (string.IsNullOrWhiteSpace(throwMessage) || throwMessage.Equals(ValidationResourceKeys.MinLength))
            {
                throwMessage = $"{parameterName} length must be equal to or bigger than {minLength}";
            }

            var ex = new DomainException(throwMessage);
            ex.WithData(s_localizer?[ValidationResourceKeys.ErrorReference], s_localizer?[parameterName].ToString());
            throw ex;
        }

        return value;
    }

    public static string NotNullOrWhiteSpace(string value, [NotNull] string parameterName, int maxLength = int.MaxValue, int minLength = 0)
    {
        if (value.IsNullOrWhiteSpace())
        {
            string throwMessage = s_localizer != null ? s_localizer[ValidationResourceKeys.IsNotEmpty] : string.Empty;
            if (string.IsNullOrWhiteSpace(throwMessage) || throwMessage.Equals(ValidationResourceKeys.IsNotEmpty))
            {
                throwMessage = $"{parameterName} can not be null, empty or white space";
            }

            var ex = new DomainException(throwMessage);
            ex.WithData(s_localizer?[ValidationResourceKeys.ErrorReference], s_localizer?[parameterName].ToString());
            throw ex;
        }

        if (value.Length > maxLength)
        {
            string throwMessage = s_localizer != null ? s_localizer[ValidationResourceKeys.MaxLength, maxLength] : string.Empty;
            if (string.IsNullOrWhiteSpace(throwMessage) || throwMessage.Equals(ValidationResourceKeys.MaxLength))
            {
                throwMessage = $"{parameterName} length must be equal to or lower than {maxLength}";
            }

            var ex = new DomainException(throwMessage);
            ex.WithData(s_localizer?[ValidationResourceKeys.ErrorReference], s_localizer?[parameterName].ToString());
            throw ex;
        }

        if (minLength > 0 && value.Length < minLength)
        {
            string throwMessage = s_localizer != null ? s_localizer[ValidationResourceKeys.MinLength, minLength] : string.Empty;
            if (string.IsNullOrWhiteSpace(throwMessage) || throwMessage.Equals(ValidationResourceKeys.MinLength))
            {
                throwMessage = $"{parameterName} length must be equal to or bigger than {minLength}";
            }

            var ex = new DomainException(throwMessage);
            ex.WithData(s_localizer?[ValidationResourceKeys.ErrorReference], s_localizer?[parameterName].ToString());
            throw ex;
        }

        return value;
    }

    public static string NotNullOrEmpty(string value, [NotNull] string parameterName, int maxLength = int.MaxValue, int minLength = 0)
    {
        if (value.IsNullOrEmpty())
        {
            string throwMessage = s_localizer != null ? s_localizer[ValidationResourceKeys.IsNotEmpty] : string.Empty;
            if (string.IsNullOrWhiteSpace(throwMessage) || throwMessage.Equals(ValidationResourceKeys.IsNotEmpty))
            {
                throwMessage = $"{parameterName} can not be null or empty";
            }

            var ex = new DomainException(throwMessage);
            ex.WithData(s_localizer?[ValidationResourceKeys.ErrorReference], s_localizer?[parameterName].ToString());
            throw ex;
        }

        if (value.Length > maxLength)
        {
            string throwMessage = s_localizer != null ? s_localizer[ValidationResourceKeys.MaxLength, maxLength] : string.Empty;
            if (string.IsNullOrWhiteSpace(throwMessage) || throwMessage.Equals(ValidationResourceKeys.MaxLength))
            {
                throwMessage = $"{parameterName} length must be equal to or lower than {maxLength}";
            }

            var ex = new DomainException(throwMessage);
            ex.WithData(s_localizer?[ValidationResourceKeys.ErrorReference], s_localizer?[parameterName].ToString());
            throw ex;
        }

        if (minLength > 0 && value.Length < minLength)
        {
            string throwMessage = s_localizer != null ? s_localizer[ValidationResourceKeys.MinLength, minLength] : string.Empty;
            if (string.IsNullOrWhiteSpace(throwMessage) || throwMessage.Equals(ValidationResourceKeys.MinLength))
            {
                throwMessage = $"{parameterName} length must be equal to or bigger than {minLength}";
            }

            var ex = new DomainException(throwMessage);
            ex.WithData(s_localizer?[ValidationResourceKeys.ErrorReference], s_localizer?[parameterName].ToString());
            throw ex;
        }

        return value;
    }

    public static string Length([CanBeNull] string value, [NotNull] string parameterName, int maxLength, int minLength = 0)
    {
        if (minLength > 0)
        {
            if (string.IsNullOrEmpty(value))
            {
                string throwMessage = s_localizer != null ? s_localizer[ValidationResourceKeys.IsNotEmpty] : string.Empty;
                if (string.IsNullOrWhiteSpace(throwMessage) || throwMessage.Equals(ValidationResourceKeys.IsNotEmpty))
                {
                    throwMessage = $"{parameterName} can not be null or empty";
                }

                var ex = new DomainException(throwMessage);
                ex.WithData(s_localizer?[ValidationResourceKeys.ErrorReference], s_localizer?[parameterName].ToString());
                throw ex;
            }

            if (value.Length < minLength)
            {
                string throwMessage = s_localizer != null ? s_localizer[ValidationResourceKeys.MinLength, minLength] : string.Empty;
                if (string.IsNullOrWhiteSpace(throwMessage) || throwMessage.Equals(ValidationResourceKeys.MinLength))
                {
                    throwMessage = $"{parameterName} length must be lower than {maxLength} and bigger than {minLength}";
                }

                var ex = new DomainException(throwMessage);
                ex.WithData(s_localizer?[ValidationResourceKeys.ErrorReference], s_localizer?[parameterName].ToString());
                throw ex;
            }
        }

        if (value != null && value.Length > maxLength)
        {
            string throwMessage = s_localizer != null ? s_localizer[ValidationResourceKeys.MaxLength, maxLength] : string.Empty;
            if (string.IsNullOrWhiteSpace(throwMessage) || throwMessage.Equals(ValidationResourceKeys.MaxLength))
            {
                throwMessage = $"{parameterName} length must be equal to or lower than {maxLength}";
            }

            var ex = new DomainException(throwMessage);
            ex.WithData(s_localizer?[ValidationResourceKeys.ErrorReference], s_localizer?[parameterName].ToString());
            throw ex;
        }

        return value;
    }

    public static short Range(short value, [NotNull] string parameterName, short minimumValue, short maximumValue = short.MaxValue)
    {
        if (value >= minimumValue && value <= maximumValue) return value;

        string throwMessage = s_localizer != null ? s_localizer[ValidationResourceKeys.Range, minimumValue, maximumValue] : string.Empty;
        if (string.IsNullOrWhiteSpace(throwMessage) || throwMessage.Equals(ValidationResourceKeys.Range))
        {
            throwMessage = $"{parameterName} length must be lower than {maximumValue} and bigger than {minimumValue}";
        }

        var ex = new DomainException(throwMessage);
        ex.WithData(s_localizer?[ValidationResourceKeys.ErrorReference], s_localizer?[parameterName].ToString());
        throw ex;
    }

    public static int Range(int value, [NotNull] string parameterName, int minimumValue, int maximumValue = int.MaxValue)
    {
        if (value >= minimumValue && value <= maximumValue) return value;

        string throwMessage = s_localizer != null ? s_localizer[ValidationResourceKeys.Range, minimumValue, maximumValue] : string.Empty;
        if (string.IsNullOrWhiteSpace(throwMessage) || throwMessage.Equals(ValidationResourceKeys.Range))
        {
            throwMessage = $"{parameterName} length must be lower than {maximumValue} and bigger than {minimumValue}";
        }

        var ex = new DomainException(throwMessage);
        ex.WithData(s_localizer?[ValidationResourceKeys.ErrorReference], s_localizer?[parameterName].ToString());
        throw ex;
    }

    public static long Range(long value, [NotNull] string parameterName, long minimumValue, long maximumValue = long.MaxValue)
    {
        if (value >= minimumValue && value <= maximumValue) return value;

        string throwMessage = s_localizer != null ? s_localizer[ValidationResourceKeys.Range, minimumValue, maximumValue] : string.Empty;
        if (string.IsNullOrWhiteSpace(throwMessage) || throwMessage.Equals(ValidationResourceKeys.Range))
        {
            throwMessage = $"{parameterName} length must be lower than {maximumValue} and bigger than {minimumValue}";
        }

        var ex = new DomainException(throwMessage);
        ex.WithData(s_localizer?[ValidationResourceKeys.ErrorReference], s_localizer?[parameterName].ToString());
        throw ex;
    }

    public static float Range(float value, [NotNull] string parameterName, float minimumValue, float maximumValue = float.MaxValue)
    {
        if (value >= minimumValue && value <= maximumValue) return value;

        string throwMessage = s_localizer != null ? s_localizer[ValidationResourceKeys.Range, minimumValue, maximumValue] : string.Empty;
        if (string.IsNullOrWhiteSpace(throwMessage) || throwMessage.Equals(ValidationResourceKeys.Range))
        {
            throwMessage = $"{parameterName} length must be lower than {maximumValue} and bigger than {minimumValue}";
        }

        var ex = new DomainException(throwMessage);
        ex.WithData(s_localizer?[ValidationResourceKeys.ErrorReference], s_localizer?[parameterName].ToString());
        throw ex;
    }

    public static double Range(double value, [NotNull] string parameterName, double minimumValue, double maximumValue = double.MaxValue)
    {
        if (value >= minimumValue && value <= maximumValue) return value;

        string throwMessage = s_localizer != null ? s_localizer[ValidationResourceKeys.Range, minimumValue, maximumValue] : string.Empty;
        if (string.IsNullOrWhiteSpace(throwMessage) || throwMessage.Equals(ValidationResourceKeys.Range))
        {
            throwMessage = $"{parameterName} length must be lower than {maximumValue} and bigger than {minimumValue}";
        }

        var ex = new DomainException(throwMessage);
        ex.WithData(s_localizer?[ValidationResourceKeys.ErrorReference], s_localizer?[parameterName].ToString());
        throw ex;
    }

    public static decimal Range(decimal value, [NotNull] string parameterName, decimal minimumValue, decimal maximumValue = decimal.MaxValue)
    {
        if (value >= minimumValue && value <= maximumValue) return value;

        string throwMessage = s_localizer != null ? s_localizer[ValidationResourceKeys.Range, minimumValue, maximumValue] : string.Empty;
        if (string.IsNullOrWhiteSpace(throwMessage) || throwMessage.Equals(ValidationResourceKeys.Range))
        {
            throwMessage = $"{parameterName} length must be lower than {maximumValue} and bigger than {minimumValue}";
        }

        var ex = new DomainException(throwMessage);
        ex.WithData(s_localizer?[ValidationResourceKeys.ErrorReference], s_localizer?[parameterName].ToString());
        throw ex;
    }
}