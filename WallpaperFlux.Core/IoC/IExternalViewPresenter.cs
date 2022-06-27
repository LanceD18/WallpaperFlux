using System;
using System.Collections.Generic;
using System.Text;

namespace WallpaperFlux.Core.IoC
{
    public interface IExternalViewPresenter
    {
        void PresentImageSelectionOptions();

        void CloseImageSelectionOptions();
    }
}
