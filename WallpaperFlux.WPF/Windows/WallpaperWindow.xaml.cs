using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata;
using System.Text;
using System.Threading;
using System.Windows;
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
using WpfAnimatedGif;
using WpfScreenHelper;
using System.Threading.Tasks;
using LibVLCSharp.Shared;
using MvvmCross.Platforms.Wpf.Presenters.Attributes;
using MvvmCross.ViewModels;
using Unosquare.FFME;
using WallpaperFlux.Core.ViewModels;
using WallpaperFlux.WPF.Windows;
using MediaElement = System.Windows.Controls.MediaElement;

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
            FFME,
            VLC
        }

        //? The index is currently gathered by the array utilized in ExternalWallpaperHandler and MainWindow
        public ImageModel ActiveImage;

        public bool ChangingWallpaper { get; private set; } = false;

        private int VideoLoopCount;

        private bool Muted;

        private LibVLC _libVlc = new LibVLC(true);

        public WallpaperWindow(Screen display, IntPtr workerw)
        {
            InitializeComponent();

            //xMpvPlayerHostElement.DllPath = MpvUtil.MpvPath;

            Loaded += (s, e) =>
            {
                // Sets bounds of the form
                Width = display.Bounds.Width;
                Height = display.Bounds.Height;
                Left = display.Bounds.X + DisplayUtil.DisplayXAdjustment;
                Top = display.Bounds.Y + DisplayUtil.MinDisplayY;

                //? Default, should match what's stated on the WPF
                WallpaperImage.Stretch = WallpaperMediaElement.Stretch = WallpaperMediaElementFFME.Stretch = WallpaperVlcViewBox.Stretch = Stretch.Fill; // this is actually stretch

                // This line makes the form a child of the WorkerW window, thus putting it behind the desktop icons and out of reach 
                // of any user input. The form will just be rendered, no keyboard or mouse input will reach it.
                //? (Would have to use WH_KEYBOARD_LL and WH_MOUSE_LL hooks to capture mouse and keyboard input)
                Win32.SetParent(new WindowInteropHelper(this).Handle, workerw);

                WallpaperVlc.MediaPlayer = new LibVLCSharp.Shared.MediaPlayer(_libVlc);
            };
        }

        //? The index is checked in ExternalWallpaperHandler now as it has access to the array, which allows wallpapers to be changed independently of one another
        public async void OnWallpaperChange(ImageModel image, bool forceChange)
        {
            ChangingWallpaper = true;
            Debug.WriteLine("Changing into: " + image.Path);

            // --- If the scan is true we end this method early as the video's display time is still valid ---
            if (await VerifyMinLoopMaxTimeSettings(forceChange)) return;

            // --- Verify Wallpaper ---
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

            //xWallpaperImage.BeginInit();
            //xWallpaperMediaElement.BeginInit();
            //xWallpaperMediaElementFFME.BeginInit();

            // -----Set Wallpaper -----
            if (WallpaperUtil.IsSupportedVideoType_GivenExtension(wallpaperInfo.Extension) || wallpaperInfo.Extension == ".gif") // ---- video or gif ----
            {
                UpdateVolume(image); //! Do NOT use ActiveImage here, it is not set until the end of the method!

                if (wallpaperInfo.Extension == ".mp4" || wallpaperInfo.Extension == ".avi") //? VLC can't load .webm files (haven't tried GIFs but FFME handles these fine)
                {
                        using (Media media = new Media(_libVlc, wallpaperInfo.FullName))
                        {
                            //xWallpaperVlc.MediaPlayer?.Stop();
                            WallpaperVlc.MediaPlayer?.Play(media);
                            WallpaperVlcViewBox.IsEnabled = true;
                            WallpaperVlcViewBox.Visibility = Visibility.Visible;

                            if (WallpaperVlc.MediaPlayer != null) // if the media fails to open
                            {
                                WallpaperVlc.MediaPlayer.EndReached += VlcMediaPlayer_OnEndReached;
                            }

                            DisableUnusedElements(UsedElement.VLC);
                        }
                }
                else if (wallpaperInfo.Extension == ".webm" || wallpaperInfo.Extension == ".gif") //? FFME can't handle .avi files and crashes on some .mp4s depending on their pixel format
                {
                    await WallpaperMediaElementFFME.Close();
                    await WallpaperMediaElementFFME.Open(new Uri(wallpaperInfo.FullName));
                    WallpaperMediaElementFFME.IsEnabled = true;
                    WallpaperMediaElementFFME.Visibility = Visibility.Visible;

                    DisableUnusedElements(UsedElement.FFME);

                    RetryMediaOpen(false, image); //? If there's too much load on the system FFME media will fail to start and need to be re-initialized
                }
                else //? Use the MediaElement as a last resort
                {
                    WallpaperMediaElement.Close();
                    WallpaperMediaElement.Source = new Uri(wallpaperInfo.FullName);
                    WallpaperMediaElement.IsEnabled = true;
                    WallpaperMediaElement.Visibility = Visibility.Visible;

                    DisableUnusedElements(UsedElement.MediaElement);
                }

            }
            else //? ---- static ----
            {
                // TODO Consider adding a check for the static image type as well, as random file types can still be detected and cause a crash
                // TODO Granted, they would have to be manually ranked by the user first, so you should probably instead just ban them from the ImageInspector

                BitmapImage bitmap = new BitmapImage();
                //xusing (FileStream stream = File.OpenRead(image.Path))
                //x{
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    //xbitmap.StreamSource = stream;
                    bitmap.UriSource = new Uri(image.Path); // supposedly Uri is asynchronous, test the speed between UriSource & StreamSource, but also test their memory handling
                    bitmap.EndInit();
                    //? this should also allow the bitmap to be picked up by the automatic garbage collection (so don't GC manually)
                    bitmap.Freeze(); // this needs to be frozen before the bitmap is used in the UI thread, call this right after bitmap.EndInit() | https://stackoverflow.com/questions/46709382/async-load-bitmapimage-in-c-sharp
                //x}

                WallpaperImage.Source = bitmap;
                WallpaperImage.IsEnabled = true;
                WallpaperImage.Visibility = Visibility.Visible;

                DisableUnusedElements(UsedElement.Image);
            }

            ActiveImage = image; //? this change implies that the wallpaper was SUCCESSFULLY changed | Errors, video loop control, etc. can stop this

            //xWallpaperImage.EndInit();
            //xWallpaperMediaElement.EndInit();
            //xWallpaperMediaElementFFME.EndInit();

            ChangingWallpaper = false;
        }

        /// <summary>
        /// Scan video wallpapers for loop & max time settings
        /// </summary>
        /// <param name="forceChange"></param>
        /// <returns></returns>
        private async Task<bool> VerifyMinLoopMaxTimeSettings(bool forceChange)
        {
            if (!forceChange && ActiveImage is { IsVideoOrGif: true }) // we can only make these checks if the previous wallpaper was a video or gif
            {
                int minLoops = ActiveImage.OverrideMinimumLoops ? ActiveImage.MinimumLoops : ThemeUtil.VideoSettings.MinimumLoops;
                int maxTime = ActiveImage.OverrideMaximumTime ? ActiveImage.MaximumTime : ThemeUtil.VideoSettings.MaximumTime;

                Debug.WriteLine("VideoLoopCount: " + VideoLoopCount + " | MinimumVideoLoops: " + minLoops);
                if (VideoLoopCount < minLoops)
                {
                    //? we will only check for the video time condition if we have not yet gone beyond the Minimum Loop count
                    //? essentially, changes are only allowed if we are both above the minimum loop count AND the max video time

                    //! a test countermeasure against failed loads never looping
                    if (WallpaperMediaElement.IsLoaded) WallpaperMediaElement.Play();
                    if (WallpaperMediaElementFFME.IsLoaded) await WallpaperMediaElementFFME.Play();
                    //! a test countermeasure against failed loads never looping

                    Debug.WriteLine("Nax Video Time: " + maxTime);
                    Debug.WriteLine("Media: " + WallpaperMediaElement.Position.Seconds + " | FFME: " + WallpaperMediaElementFFME.Position.Seconds);
                    if (WallpaperMediaElement.Position.Seconds <= maxTime ||
                        WallpaperMediaElementFFME.Position.Seconds <= maxTime)
                    {
                        ThemeUtil.Theme.WallpaperRandomizer.ActiveWallpapers[WallpaperWindowUtil.GetWallpaperIndex(this)] = ActiveImage.Path;
                        return true;
                    }
                }

                VideoLoopCount = 0; // if we are allowed to make a change, reset the loop count
            }

            return false;
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

        private async void DisableFFME()
        {
            await WallpaperMediaElementFFME.Close();
            WallpaperMediaElementFFME.IsEnabled = false;
            WallpaperMediaElementFFME.Visibility = Visibility.Hidden;
        }

        private void DisableVlc()
        {
            WallpaperVlc.MediaPlayer?.Stop();
            WallpaperVlcViewBox.IsEnabled = false;
            WallpaperVlcViewBox.Visibility = Visibility.Hidden;
        }

        private void DisableUnusedElements(UsedElement usedElement)
        {
            switch (usedElement)
            {
                case UsedElement.Image:
                    DisableMediaElement();
                    DisableFFME();
                    DisableVlc();
                    break;

                case UsedElement.MediaElement:
                    DisableFFME();
                    DisableVlc();
                    DisableImage();
                    break;

                case UsedElement.FFME:
                    DisableMediaElement();
                    DisableVlc();
                    DisableImage();
                    break;

                case UsedElement.VLC:
                    DisableMediaElement();
                    DisableFFME();
                    DisableImage();
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
                        WallpaperImage.Stretch = WallpaperMediaElement.Stretch = WallpaperMediaElementFFME.Stretch = WallpaperVlcViewBox.Stretch = Stretch.UniformToFill;
                        //xWallpaperImage.Stretch = Stretch.UniformToFill;
                        break;

                    case WallpaperStyle.Stretch:
                        WallpaperImage.Stretch = WallpaperMediaElement.Stretch = WallpaperMediaElementFFME.Stretch = WallpaperVlcViewBox.Stretch = Stretch.Fill;
                        //xWallpaperImage.Stretch = Stretch.Fill;
                        break;

                    case WallpaperStyle.Fit:
                        WallpaperImage.Stretch = WallpaperMediaElement.Stretch = WallpaperMediaElementFFME.Stretch = WallpaperVlcViewBox.Stretch = Stretch.Uniform;
                        //xWallpaperImage.Stretch = Stretch.Uniform;
                        break;

                    case WallpaperStyle.Center:
                        WallpaperImage.Stretch = WallpaperMediaElement.Stretch = WallpaperMediaElementFFME.Stretch = WallpaperVlcViewBox.Stretch = Stretch.None;
                        //xWallpaperImage.Stretch = Stretch.None;
                        break;
                }
            });
        }

        private void WallpaperMediaElement_OnMediaEnded(object sender, RoutedEventArgs e)
        {
            VideoLoopCount++;

            if (sender is MediaElement element)
            {
                element.Position = TimeSpan.Zero;
                element.Play();
            }
        }

        private void WallpaperMediaElementFFME_OnMediaEnded(object? sender, EventArgs e) => VideoLoopCount++;

        private void VlcMediaPlayer_OnEndReached(object? sender, EventArgs e)
        {
            Debug.WriteLine("am loopin");
            VideoLoopCount++;
        }

        public void Mute()
        {
            Muted = true;
            UpdateVolume(ActiveImage);
        }

        public void Unmute()
        {
            Muted = false;
            UpdateVolume(ActiveImage);
        }

        public void UpdateVolume()
        {
            if (ChangingWallpaper) return;
            UpdateVolume(ActiveImage);
        }

        private void UpdateVolume(ImageModel image)
        {
            Dispatcher.Invoke(() =>
            {
                if (!Muted)
                {
                    if (image != null) //? it's okay to set the volume to 0 ahead of time, but sometimes the given image may not be initialized
                    {
                        WallpaperMediaElement.Volume = WallpaperMediaElementFFME.Volume =  image.Volume / 100;

                        if (WallpaperVlc.MediaPlayer != null)
                        {
                            WallpaperVlc.MediaPlayer.Volume = (int)image.Volume;
                        }
                    }
                }
                else
                {
                    WallpaperMediaElement.Volume = WallpaperMediaElementFFME.Volume = 0;

                    if (WallpaperVlc.MediaPlayer != null)
                    {
                        WallpaperVlc.MediaPlayer.Volume = 0;
                    }
                }
            });
        }

        private async void RetryMediaOpen(bool finalAttempt, ImageModel image)
        {
            //xif (image.IsWebmOrGif) return; //? webms & gifs do not advance the position value, closing and reopening them will turn them off

            // retries opening the video if it fails
            await Task.Run(() =>
            {
                Thread.Sleep(1000);

                Dispatcher.Invoke(() =>
                {
                    Debug.WriteLine("FFME Opening: " + WallpaperMediaElementFFME.IsOpening);
                    if (WallpaperMediaElementFFME.IsEnabled && !WallpaperMediaElementFFME.IsOpening)
                    {
                        Debug.WriteLine("FFME Position: " + WallpaperMediaElementFFME.Position);
                        //if (WallpaperMediaElementFFME.Position < TimeSpan.FromSeconds(1))
                        //{
                        Debug.WriteLine("Fixing FFME");
                        //xWallpaperMediaElementFFME.Close();
                        WallpaperMediaElementFFME.Play();
                        DisableUnusedElements(UsedElement.FFME);

                        if (!finalAttempt)
                        {
                            //WallpaperMediaElementFFME.Open(new Uri(image.Path));
                            RetryMediaOpen(true, image);
                        }
                        //}
                    }
                });
            });
        }
    }
}
