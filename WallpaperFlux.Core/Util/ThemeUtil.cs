using System;
using System.Collections.Generic;
using System.Text;
using WallpaperFlux.Core.Controllers;
using WallpaperFlux.Core.Models.Theme;
using WallpaperFlux.Core.Tools;

namespace WallpaperFlux.Core.Util
{
    // TODO Consider minimizing the use of this or remove it entirely, might be better to move all this information onto a different assembly
    // TODO Consider using event calls to handle accessing the data within models. Keep in mind that this could result in an excessive amount of events being written
    public static class ThemeUtil
    {
        public static ThemeModel Theme;

        public static ThemeSettings ThemeSettings => Theme.Settings.ThemeSettings;
        public static FrequencyCalculator FrequencyCalculator => Theme.Settings.ThemeSettings.FrequencyCalc;
        public static VideoSettings VideoSettings => Theme.Settings.ThemeSettings.VideoSettings;

        public static RankController RankController => Theme.RankController;

        static ThemeUtil()
        {
            Theme = new ThemeModel();
            Theme.Init(10); // default max rank of 10
        }

        public static void ReconstructTheme(int maxRank)
        {
            Theme = new ThemeModel();
            Theme.Init(maxRank);
        }

        public static void ReconstructTheme(SettingsModel settings)
        {
            Theme = new ThemeModel();
            Theme.Init(settings.ThemeSettings.MaxRank);
            Theme.Settings = settings;
            Theme.Settings.UpdateDependents();
        }
    }
}
