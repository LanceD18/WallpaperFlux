using System.Diagnostics;
using WallpaperFlux.Core;
using WallpaperFlux.Core.IoC;
using WallpaperFlux.Core.Models;

namespace WallpaperFlux.WPF.IoC
{
    public class ExternalWallpaperHandler : IExternalWallpaperHandler
    {
        public void OnWallpaperChange(int index, ImageModel image, bool forceChange)
        {
            if (MainWindow.Instance.Wallpapers != null)
            {
                MainWindow.Instance.Wallpapers[index].OnWallpaperChange(image, forceChange);
            }
        }

        public void OnWallpaperStyleChange(int index, WallpaperStyle style)
        {
            if (MainWindow.Instance.Wallpapers != null)
            {
                MainWindow.Instance.Wallpapers[index].OnWallpaperStyleChange(style);
            }
        }
    }
}
