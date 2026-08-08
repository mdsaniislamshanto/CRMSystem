using System.ComponentModel.DataAnnotations;

namespace CRMSystem.Validators
{
    public class FutureDateAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(
            object? value,
            ValidationContext validationContext)
        {
            if (value == null)
            {
                return ValidationResult.Success;
            }

            if (value is DateTime date)
            {
                if (date.Date < DateTime.Today)
                {
                    return new ValidationResult(
                        "Next follow-up date cannot be in the past.");
                }
            }

            return ValidationResult.Success;
        }
    }
}