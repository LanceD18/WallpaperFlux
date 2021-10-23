﻿using System.Collections.Generic;
using System.Diagnostics;
using MvvmCross.ViewModels;
using WallpaperFlux.Core.Tools;
using WallpaperFlux.Core.Util;

namespace WallpaperFlux.Core.Models.Theme
{
    // Theme-Wide Settings
    public class ThemeSettings
    {
        // Randomization Modifications
        public bool LargerImagesOnLargerDisplays;
        public bool HigherRankedImagesOnLargerDisplays;

        // this just means that you can find these images in the image selector or other tools, they won't be a possible choice for a wallpaper however
        public bool EnableDetectionOfInactiveImages;

        // Ranking Settings
        public int MaxRank { get; set; }
        public bool WeightedRanks;
        public bool WeightedFrequency;
        public bool AllowTagBasedRenamingForMovedImages;

        // Image Type Settings
        public bool ExcludeRenamingStatic;
        public bool ExcludeRenamingGif;
        public bool ExcludeRenamingVideo;
        public FrequencyCalculator FrequencyCalc = new FrequencyCalculator();

        // Video Settings
        public VideoSettings VideoSettings;
    }

    public class VideoSettings
    {
        public bool MuteIfAudioPlaying;
        public bool MuteIfApplicationMaximized;
        public bool MuteIfApplicationFocused;
        public int MinimumVideoLoops;
        public float MaximumVideoTime;
    }

    // TODO Set me up later, this will be used to diverge displays into independent sets of options
    // Display-Bound Settings
    public class DisplaySettings
    {
        //? This will only modify settings that can independently alter a monitor

        // TODO These duplicate settings are intended as they are not yet implemented. Will be setup eventually, replacing the duplicates ub ThemeSettings

        // General Settings
        public bool WeightedRanks;
        public FrequencyCalculator FrequencyCalc = new FrequencyCalculator();
    }

    public class SettingsModel : MvxNotifyPropertyChanged
    {
        // Theme Options
        public ThemeSettings ThemeSettings { get; set; } = new ThemeSettings();

        // Monitor Options
        //! DO NOT USE YET ; This still needs to be set up, will re-purpose MOST of ThemeSettings so that Monitors can have their own options
        public DisplaySettings[] DisplaySettings { get; set; }

        // Global Options
        public string DefaultTheme { get; set; }
        public bool EnableDefaultThemeHotkey { get; set; }

        public SettingsModel(int maxRank)
        {
            ThemeSettings.MaxRank = maxRank;
        }

        #region Commands
        public void UpdateMaxRank()
        {
            DataUtil.Theme.RankController.SetMaxRank(ThemeSettings.MaxRank);
        }
        #endregion
    }
}
