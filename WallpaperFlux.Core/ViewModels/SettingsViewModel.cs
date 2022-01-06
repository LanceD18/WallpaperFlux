using System;
using System.Collections.Generic;
using System.Text;
using MvvmCross.ViewModels;
using WallpaperFlux.Core.Models.Theme;
using WallpaperFlux.Core.Util;

namespace WallpaperFlux.Core.ViewModels
{
    // TODO This window will not lock other windows this time around
    // TODO So the settings MUST have their changes applied either instantly or through a button
    public class SettingsViewModel : MvxViewModel
    {
        public static SettingsViewModel Instance;

        public SettingsModel Settings { get; set; } = DataUtil.Theme.Settings;
    }
}
