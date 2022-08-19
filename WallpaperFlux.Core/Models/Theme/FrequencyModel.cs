using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows.Input;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using WallpaperFlux.Core.Tools;
using WallpaperFlux.Core.Util;

namespace WallpaperFlux.Core.Models.Theme
{
    public class FrequencyModel : MvxNotifyPropertyChanged
    {
        private FrequencyCalculator _parentCalculator;

        // TODO Find a cleaner way to do this, maybe just give every TextBox an event to trigger on-change
        // TODO It would be better, however, if the 'event' was still connected to the variable somehow
        #region Value References

        //! Remember!
        //? The parent calculator holds the actual values
        //? This model is just used to transfer those values to the UI
        //! Remember!

        private double _relativeFrequencyStatic;
        public double RelativeFrequencyStatic
        {
            get => _relativeFrequencyStatic;
            set
            {
                SetProperty(ref _relativeFrequencyStatic, value);
                UpdateFrequency(ImageType.Static, FrequencyType.Relative, value);
            }
        }

        private double _relativeFrequencyGIF;
        public double RelativeFrequencyGIF
        {
            get => _relativeFrequencyGIF;
            set
            {
                SetProperty(ref _relativeFrequencyGIF, value);
                UpdateFrequency(ImageType.GIF, FrequencyType.Relative, value);
            }
        }

        private double _relativeFrequencyVideo;
        public double RelativeFrequencyVideo
        {
            get => _relativeFrequencyVideo;
            set
            {
                SetProperty(ref _relativeFrequencyVideo, value);
                UpdateFrequency(ImageType.Video, FrequencyType.Relative, value);
            }
        }

        private double _exactFrequencyStatic;
        public double ExactFrequencyStatic
        {
            get => _exactFrequencyStatic;
            set
            {
                SetProperty(ref _exactFrequencyStatic, value);
                UpdateFrequency(ImageType.Static, FrequencyType.Exact, value);
            }
        }

        private double _exactFrequencyGIF;
        public double ExactFrequencyGIF
        {
            get => _exactFrequencyGIF;
            set
            {
                SetProperty(ref _exactFrequencyGIF, value);
                UpdateFrequency(ImageType.GIF, FrequencyType.Exact, value);
            }
        }

        private double _exactFrequencyVideo;
        public double ExactFrequencyVideo
        {
            get => _exactFrequencyVideo;
            set
            {
                SetProperty(ref _exactFrequencyVideo, value);
                UpdateFrequency(ImageType.Video, FrequencyType.Exact, value);
            }
        }
        #endregion

        private bool _weightedFrequency;
        public bool WeightedFrequency
        {
            get => _weightedFrequency;
            set
            {
                SetProperty(ref _weightedFrequency, value); // must be called first before the below updates are handled

                DataUtil.Theme.RankController.UpdateImageTypeWeights();
            }
        }

        private bool _frequenciesUpdated = true; // assume that frequencies are updated by default

        public IMvxCommand ConfirmFrequencyCommand { get; set; }

        public FrequencyModel(FrequencyCalculator parentCalculator)
        {
            _parentCalculator = parentCalculator;
        }

        // Without this the UI won't represent the default FrequencyModel settings on launch
        public void Init() //x //? should always be done AFTER the Init() in FrequencyCalculator
        {
            // initialize frequency values
            UpdateModelFrequency();
        }

        public void UpdateFrequency(ImageType imageType, FrequencyType frequencyType, double value)
        {
            // this boolean accounts for the fact that this method will be called again when all of the below statements re-run the set method (when updating the UI)
            if (_frequenciesUpdated)
            {
                _parentCalculator.UpdateFrequency(imageType, frequencyType, value / 100); // the visual value is 100 times larger than the actual value which goes from 0-1
                UpdateModelFrequency(); // updates the UI to the potentially adjusted frequency

                //? The below is now handled by the FrequencyCalculator since every time the frequencies are verified this should be called
                //xUpdateModelFrequency(); // readjust value to new frequency
            }
        }

        //? The parent calculator holds the actual values
        //? This model is just used to transfer those values to the UI
        public void UpdateModelFrequency()
        {
            //! Updating the private variable instead would not update the UI without calling RaisePropertyChanged()
            if (!_frequenciesUpdated) return;  //? this boolean accounts for the fact that this method will be called again when all of the below statements re-run the set method
            _frequenciesUpdated = false;

            RelativeFrequencyStatic = _parentCalculator.GetRelativeFrequency(ImageType.Static) * 100;
            RelativeFrequencyGIF = _parentCalculator.GetRelativeFrequency(ImageType.GIF) * 100;
            RelativeFrequencyVideo = _parentCalculator.GetRelativeFrequency(ImageType.Video) * 100;
            ExactFrequencyStatic = _parentCalculator.GetExactFrequency(ImageType.Static) * 100;
            ExactFrequencyGIF = _parentCalculator.GetExactFrequency(ImageType.GIF) * 100;
            ExactFrequencyVideo = _parentCalculator.GetExactFrequency(ImageType.Video) * 100;

            Debug.WriteLine("Raising all properties changed for the FrequencyModel"); //? Not sure how severe the impact of this is so keep the debug statement for now
            RaiseAllPropertiesChanged();

            _frequenciesUpdated = true;
        }
    }
}
