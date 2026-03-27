using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace FinanceTracker.Api.Infrastructure;

public sealed class NotBlankAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is string text && !string.IsNullOrWhiteSpace(text))
        {
            return ValidationResult.Success;
        }

        return new ValidationResult(ErrorMessage ?? "must not be blank");
    }
}

public sealed class MinValueAttribute(string minimum) : ValidationAttribute
{
    private readonly decimal _minimum = decimal.Parse(minimum, CultureInfo.InvariantCulture);

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null)
        {
            return ValidationResult.Success;
        }

        var numericValue = Convert.ToDecimal(value, CultureInfo.InvariantCulture);
        if (numericValue >= _minimum)
        {
            return ValidationResult.Success;
        }

        return new ValidationResult(ErrorMessage ?? $"must be greater than or equal to {_minimum}");
    }
}

public sealed class MaxValueAttribute(string maximum) : ValidationAttribute
{
    private readonly decimal _maximum = decimal.Parse(maximum, CultureInfo.InvariantCulture);

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null)
        {
            return ValidationResult.Success;
        }

        var numericValue = Convert.ToDecimal(value, CultureInfo.InvariantCulture);
        if (numericValue <= _maximum)
        {
            return ValidationResult.Success;
        }

        return new ValidationResult(ErrorMessage ?? $"must be less than or equal to {_maximum}");
    }
}
