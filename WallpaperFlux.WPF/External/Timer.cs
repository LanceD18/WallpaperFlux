using System;
using System.Windows;
using System.Windows.Threading;
using WallpaperFlux.Core.External;

namespace WallpaperFlux.WPF.External
{
    //! Be sure to call dispatcherTimer.Stop() when you close your form. The WinForms version of the timer does that automatically.
    //! (That's the advantage of making the timer a Control.) If you don't you'll have a memory leak and possibly other bugs
    public class Timer : ITimer
    {
        private readonly DispatcherTimer internalTimer;

        public Timer()
        {
            internalTimer = new DispatcherTimer(DispatcherPriority.Background, Application.Current.Dispatcher);
        }

        public bool IsEnabled
        {
            get => internalTimer.IsEnabled;
            set => internalTimer.IsEnabled = value;
        }

        public TimeSpan Interval
        {
            get => internalTimer.Interval;
            set => internalTimer.Interval = value;
        }

        public void Start() => internalTimer.Start();

        public void Stop() => internalTimer.Stop();

        public event EventHandler Tick
        {
            add => internalTimer.Tick += value;
            remove => internalTimer.Tick -= value;
        }
    }
}
