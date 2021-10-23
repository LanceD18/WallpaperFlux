using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace WallpaperFlux.Core.External
{
    // Registered with the Mvx IoCProvider
    public interface IExternalImage
    {
        bool SetImage(string imagePath);

        Size GetSize();

        object GetTag();

        void SetTag(object tag);

        void Dispose();
    }
}
