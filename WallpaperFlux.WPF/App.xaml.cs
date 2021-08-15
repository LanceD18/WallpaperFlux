using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using MvvmCross;
using MvvmCross.Core;
using MvvmCross.Platforms.Wpf.Views;
using Unosquare.FFME;
using WallpaperFlux.Core;
using WallpaperFlux.WPF.Util;

namespace WallpaperFlux.WPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : MvxApplication
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            InitializeFFmpeg();

            base.OnStartup(e);
        }

        // called before OnStartup
        protected override void RegisterSetup()
        {
            this.RegisterSetupType<Setup>();
        }

        private void InitializeFFmpeg()
        {
            Library.FFmpegDirectory = @"F:\~ffmpeg\ffmpeg-4.4-full_build-shared\bin";
            Library.LoadFFmpeg();
            MediaElement.FFmpegMessageLogged += (s, ev) =>
            {
                System.Diagnostics.Debug.WriteLine(ev.Message);
            };
        }
    }
}
