using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using MvvmCross;
using MvvmCross.IoC;
using MvvmCross.Platforms.Wpf.Core;
using Serilog;
using Serilog.Extensions.Logging;
using WallpaperFlux.Core;
using WallpaperFlux.Core.IoC;
using WallpaperFlux.WPF.IoC;
using WallpaperFlux.WPF.Util;

namespace WallpaperFlux.WPF
{
    public class Setup : MvxWpfSetup<Core.App>
    {
        protected override void InitializeFirstChance(IMvxIoCProvider iocProvider)
        {
            // initializes IoCProvider
            base.InitializeFirstChance(iocProvider);
            Mvx.IoCProvider.RegisterType<IExternalTimer, ExternalTimer>();
            Mvx.IoCProvider.RegisterType<IExternalBitmapImage, ExternalBitmapImage>();
            Mvx.IoCProvider.RegisterType<IExternalDisplayUtil, ExternalDisplayUtil>();
            Mvx.IoCProvider.RegisterType<IExternalImage, ExternalImage>();
            Mvx.IoCProvider.RegisterType<IExternalMediaElement, ExternalMediaElement>();
            Mvx.IoCProvider.RegisterType<IExternalWallpaperHandler, ExternalWallpaperHandler>();
            Mvx.IoCProvider.RegisterType<IExternalFileSystemUtil, ExternalFileSystemUtil>();
            Mvx.IoCProvider.RegisterType<IExternalViewPresenter, ExternalViewPresenter>();
        }

        protected override ILoggerProvider CreateLogProvider()
        {
            return new SerilogLoggerProvider();
        }

        protected override ILoggerFactory CreateLogFactory()
        {
            // serilog configuration
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Trace()
                .CreateLogger();

            return new SerilogLoggerFactory();
        }
    }
}
