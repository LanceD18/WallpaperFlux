using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows;
using Microsoft.VisualBasic.FileIO;
using WallpaperFlux.Core.IoC;
using WallpaperFlux.WPF.Util;

namespace WallpaperFlux.WPF.IoC
{
    public class ExternalAppUtil : IExternalAppUtil
    {
        public void OpenWindows()
        {
            MainWindow.Instance.Show();
            MainWindow.Instance.WindowState = WindowState.Normal;
            WindowUtil.ShowAllWindows();
        }

        public void CloseApp() => MainWindow.Instance.Close();
    }
}
