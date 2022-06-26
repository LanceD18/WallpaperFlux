using System;
using System.Collections.Generic;
using System.Text;

namespace WallpaperFlux.Core.IoC
{
    public interface IExternalMediaElement : IDisposable
    {
        void SetMediaElement(string elementPath);

        int GetWidth();

        int GetHeight();

        double GetNaturalDuration();
    }
}
