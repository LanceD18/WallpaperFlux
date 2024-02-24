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
using XamlAnimatedGif;
using WpfScreenHelper;
using System.Threading.Tasks;
using System.Windows.Threading;
using LanceTools.WPF.Adonis.Util;
using LibVLCSharp.Shared;
using MvvmCross.Platforms.Wpf.Presenters.Attributes;
using MvvmCross.ViewModels;
using Unosquare.FFME;
using WallpaperFlux.Core.Controllers;
using WallpaperFlux.Core.Managers;
using WallpaperFlux.Core.Tools;
using WallpaperFlux.Core.ViewModels;
using WallpaperFlux.Winform;
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
            None,
            Image,
            MediaElement,
            FFME,
            Mpv
        }

        public ImageModel ActiveImage;

        private int LoopCount;

        private int AnimatedSetIndex = 0;

        public int DisplayIndex;

        public Screen Display;

        private IntPtr _workerw;

        public WallpaperForm ConnectedForm => MainWindow.Instance.WallpaperForms[DisplayIndex];

        public DispatcherTimer AnimatedImageSetTimer = new DispatcherTimer();

        public WallpaperWindow(Screen display, IntPtr workerw, int displayIndex)
        {
            InitializeComponent();

            _workerw = workerw;
            Display = display;
            DisplayIndex = displayIndex;

            Loaded += OnLoaded;
            Closed += OnClosed;
        }

        void OnLoaded(object s, RoutedEventArgs e)
        {
            // Sets bounds of the form
            Width = Display.Bounds.Width + ThemeUtil.Theme.Settings.WindowWidthOffset;
            Height = Display.Bounds.Height + ThemeUtil.Theme.Settings.WindowHeightOffset;
            Left = Display.Bounds.X + DisplayUtil.DisplayXAdjustment;
            Top = Display.Bounds.Y + DisplayUtil.MinDisplayY;

            //? Default, should match what's stated on the WPF
            WallpaperImage.Stretch = WallpaperMediaElement.Stretch = WallpaperMediaElementFFME.Stretch = WallpaperVlcViewBox.Stretch = Stretch.Fill; // this is actually stretch

            // This line makes the form a child of the WorkerW window, thus putting it behind the desktop icons and out of reach 
            // of any user input. The form will just be rendered, no keyboard or mouse input will reach it.
            //? (Would have to use WH_KEYBOARD_LL and WH_MOUSE_LL hooks to capture mouse and keyboard input)
            Win32.SetParent(new WindowInteropHelper(this).Handle, _workerw);

            MainWindow.Instance.OpenWinform(Display, _workerw, DisplayIndex, IncrementLoopCount);
            DisableMpv();
        }

        private void OnClosed(object sender, EventArgs e)
        {
            DisableUnusedElements(UsedElement.None, false);
            ConnectedForm.Close();
        }

        public void UpdateSize()
        {
            // Sets bounds of the form

            try
            {
                Width = Display.Bounds.Width + ThemeUtil.Theme.Settings.WindowWidthOffset;
            }
            catch (Exception) // invalid input, reset to 0
            {
                ThemeUtil.Theme.Settings.WindowWidthOffset = 0;
                Width = Display.Bounds.Width + ThemeUtil.Theme.Settings.WindowWidthOffset;
            }

            try
            {
                Height = Display.Bounds.Height + ThemeUtil.Theme.Settings.WindowHeightOffset;
            }
            catch (Exception) // invalid input, reset to 0
            {
                ThemeUtil.Theme.Settings.WindowHeightOffset = 0;
                Height = Display.Bounds.Height + ThemeUtil.Theme.Settings.WindowHeightOffset;
            }

            Left = Display.Bounds.X + DisplayUtil.DisplayXAdjustment;
            Top = Display.Bounds.Y + DisplayUtil.MinDisplayY;
        }

        //? The index is checked in ExternalWallpaperHandler now as it has access to the array, which allows wallpapers to be changed independently of one another
        public void OnWallpaperChange(BaseImageModel image, bool forceChange)
        {
            // --- If the scan is true we end this method early as the video's display time is still valid ---
            if (VerifyMinLoopMaxTimeSettings(forceChange)) return;

            LoopCount = 0; // if we are allowed to make a change, reset the loop count

            SetWallpaper(image, false);
        }

        private void SetWallpaper(BaseImageModel image, bool isAnimatedImage)
        {
            // -----Set Wallpaper -----
            if (image is ImageModel imageModel)
            {
                FileInfo wallpaperInfo;
                string wallpaperPath = imageModel.Path;

                // --- Verify Wallpaper ---
                if (!string.IsNullOrEmpty(wallpaperPath))
                {
                    wallpaperInfo = new FileInfo(wallpaperPath);
                }
                else
                {
                    Debug.WriteLine("Null Wallpaper Path found when calling OnWallpaperChange");
                    return;
                }

                Debug.WriteLine("Changing into: " + wallpaperPath);

                if (WallpaperUtil.IsSupportedVideoType_GivenExtension(wallpaperInfo.Extension) || imageModel.IsGif) //? ---- video or gif ----
                {
                    SetVideoOrGif(imageModel, wallpaperInfo, isAnimatedImage);
                }
                else //? ---- static ----
                {
                    SetStatic(imageModel.Path, isAnimatedImage);
                }

                //! keep in mind that the image set will loop around to this
                ActiveImage = imageModel; //? this change implies that the wallpaper was SUCCESSFULLY changed | Errors, video loop control, etc. can stop this
            }
            else if (image is ImageSetModel imageSet)
            {
                SetImageSet(imageSet);
            }
        }

        public async void SetVideoOrGif(ImageModel image, FileInfo wallpaperInfo, bool isAnimatedImage)
        {
            UpdateVolume(image); //! Do NOT use ActiveImage here, it is not set until the end of the method!

            if (WallpaperUtil.IsSupportedVideoType_GivenExtension(wallpaperInfo.Extension))
            {
                ConnectedForm.Enabled = true;
                ConnectedForm.Visible = true;
                //xConnectedForm.BringToFront();
                ConnectedForm.SetWallpaper(image);
                DisableUnusedElements(UsedElement.Mpv, isAnimatedImage);
            }
            else if (/*wallpaperInfo.Extension == ".webm" ||*/ image.IsGif) //? FFME can't handle .avi files and crashes on some videos depending on their pixel format, this seems to be more common with .webms
            {
                Debug.WriteLine("FFME (Recording path for just in case of crash, convert crashed .webms to .mp4): " + image.Path);

                try
                {
                    await WallpaperMediaElementFFME.Close();
                    await WallpaperMediaElementFFME.Open(new Uri(wallpaperInfo.FullName));
                    WallpaperMediaElementFFME.IsEnabled = true;
                    WallpaperMediaElementFFME.Visibility = Visibility.Visible;

                    DisableUnusedElements(UsedElement.FFME, isAnimatedImage);

                    RetryMediaOpen(false, isAnimatedImage); //? If there's too much load on the system FFME media will fail to start and need to be re-initialized
                }
                catch (Exception)
                {
                    Debug.WriteLine("Failed FFME Open: " + image.Path);
                }

            }
            else //? Use the MediaElement as a last resort | seems to handle .wmv just fine
            {
                WallpaperMediaElement.Close();
                WallpaperMediaElement.Source = new Uri(wallpaperInfo.FullName);
                WallpaperMediaElement.IsEnabled = true;
                WallpaperMediaElement.Visibility = Visibility.Visible;

                DisableUnusedElements(UsedElement.MediaElement, isAnimatedImage);
            }
        }

        public async void SetStatic(string wallpaperPath, bool isAnimatedImage)
        {
            await Task.Factory.StartNew(() =>
            {
                BitmapImage bitmap = new BitmapImage();
                //xusing (FileStream stream = File.OpenRead(image.Path))
                //x{
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                //xbitmap.StreamSource = stream;
                bitmap.UriSource = new Uri(wallpaperPath); // supposedly Uri is asynchronous, test the speed between UriSource & StreamSource, but also test their memory handling
                bitmap.EndInit();
                //? this should also allow the bitmap to be picked up by the automatic garbage collection (so don't GC manually)
                bitmap.Freeze(); // this needs to be frozen before the bitmap is used in the UI thread, call this right after bitmap.EndInit() | https://stackoverflow.com/questions/46709382/async-load-bitmapimage-in-c-sharp

                Dispatcher.Invoke(() =>
                {
                    WallpaperImage.Source = bitmap;
                    WallpaperImage.IsEnabled = true;
                    WallpaperImage.Visibility = Visibility.Visible;
                });
            }, TaskCreationOptions.LongRunning);

            DisableUnusedElements(UsedElement.Image, isAnimatedImage);
        }

        public void SetImageSet(ImageSetModel imageSet)
        {
            switch (imageSet.SetType)
            {
                case ImageSetType.Alt:

                    BaseImageModel wallpaper = WallpaperRandomizationController.GetRandomImageFromPreset(imageSet.GetRelatedImages(), imageSet.ImageType, true);

                    if (wallpaper is ImageModel wallpaperImageModel)
                    {
                        SetWallpaper(wallpaperImageModel, false);
                    }

                    break;

                case ImageSetType.Animate:
                    
                    AnimatedSetIndex = 0;
                    SetWallpaper(imageSet.GetRelatedImage(AnimatedSetIndex), true); //? starting on the first index

                    double intervalTime = 0;
                    int displayTimerMax = WallpaperFluxViewModel.Instance.DisplaySettings[DisplayIndex].DisplayTimerMax;
                    int relatedImageLength = imageSet.GetRelatedImages(true).Length;

                    if (imageSet.FractionIntervals)
                    {
                        intervalTime = displayTimerMax / (double)relatedImageLength;
                        intervalTime /= imageSet.Speed;
                    }

                    if (imageSet.StaticIntervals) intervalTime = imageSet.Speed;

                    if (intervalTime == 0) intervalTime = displayTimerMax; //? will end up just playing one image from the set

                    if (imageSet.WeightedIntervals)
                    {
                        double totalTime = intervalTime * relatedImageLength;

                        // TODO Get average rank
                        // TODO Get weighting and multiply by max rank
                        // TODO Divide the values to get the weight multiple
                        // TODO Multiply weight by given interval

                        // TODO Will update with each call of AdvanceAnimatedImageSet()
                    }

                    AnimatedImageSetTimer = new DispatcherTimer(DispatcherPriority.Background, Application.Current.Dispatcher); //? resetting events
                    AnimatedImageSetTimer.Interval = TimeSpan.FromSeconds(intervalTime);
                    AnimatedImageSetTimer.Tick += (sender, e) => AdvanceAnimatedImageSet(imageSet);
                    AnimatedImageSetTimer.Start();

                    break;

                case ImageSetType.Merge:
                    //! If you don't use SetStatic, add this: DisableUnusedElements(UsedElement.Image);

                    MessageBoxUtil.ShowError("Merge Sets have yet to be implemented");
                    break;
            }
        }

        private void AdvanceAnimatedImageSet(ImageSetModel set)
        {
            if (set.SetType != ImageSetType.Animate) return;

            AnimatedSetIndex++;
            if (AnimatedSetIndex >= set.GetRelatedImages().Length)
            {
                AnimatedSetIndex = 0;
            }

            if (AnimatedSetIndex >= set.GetRelatedImages().Length - 1)
            {
                LoopCount++;
            }

            SetWallpaper(set.GetRelatedImage(AnimatedSetIndex), true);
        }

        /// <summary>
        /// Scan video wallpapers for loop & max time settings
        /// </summary>
        /// <param name="forceChange"></param>
        /// <returns></returns>
        private bool VerifyMinLoopMaxTimeSettings(bool forceChange)
        {
            if (ActiveImage == null) return false;

            bool animatedSet = ActiveImage.IsInImageSet && ActiveImage.ParentImageSet.IsAnimated;
            bool validImage = ActiveImage.IsAnimated || animatedSet;

            if (!forceChange && validImage) // we can only make these checks if the previous wallpaper was a video or gif
            {
                int minLoops = ActiveImage.OverrideMinimumLoops ? ActiveImage.MinimumLoops : ThemeUtil.VideoSettings.MinimumLoops;
                int maxTime = ActiveImage.OverrideMaximumTime ? ActiveImage.MaximumTime : ThemeUtil.VideoSettings.MaximumTime;

                Debug.WriteLine("LoopCount: " + LoopCount + " | MinimumVideoLoops: " + minLoops);
                if (LoopCount < minLoops)
                {
                    //? we will only check for the video time condition if we have not yet gone beyond the Minimum Loop count
                    //? essentially, changes are only allowed if we are both above the minimum loop count AND the max video time

                    //! a test countermeasure against failed loads never looping
                    //xif (WallpaperMediaElement.IsLoaded) WallpaperMediaElement.Play();
                    //xif (WallpaperMediaElementFFME.IsLoaded) await WallpaperMediaElementFFME.Play();
                    //! a test countermeasure against failed loads never looping

                    Debug.WriteLine("Max Video Time: " + maxTime);
                    Debug.WriteLine("Media: " + WallpaperMediaElement.Position.Seconds + " | FFME: " + WallpaperMediaElementFFME.Position.Seconds);
                    if (WallpaperMediaElement.Position.Seconds <= maxTime ||
                        WallpaperMediaElementFFME.Position.Seconds <= maxTime)
                    {
                        // keep the current wallpaper
                        ThemeUtil.Theme.WallpaperRandomizer.ActiveWallpapers[WallpaperWindowUtil.GetWallpaperIndex(this)] = ActiveImage;
                        return true;
                    }
                }
            }

            if (animatedSet) DisableSet();

            //xvlcStopwatch.Reset(); // for situations where the next wallpaper is not a VLC wallpaper
            return false;
        }

        #region Disable Unused
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

        public void DisableMpv()
        {
            ConnectedForm.Enabled = false;
            ConnectedForm.Visible = false;
            ConnectedForm.StopMpv();
        }

        public void DisableSet() => AnimatedImageSetTimer.Stop();

        private void DisableUnusedElements(UsedElement usedElement, bool isAnimatedImage)
        {
            if (usedElement != UsedElement.Image) DisableImage();
            if (usedElement != UsedElement.MediaElement) DisableMediaElement();
            if (usedElement != UsedElement.FFME) DisableFFME();
            if (usedElement != UsedElement.Mpv) DisableMpv();
            if (!isAnimatedImage) DisableSet();
        }
        #endregion

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

                ConnectedForm.SetWallpaperStyle(style);
            });
        }

        private void WallpaperMediaElement_OnMediaEnded(object sender, RoutedEventArgs e)
        {
            LoopCount++;

            if (sender is MediaElement element)
            {
                element.Position = TimeSpan.Zero;
                element.Play();
            }
        }

        private void WallpaperMediaElementFFME_OnMediaEnded(object? sender, EventArgs e) => LoopCount++;

        #region Volume
        public void Mute()
        {
            UpdateVolume(ActiveImage);
            ConnectedForm.Mute();
        }

        public void Unmute()
        {
            UpdateVolume(ActiveImage);
            ConnectedForm.Unmute();
        }

        public void UpdateVolume()
        {
            UpdateVolume(ActiveImage);
        }

        public void UpdateVolume(BaseImageModel image)
        {
            if (ActiveImage is ImageModel imageModel)
            {
                UpdateVolume(imageModel);
            }
        }

        private async void UpdateVolume(ImageModel image)
        {
            await Dispatcher.Invoke(async () =>
            {
                if (!AudioManager.IsWallpapersMuted)
                {
                    int repeatInterval = 100;

                    if (image != null) //? it's okay to set the volume to 0 ahead of time, but sometimes the given image may not be initialized
                    {
                        if (!image.IsVideo) return;

                        if (WallpaperUtil.IsSupportedVideoType(image.Path))
                        {
                            ConnectedForm.UpdateVolume(image);
                        }
                        else
                        {
                            if (Math.Abs(WallpaperMediaElement.Volume - (image.Volume / 100)) > 0.00001 || Math.Abs(WallpaperMediaElementFFME.Volume - (image.Volume / 100)) > 0.00001)
                            {
                                WallpaperMediaElement.Volume = WallpaperMediaElementFFME.Volume = image.Volume;

                                //! Debug fix to volume failing to update
                                await Task.Factory.StartNew(() =>
                                {
                                    Thread.Sleep(repeatInterval);

                                    Dispatcher.Invoke(() =>
                                    {
                                        WallpaperMediaElement.Volume = WallpaperMediaElementFFME.Volume = image.Volume / 100;
                                        Debug.WriteLine("MediaElement Volume: " + WallpaperMediaElement.Volume);
                                        Debug.WriteLine("FFME Volume: " + WallpaperMediaElementFFME.Volume);

                                        if (Math.Abs(WallpaperMediaElement.Volume - (image.Volume / 100)) > 0.00001 ||
                                            Math.Abs(WallpaperMediaElementFFME.Volume - (image.Volume / 100)) > 0.00001)
                                            UpdateVolume();
                                    });
                                }, TaskCreationOptions.LongRunning);
                            }
                        }
                    }
                }
                else
                {
                    //? mute regardless of whether or not there is a valid image

                    WallpaperMediaElement.Volume = WallpaperMediaElementFFME.Volume = 0;
                }
            });
        }
        #endregion

        private async void RetryMediaOpen(bool finalAttempt, bool isAnimatedImage)
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
                        DisableUnusedElements(UsedElement.FFME, isAnimatedImage);

                        if (!finalAttempt)
                        {
                            //WallpaperMediaElementFFME.Open(new Uri(image.Path));
                            RetryMediaOpen(true, isAnimatedImage);
                        }
                        //}
                    }
                });
            });
        }

        public void IncrementLoopCount() => LoopCount++;
    }
}
