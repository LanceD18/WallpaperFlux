using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LanceTools;

namespace WallpaperFlux.WPF.Util
{
    public static class WallpaperWindowUtil
    {
        //? doesn't necessarily apply to all compatibilities, just the most viable ones
        public static bool IsVideoVlcCompatible(string videoExtension) => videoExtension == ".mp4" || videoExtension == ".avi";

        public static int GetWallpaperIndex(WallpaperWindow wallpaper)
        {
            return MainWindow.Instance.Wallpapers.IndexOf(wallpaper);
        }
    }
}
