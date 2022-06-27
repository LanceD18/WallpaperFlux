using System;
using System.Collections.Generic;
using System.Text;
using WallpaperFlux.Core.IoC;
using WallpaperFlux.WPF.Util;

namespace WallpaperFlux.WPF.IoC
{
    public class ExternalViewPresenter : IExternalViewPresenter
    {
        public void PresentImageSelectionOptions() => WindowUtil.PresentImageSelectionView();

        public void CloseImageSelectionOptions() => WindowUtil.CloseWindow(WindowUtil.ImageSelectionPresenter);
    }
}
