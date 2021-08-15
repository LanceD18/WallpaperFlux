using System;
using System.Collections.Generic;
using System.Text;

namespace WallpaperFlux.Core
{
    public interface ITimer
    {
        TimeSpan Interval { get; set; }

        bool IsEnabled { get; set; }

        void Start();

        void Stop();

        event EventHandler Tick;
    }
}
