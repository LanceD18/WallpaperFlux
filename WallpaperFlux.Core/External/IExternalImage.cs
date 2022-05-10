using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;

namespace WallpaperFlux.Core.External
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
