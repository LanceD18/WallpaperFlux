using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows;
using LanceTools.IO;
using Unosquare.FFME;
using WallpaperFlux.Core.IoC;
using WallpaperFlux.Core.Util;

namespace WallpaperFlux.WPF.IoC
{
    public class ExternalMediaElement : IExternalMediaElement
    {
        private MediaElement _internalMediaElement = new MediaElement();

        public void SetMediaElement(string elementPath)
        {
            if (ImageUtil.SetImageThread.IsAlive) ImageUtil.SetImageThread.Join();

            ImageUtil.SetImageThread = new Thread(() =>
            {
                /*x
                //? will conflict with the UI thread without this
                Application.Current.Dispatcher.Invoke(delegate 
                {
                    _internalMediaElement = new MediaElement();
                });
                */

                if (FileUtil.Exists(elementPath) && !WallpaperUtil.IsSupportedVideoType(elementPath))
                {
                    _internalMediaElement.Open(new Uri(elementPath));
                }
            });
            ImageUtil.SetImageThread.Start();
            ImageUtil.SetImageThread.Join();
        }
        

        public int GetWidth() => (int)_internalMediaElement.RenderSize.Width;

        public int GetHeight() => (int)_internalMediaElement.RenderSize.Height;

        public double GetNaturalDuration() => _internalMediaElement.NaturalDuration.Value.TotalSeconds;

        public void Dispose() => _internalMediaElement?.Dispose();
    }
}
