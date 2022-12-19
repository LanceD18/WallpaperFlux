using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata;
using System.Text;
using System.Threading;
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
using WallpaperFlux.WPF.Controllers;
using WallpaperFlux.WPF.Util;
using WpfAnimatedGif;
using WpfScreenHelper;

namespace WallpaperFlux.WPF
{
    /// <summary>
    /// Interaction logic for WallpaperWindow.xaml
    /// </summary>
    public partial class WallpaperWindow : MvxWindow
    {
        private enum UsedElement
        {
            Image,
            MediaElement,
            FFME
        }

        //? The index is currently gathered by the array utilized in ExternalWallpaperHandler and MainWindow
        public ImageModel ActiveImage;

        private bool muted;

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

                //? Default, should match what's stated on the WPF
                WallpaperImage.Stretch = WallpaperMediaElement.Stretch = WallpaperMediaElementFFME.Stretch = Stretch.Fill; // this is actually stretch

                // This line makes the form a child of the WorkerW window, thus putting it behind the desktop icons and out of reach 
                // of any user input. The form will just be rendered, no keyboard or mouse input will reach it.
                //? (Would have to use WH_KEYBOARD_LL and WH_MOUSE_LL hooks to capture mouse and keyboard input)
                Win32.SetParent(new WindowInteropHelper(this).Handle, workerw);
            };
        }

        //? The index is checked in ExternalWallpaperHandler now as it has access to the array, which allows wallpapers to be changed independently of one another
        public async void OnWallpaperChange(ImageModel image)
        {
            ActiveImage = image;

            FileInfo wallpaperInfo;
            string wallpaperPath = image.Path;

            if (!string.IsNullOrEmpty(wallpaperPath))
            {
                wallpaperInfo = new FileInfo(wallpaperPath);
            }
            else
            {
                Debug.WriteLine("Null Wallpaper Path found when calling OnWallpaperChange");
                return;
            }

            WallpaperImage.BeginInit();
            WallpaperMediaElement.BeginInit();
            WallpaperMediaElementFFME.BeginInit();

            if (WallpaperUtil.IsSupportedVideoType(wallpaperInfo.FullName) || wallpaperInfo.Extension == ".gif") // ---- video or gif ----
            {
                UpdateVolume();

                //xawait WallpaperMediaElementFFME.Close();
                //xawait WallpaperMediaElementFFME.Open(new Uri(wallpaperInfo.FullName));
                MediaController.SendRequest(WallpaperMediaElementFFME, wallpaperPath);
                WallpaperMediaElementFFME.IsEnabled = true;
                WallpaperMediaElementFFME.Visibility = Visibility.Visible;

                /*x
                //? FFME is unstable and likely to crash on larger videos, but Windows Media Player can't load webms | Also, it seems to load Gifs faster
                if (wallpaperInfo.Extension == ".webm" || wallpaperInfo.Extension == ".gif") 
                {
                    DisableMediaElement();

                    await WallpaperMediaElementFFME.Close();
                    await WallpaperMediaElementFFME.Open(new Uri(wallpaperInfo.FullName));
                    WallpaperMediaElementFFME.IsEnabled = true;
                    WallpaperMediaElementFFME.Visibility = Visibility.Visible;
                }
                else //? The regular MediaElement (Windows Media Player) will randomly fail to load gifs (seems to be if loading more than one), loading a blank screen instead
                {
                    DisableMediaElementFFME();
                    
                    WallpaperMediaElement.Source = new Uri(wallpaperInfo.FullName); 
                    WallpaperMediaElement.IsEnabled = true;
                    WallpaperMediaElement.Visibility = Visibility.Visible;
                }
                */

                DisableImage();
            }
            else // ---- static ----
            {
                // TODO Consider adding a check for the static image type as well, as random file types can still be detected and cause a crash
                // TODO Granted, they would have to be manually ranked by the user first, so you should probably instead just ban them from the ImageInspector

                BitmapImage bitmap = new BitmapImage();

                bitmap.BeginInit();
                bitmap.UriSource = new Uri(image.Path);
                bitmap.EndInit();
                
                WallpaperImage.Source = bitmap;

                WallpaperImage.IsEnabled = true;
                WallpaperImage.Visibility = Visibility.Visible;

                DisableUnusedElements(UsedElement.Image);

                bitmap.Close();
            }

            WallpaperImage.EndInit();
            WallpaperMediaElement.EndInit();
            WallpaperMediaElementFFME.EndInit();
        }

        private void DisableImage()
        {
            WallpaperImage.Source = null;
            WallpaperImage.IsEnabled = false;
            WallpaperImage.Visibility = Visibility.Hidden;
        }

        private void DisableMediaElement()
        {
            WallpaperMediaElement.Close();
            WallpaperMediaElement.Source = null; //! .Close() won't actually stop the video for MediaElements, see if you should just remove it
            WallpaperMediaElement.IsEnabled = false;
            WallpaperMediaElement.Visibility = Visibility.Hidden;
        }

        private async void DisableMediaElementFFME()
        {
            await WallpaperMediaElementFFME.Close();
            WallpaperMediaElementFFME.IsEnabled = false;
            WallpaperMediaElementFFME.Visibility = Visibility.Hidden;
        }

        private void DisableUnusedElements(UsedElement usedElement)
        {
            switch (usedElement)
            {
                case UsedElement.Image:
                    DisableMediaElement();
                    DisableMediaElementFFME();
                    break;

                case UsedElement.MediaElement:
                    DisableImage();
                    DisableMediaElementFFME();
                    break;

                case UsedElement.FFME:
                    DisableImage();
                    DisableMediaElement();
                    break;
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
                element.Position = TimeSpan.Zero;
                element.Play();
            }
        }
        
        /*x
        private void WallpaperMediaElementFFME_OnMediaEnded(object? sender, EventArgs e)
        {
            Debug.WriteLine("(FFME) A");
            if (sender is Unosquare.FFME.MediaElement ffmeElement)
            {
                Debug.WriteLine(ffmeElement.Source.AbsoluteUri);
                Debug.WriteLine("B");
                //xffmeElement.Pause();
                //xffmeElement.Stop();
                //xffmeElement.Position = TimeSpan.Zero;
                //xffmeElement.Play();
                //xWallpaperMediaElementFFME.Open(new Uri(ffmeElement.Source.AbsoluteUri));
                Debug.WriteLine("C");
            }

            Debug.WriteLine("D");
        }

        */
        public void Mute()
        {
            muted = true;
            UpdateVolume();
        }

        public void Unmute()
        {
            muted = false;
            UpdateVolume();
        }

        public void UpdateVolume()
        {
            Dispatcher.Invoke(() =>
            {
                if (!muted)
                {
                    if (ActiveImage != null) //? it's okay to set the volume to 0 ahead of time, but sometimes the ActiveImage may not be initialized
                    {
                        WallpaperMediaElement.Volume = WallpaperMediaElementFFME.Volume = ActiveImage.Volume / 100;
                    }
                }
                else
                {
                    WallpaperMediaElement.Volume = WallpaperMediaElementFFME.Volume = 0;
                }
            });
        }
    }
}
