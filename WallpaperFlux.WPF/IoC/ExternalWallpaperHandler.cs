using WallpaperFlux.Core;
using WallpaperFlux.Core.IoC;

namespace WallpaperFlux.WPF.IoC
{
    public class ExternalWallpaperHandler : IExternalWallpaperHandler
    {
        public void OnWallpaperChange(int index, string path)
        {
            if (MainWindow.Instance.Wallpapers != null)
            {
                MainWindow.Instance.Wallpapers[index].OnWallpaperChange(path);
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
