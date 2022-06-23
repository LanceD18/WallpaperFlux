using System;
using System.Collections.Generic;
using System.Text;

namespace WallpaperFlux.Core.IoC
{
    public interface IExternalWallpaperHandler
    {
        void OnWallpaperChange(int index, string path);

        void OnWallpaperStyleChange(int index, WallpaperStyle style);
    }
}
