using System;

namespace WallpaperFlux.Core.External
{
    // Registered with the Mvx IoCProvider
    public interface ITimer
    {
        TimeSpan Interval { get; set; }

        bool IsEnabled { get; set; }

        void Start();

        void Stop();

        event EventHandler Tick;
    }
}