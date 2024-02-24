using System;
using System.Collections.Generic;
using System.Text;
using WallpaperFlux.Core.Models;

namespace WallpaperFlux.Core.IoC
{
    public interface IExternalWallpaperHandler
    {
        void OnWallpaperChange(int index, BaseImageModel image, bool forceChange);

        void OnWallpaperStyleChange(int index, WallpaperStyle style);

        string GetWallpaperPath(int index);

        void UpdateVolume(int index);

        void Mute(int index);

        void Unmute(int index);

        void UpdateSize();

        void DisableMpv();
    }
}
