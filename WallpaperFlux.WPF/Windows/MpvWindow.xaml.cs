using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using LanceTools.WindowsUtil;
using WallpaperFlux.WPF.Util;

namespace WallpaperFlux.WPF.Windows
{
    /// <summary>
    /// Interaction logic for MpvWindow.xaml
    /// </summary>
    public partial class MpvWindow : Window
    {
        public int DisplayIndex;

        public MpvWindow(Window parent, int displayIndex, IntPtr workerw)
        {
            InitializeComponent();

            /*x
            Loaded += (s, e) =>
            {
                Win32.SetParent(new WindowInteropHelper(this).Handle, workerw);

                DisplayIndex = displayIndex;

                MpvHost.DllPath = MpvUtil.MpvPath;
                MpvUtil.Open[DisplayIndex] = Open;
            };
            */

        }

        public void Open(string filename)
        {
            MpvHost.Source = filename;
        }
    }
}
