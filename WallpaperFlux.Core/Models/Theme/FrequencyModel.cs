using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows.Input;
using MvvmCross.Commands;
using WallpaperFlux.Core.Tools;

namespace WallpaperFlux.Core.Models.Theme
{
    public class FrequencyModel
    {
        private FrequencyCalculator _parentCalculator;
        
        private double _relativeFrequencyStatic;

        public double RelativeFrequencyStatic
        {
            get { return _relativeFrequencyStatic; }
            set
            {
                Debug.WriteLine("Send this new frequency back to FrequencyCalc and update it, make sure to divide by 100");
                _relativeFrequencyStatic = value;
            }
        }

        public IMvxCommand ConfirmFrequencyCommand { get; set; }

        public FrequencyModel(FrequencyCalculator parentCalculator)
        {
            _parentCalculator = parentCalculator;

            RelativeFrequencyStatic = _parentCalculator.GetRelativeFrequency(ImageType.Static) * 100;

            ConfirmFrequencyCommand = new MvxCommand(ConfirmFrequency);
        }

        // TODO make this work since you don't want to have to rely on losing focus for changes
        public void ConfirmFrequency()
        {
            Debug.WriteLine("yes");
            RelativeFrequencyStatic = 50;
            Debug.WriteLine("Test: " + RelativeFrequencyStatic);
        }
    }
}
