using System;
using System.Collections.Generic;
using System.Text;
using WallpaperFlux.Core.Models;

namespace WallpaperFlux.Core.IoC
{
    public interface IExternalWallpaperHandler
    {
        void OnWallpaperChange(int index, ImageModel image);

        void OnWallpaperStyleChange(int index, WallpaperStyle style);
    }
}
