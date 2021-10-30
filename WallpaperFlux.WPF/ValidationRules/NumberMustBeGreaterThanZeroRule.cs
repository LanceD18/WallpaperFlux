using System;
using System.Collections.Generic;
using System.Text;

namespace WallpaperFlux.WPF.ValidationRules
{
    public class NumberMustBeGreaterThanZeroRule : NumberRangeRule
    {
        public NumberMustBeGreaterThanZeroRule()
        {
            Min = 0;
            Max = int.MaxValue;
        }
    }
}
