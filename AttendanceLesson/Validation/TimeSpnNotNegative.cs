using System.ComponentModel.DataAnnotations;

namespace AttendanceLesson.Validation;
public class TimeSpanNonNegativeAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is TimeSpan timeSpan)
        {
            if (timeSpan < TimeSpan.Zero)
            {
                return new ValidationResult(ErrorMessage ?? "Value must be not negative");
            }
        }
        return ValidationResult.Success;
    }
}