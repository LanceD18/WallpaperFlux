using System;
using System.Diagnostics;
using System.Windows.Interop;
using WallpaperFlux.Core;
using WallpaperFlux.Core.IoC;
using WallpaperFlux.Core.Models;
using WallpaperFlux.WPF.Util;

namespace WallpaperFlux.WPF.IoC
{
    public class ExternalWallpaperHandler : IExternalWallpaperHandler
    {
        public void OnWallpaperChange(int index, BaseImageModel image, bool forceChange)
        {
            MainWindow.Instance.Wallpapers?[index].OnWallpaperChange(image, forceChange);
        }

        public void OnWallpaperStyleChange(int index, WallpaperStyle style)
        {
            MainWindow.Instance.Wallpapers?[index].OnWallpaperStyleChange(style);
        }

        public string GetWallpaperPath(int index)
        {
            return MainWindow.Instance.Wallpapers?[index].ActiveImage.Path;
        }

        public void UpdateVolume(int index)
        {
            MainWindow.Instance.Wallpapers?[index].UpdateVolume();
        }

        public void Mute(int index)
        {
            MainWindow.Instance.Wallpapers?[index].Mute();
        }

        public void Unmute(int index)
        {
            MainWindow.Instance.Wallpapers?[index].Unmute();
        }

        public void UpdateSize()
        {
            foreach (WallpaperWindow wallpaper in MainWindow.Instance.Wallpapers)
            {
                wallpaper.UpdateSize();
            }
        }

        public void DisableMpv()
        {
            foreach (WallpaperWindow wallpaper in MainWindow.Instance.Wallpapers)
            {
                wallpaper.DisableMpv();
            }
        }
    }
}
