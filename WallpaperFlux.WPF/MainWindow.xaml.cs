using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using HanumanInstitute.MediaPlayer.Wpf.Mpv;
using Mpv.NET.Player;
using MvvmCross.Platforms.Wpf.Views;
using WallpaperFlux.Core.Tools;
using WallpaperFlux.Core.Util;
using WallpaperFlux.Winform;
using WallpaperFlux.Winform.Util;
using WallpaperFlux.WPF.Tools;
using WallpaperFlux.WPF.Util;
using WallpaperFlux.WPF.Views;
using WallpaperFlux.WPF.Windows;
using WpfScreenHelper;
using Application = System.Windows.Application;

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
        public WallpaperForm[] WallpaperForms;

        private HotkeyManager _hotkeyManager;
        //xprivate HwndSource _source;

        private WindowInteropHelper _interopHelper;
        
        public MainWindow()
        {
            Debug.WriteLine("------------------------------------" +
                            "\nInitializing static MainWindow Instance");
            Instance = this;
            
            InitializeComponent();
            InitializeWallpapers();
            WindowUtil.InitializeViewModels();

            Closing += OnCloseApplication;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            _interopHelper = new WindowInteropHelper(this);
            _hotkeyManager = new HotkeyManager(_interopHelper.Handle);

            /*
            _source = HwndSource.FromHwnd(helper.Handle);
            _source.AddHook(HwndHook);
            RegisterHotKey();
            */

        }

        // hide the application on minimize (will go to system tray)
        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == System.Windows.WindowState.Minimized)
            {
                this.Hide();
                WindowUtil.HideAllWindows();
            }

            base.OnStateChanged(e);
        }

        private void InitializeWallpapers()
        {
            IntPtr workerw = WallpaperUtil.GetDesktopWorkerW();

            int displayCount = DisplayUtil.Displays.Count();
            MpvUtil.Open = new Action<string>[displayCount];

            Wallpapers = new WallpaperWindow[displayCount];
            WallpaperForms = new WallpaperForm[displayCount];
            for (int i = 0; i < displayCount; i++)
            {
                Screen display = DisplayUtil.Displays.ElementAt(i);

                Wallpapers[i] = new WallpaperWindow(display, workerw, i);
                Wallpapers[i].Show();

                AudioManager.OnMute += Wallpapers[i].Mute;
                AudioManager.OnUnmute += Wallpapers[i].Unmute;
            }
        }

        public void OpenWinform(Screen display, IntPtr workerw, int index, Action onVideoEnd)
        {
            WallpaperForm form = new WallpaperForm(display, workerw, index, onVideoEnd);
            WindowInteropHelper wih = new WindowInteropHelper(this);
            wih.Owner = form.Handle;

            WallpaperForms[index] = form;
        }

        private void OnCloseApplication(object s, CancelEventArgs e)
        {
            _hotkeyManager.UnregisterKeys();

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
    }
}
