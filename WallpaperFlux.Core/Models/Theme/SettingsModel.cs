﻿using System.Collections.Generic;
using System.Diagnostics;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using Newtonsoft.Json;
using WallpaperFlux.Core.Tools;
using WallpaperFlux.Core.Util;

namespace WallpaperFlux.Core.Models.Theme
{
    // Theme-Wide Settings
    public class ThemeSettings : MvxNotifyPropertyChanged
    {
        // Randomization Modifications
        public bool LargerImagesOnLargerDisplays { get; set; }
        public bool HigherRankedImagesOnLargerDisplays { get; set; }

        // this just means that you can find these images in the image selector or other tools, they won't be a possible choice for a wallpaper however
        public bool EnableDetectionOfInactiveImages { get; set; }

        // Ranking Settings
        private int _maxRank;
        public int MaxRank
        {
            get => _maxRank;
            set => SetProperty(ref _maxRank, value);
        }

        public bool WeightedRanks { get; set; }
        public bool AllowTagBasedRenamingForMovedImages { get; set; }

        [JsonIgnore] public FrequencyCalculator FrequencyCalc { get; set; } = new FrequencyCalculator();
        public FrequencyModel FrequencyModel { get; set; } = new FrequencyModel();

        // Video Settings
        public VideoSettings VideoSettings { get; set; } = new VideoSettings();

        // Monitor Options
        //! DO NOT USE YET ; This still needs to be set up, will re-purpose MOST of ThemeSettings so that Monitors can have their own options
        [JsonIgnore] public DisplaySettings[] DisplaySettings { get; set; }
    }

    public class VideoSettings
    {
        public int MinimumVideoLoops { get; set; }
        public int MaximumVideoTime { get; set; }

        public int DefaultVideoVolume { get; set; } = 50;
        public bool MuteIfAudioPlaying { get; set; }
        public bool MuteIfApplicationFocused { get; set; }
        public bool MuteIfApplicationMaximized { get; set; }
    }

    // TODO Set me up later, this will be used to diverge displays into independent sets of options
    // Display-Bound Settings
    public class DisplaySettings
    {
        //? This will only modify settings that can independently alter a monitor

        // TODO These duplicate settings are intended as they are not yet implemented. Will be setup eventually, replacing the duplicates in ThemeSettings

        // General Settings
        public bool WeightedRanks;
        [JsonIgnore] public FrequencyCalculator FrequencyCalc = new FrequencyCalculator();
    }

    public class SettingsModel : MvxNotifyPropertyChanged
    {
        // Theme Settings
        public ThemeSettings ThemeSettings { get; set; } = new ThemeSettings();

        // Global Settings
        public string DefaultTheme { get; set; }
        public bool EnableDefaultThemeHotkey { get; set; }

        // ----- WPF -----
        // Commands
        [JsonIgnore] public IMvxCommand UpdateMaxRankCommand { get; set; }

        public SettingsModel(int maxRank)
        {
            ThemeSettings.MaxRank = maxRank;
            InitCommands();
        }

        public void InitCommands()
        {
            UpdateMaxRankCommand = new MvxCommand(UpdateMaxRank);
        }

        #region Commands
        //? Included Command at the end of this method name to avoid accidentally using this over SetMaxRank
        /// <summary>
        /// Sends the current max rank input to the SetMaxRank method of the RankController
        /// </summary>
        public void UpdateMaxRank()
        {
            if (MessageBoxUtil.GetPositiveInteger("Set Max Rank", "Enter a new max rank", out int maxRank, "Max Rank..."))
            {
                ThemeUtil.Theme.RankController.SetMaxRank(maxRank);
            }
        }
        #endregion
    }
}
