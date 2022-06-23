using System;
using System.Drawing;

namespace WallpaperFlux.Core.IoC
{
    // Registered with the Mvx IoCProvider
    public interface IExternalImage : IDisposable
    {
        bool SetImage(string imagePath);

        Size GetSize();

        object GetTag();

        void SetTag(object tag);
    }
}
