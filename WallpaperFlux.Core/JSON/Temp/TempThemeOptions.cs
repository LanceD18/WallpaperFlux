using System;
using System.Collections.Generic;
using System.Text;

namespace WallpaperFlux.Core.JSON.Temp
{
    public struct TempThemeOptions
    {
        // General Options
        public bool LargerImagesOnLargerDisplays;
        public bool HigherRankedImagesOnLargerDisplays;
        public bool EnableDetectionOfInactiveImages;
        public bool WeightedRanks;
        public bool WeightedFrequency;
        public bool AllowTagBasedRenamingForMovedImages;

        public bool ExcludeRenamingStatic;
        public bool ExcludeRenamingGif;
        public bool ExcludeRenamingVideo;

        public Dictionary<ImageType, double> RelativeFrequency;
        public Dictionary<ImageType, double> ExactFrequency;

        public TempVideoOptions VideoOptions;
    }

    public struct TempVideoOptions
    {
        public bool MuteIfAudioPlaying;
        public bool MuteIfApplicationMaximized;
        public bool MuteIfApplicationFocused;
        public int MinimumVideoLoops;
        public float MaximumVideoTime;
    }
}
