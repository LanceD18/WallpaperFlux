using System;
using System.Collections.Generic;
using System.Text;

namespace WallpaperFlux.Core.External
{
    // Registered with the Mvx IoCProvider
    public interface IExternalBitmapImage
    {
        void InitCompressedSource(string imagePath, int width, int height);
    }
}
