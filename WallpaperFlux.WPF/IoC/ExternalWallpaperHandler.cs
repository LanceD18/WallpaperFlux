using System.Diagnostics;
using WallpaperFlux.Core;
using WallpaperFlux.Core.IoC;
using WallpaperFlux.Core.Models;

namespace WallpaperFlux.WPF.IoC
{
    public class ExternalWallpaperHandler : IExternalWallpaperHandler
    {
        public void OnWallpaperChange(int index, ImageModel image)
        {
            if (MainWindow.Instance.Wallpapers != null)
            {
                MainWindow.Instance.Wallpapers[index].OnWallpaperChange(image);
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
