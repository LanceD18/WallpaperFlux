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
using HanumanInstitute.MediaPlayer.Wpf.Mpv;
using LanceTools.WindowsUtil;
using Mpv.NET.Player;
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
        //? The index is currently gathered by the array utilized in ExternalWallpaperHandler and MainWindow

        public WallpaperWindow(Screen display, IntPtr workerw)
        {
            InitializeComponent();
            

            Loaded += (s, e) =>
            {
                // Sets bounds of the form
                Width = display.Bounds.Width;
                Height = display.Bounds.Height;
                Left = display.Bounds.X + DisplayUtil.DisplayXAdjustment;
                Top = display.Bounds.Y + DisplayUtil.MinDisplayY;

                //x // TODO Remove me once you've implemented a control for setting the default volume & changing video volume
                //x //! temp
                //x Debug.WriteLine("Forcing video volume to 0.1 for now");
                //x WallpaperMediaElement.Volume = 0.1;
                //x //! temp

                //? Default, should match what's stated on the WPF
                WallpaperImage.Stretch = WallpaperMediaElement.Stretch = WallpaperMediaElementFFME.Stretch = Stretch.Fill;
                //xWallpaperImage.Stretch = Stretch.Fill;

                // This line makes the form a child of the WorkerW window, thus putting it behind the desktop icons and out of reach 
                // of any user input. The form will just be rendered, no keyboard or mouse input will reach it.
                //? (Would have to use WH_KEYBOARD_LL and WH_MOUSE_LL hooks to capture mouse and keyboard input)
                Win32.SetParent(new WindowInteropHelper(this).Handle, workerw);
            };

        }

        //? The index is checked in ExternalWallpaperHandler now as it has access to the array, which allows wallpapers to be changed independently of one another
        public void OnWallpaperChange(ImageModel image)
        {
            FileInfo wallpaperInfo;
            string wallpaperPath = image.Path;

            if (!String.IsNullOrEmpty(wallpaperPath))
            {
                wallpaperInfo = new FileInfo(wallpaperPath);
            }
            else
            {
                Debug.WriteLine("Null Wallpaper Path found when calling OnWallpaperChange");
                return;
            }

            if (wallpaperInfo.Extension == ".gif" || WallpaperUtil.IsSupportedVideoType(wallpaperInfo.FullName)) // gif & video
            {
                WallpaperMediaElement.Volume = WallpaperMediaElementFFME.Volume = image.Volume;
                //xWallpaperMediaElement.PlayerHost.Volume = (int)(image.Volume * 100);

                //? FFME is unstable and likely to crash on larger videos, but Windows Media Player can't load webms | Also, it seems to load Gifs faster
                if (wallpaperInfo.Extension == ".gif" || wallpaperInfo.Extension == ".webm") 
                {
                    WallpaperMediaElement.Close();
                    WallpaperMediaElementFFME.Open(new Uri(wallpaperInfo.FullName));
                    WallpaperMediaElement.IsEnabled = false;
                    WallpaperMediaElement.Visibility = Visibility.Hidden;
                    WallpaperMediaElementFFME.IsEnabled = true;
                    WallpaperMediaElementFFME.Visibility = Visibility.Visible;
                }
                else
                {
                    WallpaperMediaElementFFME.Close();
                    WallpaperMediaElement.Source = new Uri(wallpaperInfo.FullName);
                    WallpaperMediaElementFFME.IsEnabled = false;
                    WallpaperMediaElementFFME.Visibility = Visibility.Hidden;
                    WallpaperMediaElement.IsEnabled = true;
                    WallpaperMediaElement.Visibility = Visibility.Visible;
                }
                
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

                //xWallpaperMediaElement.Close(); // ensures that the audio stops playing
                //xWallpaperMediaElement.PlayerHost.Stop();
                WallpaperMediaElement.Close();
                WallpaperMediaElementFFME.Close();
                WallpaperMediaElement.IsEnabled = WallpaperMediaElementFFME.IsEnabled = false;
                WallpaperMediaElement.Visibility = WallpaperMediaElementFFME.Visibility = Visibility.Hidden;
                WallpaperImage.IsEnabled = true;
                WallpaperImage.Visibility = Visibility.Visible;
            }
        }

        //? The index is checked in ExternalWallpaperHandler now as it has access to the array, which allows wallpapers to be changed independently of one another
        public void OnWallpaperStyleChange(WallpaperStyle style)
        {
            Dispatcher.Invoke(() =>
            {
                switch (style)
                {
                    case WallpaperStyle.Fill:
                        WallpaperImage.Stretch = WallpaperMediaElement.Stretch = WallpaperMediaElementFFME.Stretch = Stretch.UniformToFill;
                        //xWallpaperImage.Stretch = Stretch.UniformToFill;
                        break;

                    case WallpaperStyle.Stretch:
                        WallpaperImage.Stretch = WallpaperMediaElement.Stretch = WallpaperMediaElementFFME.Stretch = Stretch.Fill;
                        //xWallpaperImage.Stretch = Stretch.Fill;
                        break;

                    case WallpaperStyle.Fit:
                        WallpaperImage.Stretch = WallpaperMediaElement.Stretch = WallpaperMediaElementFFME.Stretch = Stretch.Uniform;
                        //xWallpaperImage.Stretch = Stretch.Uniform;
                        break;

                    case WallpaperStyle.Center:
                        WallpaperImage.Stretch = WallpaperMediaElement.Stretch = WallpaperMediaElementFFME.Stretch = Stretch.None;
                        //xWallpaperImage.Stretch = Stretch.None;
                        break;
                }
            });
        }

        private void WallpaperMediaElement_OnMediaEnded(object sender, RoutedEventArgs e)
        {
            if (sender is MediaElement element)
            {
                element.Position = TimeSpan.FromSeconds(0);
                element.Play();
            }
        }
    }
}
