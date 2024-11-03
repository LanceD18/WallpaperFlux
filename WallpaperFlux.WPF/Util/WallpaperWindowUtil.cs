using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LanceTools;

namespace WallpaperFlux.WPF.Util
{
    public static class WallpaperWindowUtil
    {
        public static int GetWallpaperIndex(WallpaperWindow wallpaper)
        {
            return MainWindow.Instance.Wallpapers.IndexOf(wallpaper);
        }
    }
}
