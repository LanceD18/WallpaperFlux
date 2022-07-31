using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MvvmCross.Platforms.Wpf.Views;
using WallpaperFlux.Core.Tools;
using WallpaperFlux.Core.Util;
using WallpaperFlux.WPF.Util;
using WallpaperFlux.WPF.Views;
using WallpaperFlux.WPF.Windows;

namespace WallpaperFlux.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MvxWindow
    {
        public static MainWindow Instance; //! Static reference to the active MainWindow instance, always reset in the constructor

        private const int SetDeskWallpaper = 20;
        private const int UpdateIniFile = 0x01;
        private const int SendWinIniChange = 0x02;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

        public WallpaperWindow[] Wallpapers;

        public MainWindow()
        {
            Debug.WriteLine("------------------------------------" +
                            "\nInitializing static MainWindow Instance");
            Instance = this;
            
            InitializeComponent();
            InitializeWallpapers();

            Closing += OnCloseApplication;
            MessageBoxUtil.InputBoxFunc = OnCallInputBox;
        }

        private void InitializeWallpapers()
        {
            IntPtr workerw = WallpaperUtil.GetDesktopWorkerW();

            int displayCount = DisplayUtil.Displays.Count();
            Wallpapers = new WallpaperWindow[displayCount];
            for (int i = 0; i < displayCount; i++)
            {
                Wallpapers[i] = new WallpaperWindow(DisplayUtil.Displays.ElementAt(i), workerw);
                Wallpapers[i].Show();

                AudioManager.OnMute += Wallpapers[i].Mute;
                AudioManager.OnUnmute += Wallpapers[i].Unmute;
            }
        }

        private void OnCloseApplication(object s, CancelEventArgs e)
        {
            SystemParametersInfo(SetDeskWallpaper, 0, null, UpdateIniFile | SendWinIniChange);

            foreach (WallpaperWindow wallpaper in Wallpapers)
            {
                wallpaper.Close();
            }

            //? MediaElements may continue to overwrite the default wallpaper on closing
            SystemParametersInfo(SetDeskWallpaper, 0, null, UpdateIniFile | SendWinIniChange);

            //? Without this the creation of extra views will stop the program from completely closing normally
            Application.Current.Shutdown();
        }

        private string OnCallInputBox(string title, string caption, string watermark, InputBoxType inputBoxType)
        {
            InputBoxWindow inputBox = new InputBoxWindow(title, caption, watermark, inputBoxType);
            inputBox.ShowDialog();
            return inputBox.Input;
        }

    }
}
