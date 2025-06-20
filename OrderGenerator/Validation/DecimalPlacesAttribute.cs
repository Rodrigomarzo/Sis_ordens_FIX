using System;
using System.ComponentModel.DataAnnotations;

namespace OrderGenerator.Validation
{
    public class DecimalPlacesAttribute : ValidationAttribute
    {
        private readonly int _maxDecimalPlaces;

        public DecimalPlacesAttribute(int maxDecimalPlaces)
        {
            _maxDecimalPlaces = maxDecimalPlaces;
            ErrorMessage = $"O valor não pode ter mais de {maxDecimalPlaces} casas decimais.";
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is decimal decValue)
            {
                if (GetDecimalPlaces(decValue) > _maxDecimalPlaces)
                {
                    return new ValidationResult(ErrorMessage);
                }
            }

            return ValidationResult.Success;
        }

        private static int GetDecimalPlaces(decimal n)
        {
            //This is a common way to count decimal places.
            n = Math.Abs(n); 
            n -= (int)n;     
            var decimalPlaces = 0;
            while (n > 0)
            {
                decimalPlaces++;
                n *= 10;
                n -= (int)n;
            }
            return decimalPlaces;
        }
    }
} 