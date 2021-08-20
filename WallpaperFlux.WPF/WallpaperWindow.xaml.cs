using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata;
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
using MvvmCross.Platforms.Wpf.Views;
using Unosquare.FFME.Common;
using WallpaperFlux.Core.Models;
using WallpaperFlux.Core;
using WallpaperFlux.Core.Util;
using WallpaperFlux.WPF.Util;
using WpfScreenHelper;

namespace WallpaperFlux.WPF
{
    /// <summary>
    /// Interaction logic for WallpaperWindow.xaml
    /// </summary>
    public partial class WallpaperWindow : MvxWindow
    {
        public int WallpaperIndex { get; }

        public WallpaperWindow(Screen display, IntPtr workerw, int wallpaperIndex)
        {
            InitializeComponent();

            WallpaperIndex = wallpaperIndex;

            Loaded += (s, e) =>
            {
                // Sets bounds of the form
                Width = display.Bounds.Width;
                Height = display.Bounds.Height;
                Left = display.Bounds.X + DisplayUtil.DisplayXAdjustment;
                Top = display.Bounds.Y + DisplayUtil.MinDisplayY;

                //! temp
                WallpaperMediaElement.Volume = 0.1;
                //! temp

                //? Default, should match what's stated on the WPF
                WallpaperImage.Stretch = WallpaperMediaElement.Stretch = Stretch.Fill;

                WallpaperUtil.OnWallpaperChange += OnWallpaperChange;

                WallpaperUtil.OnWallpaperStyleChange += OnWallpaperStyleChange;

                // This line makes the form a child of the WorkerW window, thus putting it behind the desktop icons and out of reach 
                // of any user input. The form will just be rendered, no keyboard or mouse input will reach it.
                //? (Would have to use WH_KEYBOARD_LL and WH_MOUSE_LL hooks to capture mouse and keyboard input)
                Win32.SetParent(new WindowInteropHelper(this).Handle, workerw);
            };
        }

        private void OnWallpaperChange(int index, string path)
        {
            if (index != WallpaperIndex) return; // allows wallpapers to be changed independently of one another

            FileInfo wallpaperInfo;
            string wallpaperPath = path;

            if (!String.IsNullOrEmpty(wallpaperPath))
            {
                wallpaperInfo = new FileInfo(wallpaperPath);
            }
            else
            {
                Debug.WriteLine("Null Wallpaper Path found when calling OnWallpaperChange");
                return;
            }

            if (wallpaperInfo.Extension == ".gif" || WallpaperUtil.IsSupportedVideoType(wallpaperInfo)) // gif & video
            {
                WallpaperMediaElement.Open(new Uri(wallpaperInfo.FullName));
                WallpaperMediaElement.IsEnabled = true;
                WallpaperMediaElement.Visibility = Visibility.Visible;
                WallpaperImage.IsEnabled = false;
                WallpaperImage.Visibility = Visibility.Hidden;
            }
            else // static image
            {
                // TODO Consider adding a check for the static image type as well, as random file types can still be detected and cause a crash
                // TODO Granted, they would have to be manually ranked by the user first, so you should probably instead just ban them from the ImageInspector
                WallpaperImage.Source = new BitmapImage(new Uri(wallpaperInfo.FullName));

                // TODO Wallpaper Styling:
                //! Use me later: WallpaperImage.Stretch

                WallpaperMediaElement.Close(); // ensures that the audio stops playing
                WallpaperMediaElement.IsEnabled = false;
                WallpaperMediaElement.Visibility = Visibility.Hidden;
                WallpaperImage.IsEnabled = true;
                WallpaperImage.Visibility = Visibility.Visible;
            }
        }

        private void OnWallpaperStyleChange(int index, WallpaperStyle style)
        {
            if (index != WallpaperIndex) return;

            Dispatcher.Invoke(() =>
            {
                switch (style)
                {
                    case WallpaperStyle.Fill:
                        WallpaperImage.Stretch = WallpaperMediaElement.Stretch = Stretch.UniformToFill;
                        break;

                    case WallpaperStyle.Stretch:
                        WallpaperImage.Stretch = WallpaperMediaElement.Stretch = Stretch.Fill;
                        break;

                    case WallpaperStyle.Fit:
                        WallpaperImage.Stretch = WallpaperMediaElement.Stretch = Stretch.Uniform;
                        break;

                    case WallpaperStyle.Center:
                        WallpaperImage.Stretch = WallpaperMediaElement.Stretch = Stretch.None;
                        break;
                }
            });
        }
    }
}
