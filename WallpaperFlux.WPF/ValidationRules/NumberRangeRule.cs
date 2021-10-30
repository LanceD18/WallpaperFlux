using System;
using System.Globalization;
using System.Windows.Controls;

namespace WallpaperFlux.WPF.ValidationRules
{
    public class NumberRangeRule : ValidationRule
    {
        public int Min { get; set; }

        public int Max { get; set; }


        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            int num = 0;

            try
            {
                if ((value as string).Length > 0)
                {
                    num = int.Parse((value as string));
                }
            }
            catch (Exception e)
            {
                return new ValidationResult(false, $"Illegal characters or {e.Message}");
            }

            if (num < Min || num > Max) //? remember that this is still inclusive for the range Min---Max
            {
                if ((Max != int.MaxValue && Min != int.MinValue) || (Max == int.MaxValue && Min == int.MinValue))
                {
                    return new ValidationResult(false, $"Please enter a number within the range: {Min}--{Max}");
                }

                if (Max == int.MaxValue)
                {
                    return new ValidationResult(false, $"Please enter a number greater than {Min}");
                }

                if (Min == int.MinValue)
                {
                    return new ValidationResult(false, $"Please enter a number less than {Max}");
                }
            }

            return ValidationResult.ValidResult;
        }
    }
}
