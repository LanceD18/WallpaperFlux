using System.Collections.Generic;
using System.Diagnostics;
using MvvmCross.ViewModels;
using WallpaperFlux.Core.Tools;
using WallpaperFlux.Core.Util;

namespace WallpaperFlux.Core.Models.Theme
{
    // Theme-Wide Settings
    public class ThemeSettings : MvxNotifyPropertyChanged
    {
        // Randomization Modifications
        public bool LargerImagesOnLargerDisplays;
        public bool HigherRankedImagesOnLargerDisplays;

        // this just means that you can find these images in the image selector or other tools, they won't be a possible choice for a wallpaper however
        public bool EnableDetectionOfInactiveImages;

        // Ranking Settings
        private int _maxRank;
        public int MaxRank
        {
            get => _maxRank;
            set => SetProperty(ref _maxRank, value);
        }

        public bool WeightedRanks;
        public bool WeightedFrequency;
        public bool AllowTagBasedRenamingForMovedImages;

        // Image Type Settings
        public bool ExcludeRenamingStatic;
        public bool ExcludeRenamingGif;
        public bool ExcludeRenamingVideo;
        public FrequencyCalculator FrequencyCalc;
        public FrequencyModel FrequencyModel { get; set; }

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
        // Theme Settings
        public ThemeSettings ThemeSettings { get; set; } = new ThemeSettings();

        // Monitor Options
        //! DO NOT USE YET ; This still needs to be set up, will re-purpose MOST of ThemeSettings so that Monitors can have their own options
        public DisplaySettings[] DisplaySettings { get; set; }

        // Global Settings
        public string DefaultTheme { get; set; }
        public bool EnableDefaultThemeHotkey { get; set; }

        public SettingsModel(int maxRank)
        {
            ThemeSettings.FrequencyCalc = new FrequencyCalculator(); //? this must come before FrequencyModel
            ThemeSettings.FrequencyModel = new FrequencyModel(ThemeSettings.FrequencyCalc);
            ThemeSettings.MaxRank = maxRank;
        }

        #region Commands
        //? Included Command at the end of this method name to avoid accidentally using this over SetMaxRank
        /// <summary>
        /// Sends the current max rank input to the SetMaxRank method of the RankController
        /// </summary>
        public void UpdateMaxRankCommand()
        {
            if (MessageBoxUtil.GetPositiveInteger("Set Max Rank", "Enter a new max rank", out int maxRank, "Max Rank..."))
            {
                DataUtil.Theme.RankController.SetMaxRank(maxRank);
            }
        }
        #endregion
    }
}
