using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using MvvmCross;
using MvvmCross.ViewModels;
using WallpaperFlux.Core.ViewModels;

namespace WallpaperFlux.Core
{
    public class App : MvxApplication
    {
        public override void Initialize()
        {
            RegisterAppStart<WallpaperFluxViewModel>();
        }
    }
}
