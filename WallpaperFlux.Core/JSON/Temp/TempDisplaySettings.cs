using System;
using System.Collections.Generic;
using System.Text;

namespace WallpaperFlux.Core.JSON.Temp
{
    public struct TempDisplaySettings
    {
        public int[] WallpaperIntervals;
        public WallpaperStyle[] WallpaperStyles;
        public bool Synced;

        public TempDisplaySettings(int[] wallpaperInterval, WallpaperStyle[] wallpaperStyle, bool synced)
        {
            this.WallpaperIntervals = wallpaperInterval;
            this.WallpaperStyles = wallpaperStyle;
            this.Synced = synced;
        }
    }
}
