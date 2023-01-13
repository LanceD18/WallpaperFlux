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
using MvvmCross.Platforms.Wpf.Presenters.Attributes;
using MvvmCross.ViewModels;
using Unosquare.FFME;
using WallpaperFlux.Core.ViewModels;
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
            FFME
        }

        //? The index is currently gathered by the array utilized in ExternalWallpaperHandler and MainWindow
        public ImageModel ActiveImage;

        public bool ChangingWallpaper { get; private set; } = false;

        //xprivate MpvPlayer MpvPlayerElement;

        private int VideoLoopCount;

        private bool Muted;

        private Screen Display;

        public WallpaperWindow(Screen display, IntPtr workerw)
        {
            InitializeComponent();

            //xMpvPlayerElement = new MpvPlayer(workerw, MpvUtil.MpvPath);

            Loaded += (s, e) =>
            {
                //xDisplay = display;

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

                /*x
                MpvPlayerElement = new MpvPlayer(CreateHwndSource(workerw).Handle, MpvUtil.MpvPath)
                {
                    Loop = true,
                    AutoPlay = true
                };
                */
            };
        }

        //? The index is checked in ExternalWallpaperHandler now as it has access to the array, which allows wallpapers to be changed independently of one another
        public async void OnWallpaperChange(ImageModel image, bool forceChange)
        {
            ChangingWallpaper = true;
            Debug.WriteLine("Changing into: " + image.Path);

            // --- Scan video wallpapers for loop & max time settings ---
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
                        return;
                    }
                }

                VideoLoopCount = 0; // if we are allowed to make a change, reset the loop count
            }

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

                bool ffmeFail = false;

                //? FFME is unstable and likely to crash on larger videos, but Windows Media Player can't load webms | Also, it seems to load gifs faster
                if (wallpaperInfo.Extension == ".webm" || wallpaperInfo.Extension == ".gif" || wallpaperInfo.Extension == ".mp4") 
                {
                    try
                    {
                        /* TODO

                        // https://stackoverflow.com/questions/53799646/ffmpeg-change-output-to-specific-pixel-format
                        // https://stackoverflow.com/questions/4749967/execute-ffmpeg-command-with-c-sharp

                        using (Process p = new Process())
                        {
                            p.StartInfo.UseShellExecute = false;
                            p.StartInfo.CreateNoWindow = true;
                            p.StartInfo.RedirectStandardOutput = true;
                            p.StartInfo.FileName = Library.FFmpegDirectory + "\\ffmpeg.exe";
                            p.StartInfo.Arguments = parameters;
                            p.Start();
                            p.WaitForExit();

                            result = p.StandardOutput.ReadToEnd();
                        }
                        */
                        
                        //xMpvPlayerElement.Load(wallpaperInfo.FullName);
                        //xMpvPlayerElement.Resume();
                        WallpaperMpvFormHost.IsEnabled = true;
                        WallpaperMpvFormHost.Visibility = Visibility.Visible;

                        /*
                        await WallpaperMediaElementFFME.Close();
                        await WallpaperMediaElementFFME.Open(new Uri(wallpaperInfo.FullName));
                        WallpaperMediaElementFFME.IsEnabled = true;
                        WallpaperMediaElementFFME.Visibility = Visibility.Visible;
                        */

                        DisableMediaElement();
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine("FFME fail, trying MediaPlayer | Fail Source: " + wallpaperPath);
                        //? Playing .mp4 videos will sometimes fail and crash the program with no warning, this is likely an exit with the code -1073740940 (0xc0000374)
                        //? MediaPlayer can play .mp4s without crashing but *some* will fail to play their audio, this is a nice compromise

                        ffmeFail = true;
                    }
                }
                else
                {
                    ffmeFail = true;
                }
                
                if (ffmeFail) //? The regular MediaElement (Windows Media Player) will randomly fail to load gifs (seems to be if loading more than one), loading a blank screen instead
                {
                    try
                    {
                        WallpaperMediaElement.Close();
                        WallpaperMediaElement.Source = new Uri(wallpaperInfo.FullName);
                        WallpaperMediaElement.IsEnabled = true;
                        WallpaperMediaElement.Visibility = Visibility.Visible;

                        DisableMediaElementFFME();
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine("MediaPlayer fail, create another solution | Fail Source: " + wallpaperPath);
                        Debug.WriteLine("MediaPlayer fail, create another solution | Fail Source: " + wallpaperPath);
                        Debug.WriteLine("MediaPlayer fail, create another solution | Fail Source: " + wallpaperPath);
                        Debug.WriteLine("MediaPlayer fail, create another solution | Fail Source: " + wallpaperPath);

                        // TODO If we fail again then we'll just have to not load the wallpaper for now and attempt to find a solution in the future
                        // TODO If we fail again then we'll just have to not load the wallpaper for now and attempt to find a solution in the future
                        // TODO If we fail again then we'll just have to not load the wallpaper for now and attempt to find a solution in the future
                    }

                }

                RetryMediaOpen(false, image);

                DisableImage();
            }
            else // ---- static ----
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

            //! temp
            //xMpvPlayerElement.Stop();
            WallpaperMpvFormHost.IsEnabled = false;
            WallpaperMpvFormHost.Visibility = Visibility.Hidden;
            //! temp
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
            VideoLoopCount++;

            if (sender is MediaElement element)
            {
                element.Position = TimeSpan.Zero;
                element.Play();
            }
        }

        private void WallpaperMediaElementFFME_OnMediaEnded(object? sender, EventArgs e) => VideoLoopCount++;

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
                        WallpaperMediaElement.Volume = WallpaperMediaElementFFME.Volume = image.Volume / 100;
                        //xMpvPlayerElement.Volume = (int)image.Volume;
                    }
                }
                else
                {
                   //x WallpaperMediaElement.Volume = WallpaperMediaElementFFME.Volume = MpvPlayerElement.Volume = 0;
                    WallpaperMediaElement.Volume = WallpaperMediaElementFFME.Volume = 0;
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

                    Debug.WriteLine("MediaPlayer Opening: " + WallpaperMediaElementFFME.IsOpening);
                    if (WallpaperMediaElement.IsEnabled && !WallpaperMediaElementFFME.IsOpening)
                    {
                        Debug.WriteLine("MediaPlayer Position: " + WallpaperMediaElement.Position);
                        //if (WallpaperMediaElement.Position < TimeSpan.FromSeconds(1))
                        //{
                        Debug.WriteLine("Fixing MediaPlayer");
                        //xWallpaperMediaElement.Close();
                        WallpaperMediaElementFFME.Play();
                        DisableUnusedElements(UsedElement.MediaElement);

                        if (!finalAttempt)
                        {
                            //WallpaperMediaElement.Source = new Uri(image.Path);
                            RetryMediaOpen(true, image);
                        }
                        //}
                    }
                });
            });
        }
    }
}
