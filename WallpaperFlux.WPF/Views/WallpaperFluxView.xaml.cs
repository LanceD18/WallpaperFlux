using System;
using System.Collections.Generic;
using System.Configuration.Internal;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using HanumanInstitute.MediaPlayer.Wpf.Mpv;
using LanceTools.IO;
using MediaToolkit;
using MediaToolkit.Model;
using MediaToolkit.Options;
using Microsoft.VisualBasic.FileIO;
using Microsoft.WindowsAPICodePack.Shell;
using MvvmCross.Base;
using MvvmCross.Binding.Extensions;
using MvvmCross.Platforms.Wpf.Presenters.Attributes;
using MvvmCross.Platforms.Wpf.Views;
using MvvmCross.ViewModels;
using Unosquare.FFME.Common;
using WallpaperFlux.Core.Models;
using WallpaperFlux.Core.Models.Controls;
using WallpaperFlux.Core.Models.Theme;
using WallpaperFlux.Core.Util;
using WallpaperFlux.Core.ViewModels;
using WallpaperFlux.WPF.Util;
using WallpaperFlux.WPF.Windows;
using WpfAnimatedGif;
using WpfScreenHelper;
using ControlUtil = WallpaperFlux.WPF.Util.ControlUtil;
using Image = System.Windows.Controls.Image;
using Point = System.Windows.Point;
using Size = System.Windows.Size;

namespace WallpaperFlux.WPF.Views
{

    /// <summary>
    /// Interaction logic for WallpaperFluxView.xaml
    /// </summary>
    [MvxContentPresentation]
    [MvxViewFor(typeof(WallpaperFluxViewModel))]
    public partial class WallpaperFluxView : MvxWpfView
    {
        //! don't actually outright kill the thread, find a more graceful solutions:
        //! https://stackoverflow.com/questions/14817427/how-to-stop-threads
        //! https://josipmisko.com/posts/c-sharp-stop-thread
        //! https://stackoverflow.com/questions/7834351/gracefully-shutdown-a-thread
        //! https://stackoverflow.com/questions/17095696/how-do-i-end-a-thread-gracefully-at-the-point-when-the-calling-process-exits-or

        public WallpaperFluxView()
        {
            InitializeComponent();
            ToolTipThumbnailImage = new Image();
            ToolTipFfmeMediaElement = new Unosquare.FFME.MediaElement();
        }

        private void WallpaperFluxView_OnSizeChanged_UpdateInspectorHeight(object sender, SizeChangedEventArgs e) => WallpaperFluxViewModel.Instance.SetInspectorHeight(ActualHeight - 75);

        #region MediaElement & Images

        private void Inspector_OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            UnloadMediaElement(sender);

            LoadImageOrMediaElementOrMpvPlayerHost(sender);
        }

        private void Tooltip_MediaElement_OnLoaded(object sender, RoutedEventArgs e) => LoadImageOrMediaElementOrMpvPlayerHost(sender);

        private void Tooltip_MediaElement_OnUnloaded(object sender, RoutedEventArgs e) => UnloadMediaElement(sender);

        private void LoadImageOrMediaElementOrMpvPlayerHost(object sender)
        {
            if (sender is Image image)
            {
                LoadImage(image, false, image);
            }
            else if (sender is MediaElement element)
            {
                LoadMediaElement(element);
            }
            else if (sender is Unosquare.FFME.MediaElement elementFFME)
            {
                LoadFFMEMediaElement(elementFFME);
            }
            else if (sender is MpvPlayerHost mpvPlayerHost)
            {
                LoadMpvPlayerHost(mpvPlayerHost);
            }
        }

        private void UnloadMediaElement(object sender)
        {
            try
            {
                if (sender is MediaElement element)
                {
                    element.Stop();
                    element.Close();
                    element.ClearValue(MediaElement.SourceProperty);
                    element.Source = null;
                }

                if (sender is Unosquare.FFME.MediaElement elementFFME)
                {
                    elementFFME.Stop();
                    elementFFME.Close();
                    elementFFME.ClearValue(MediaElement.SourceProperty);
                }
                //! Dispose() will freeze the program
                //xelement?.Dispose();
            }
            catch (Exception exception)
            {
                Console.WriteLine("ERROR: Element Unload Failed: " + exception);
            }
        }

        private async void LoadImage(Image image, bool highQuality, Image imageWithContext)
        {
            ImageModel thumbnailSource = imageWithContext.DataContext switch
            {
                ImageModel imageModel => imageModel,
                ImageSetModel imageSet => imageSet.GetHighestRankedImage(),
                _ => null
            };

            if (thumbnailSource == null) return; // ! failed

            await Task.Run(() =>
            {
                if (!FileUtil.Exists(thumbnailSource.Path)) return;

                try
                {
                    LoadBitmapImage(image, thumbnailSource.IsGif/*x, highQuality*/, path: thumbnailSource.Path);
                }
                catch (Exception e)
                {
                    Debug.WriteLine("ERROR: Image Loading Failed: " + e);
                }
            }).ConfigureAwait(false);
        }

        private void LoadMediaElement(MediaElement element)
        {
            if (element.DataContext is ImageModel elementImage)
            {
                try
                {
                    element.Source = new Uri(elementImage.Path);
                }
                catch (Exception e)
                {
                    Debug.WriteLine("ERROR: Element Loading Failed: " + e);
                }
            }
        }

        private void LoadFFMEMediaElement(Unosquare.FFME.MediaElement element)
        {
            if (element.DataContext is ImageModel elementImage)
            {
                try
                {
                    element.Open(new Uri(elementImage.Path));
                }
                catch (Exception e)
                {
                    Debug.WriteLine("ERROR: Element Loading Failed: " + e);
                }
            }
        }

        private void LoadMpvPlayerHost(MpvPlayerHost mpvPlayerHost)
        {
            if (mpvPlayerHost.DataContext is ImageModel elementImage)
            {
                mpvPlayerHost.Player.Load(elementImage.Path);
            }
        }

        //? https://stackoverflow.com/questions/8352787/how-to-free-the-memory-after-the-bitmapimage-is-no-longer-needed
        //? https://stackoverflow.com/questions/2631604/get-absolute-file-path-from-image-in-wpf
        //? prevents the application from continuing to 'use' the image after loading it in, which also saves some memory
        private void LoadBitmapImage(Image image, bool isGif, string path = "", int decodePixelHeight = 0, MemoryStream ms = null)
        {
            // TODO THIS METHOD IS BEING CALLED MULTIPLE TIMES PER INSPECTOR SWITCH, FIX
            try // ! this can accidentally fire off multiple times and cause crashes when trying to load videos
            {
                BitmapImage bitmap = new BitmapImage();

                // --- Begin Init ---
                bitmap.BeginInit();
                if (ms != null) ms.Seek(0, SeekOrigin.Begin);
                if (decodePixelHeight != 0) bitmap.DecodePixelHeight = decodePixelHeight; // ! only set either Height of Width (preferably whichever is larger) to prevent stretching
                //xif (!highQuality) RenderOptions.SetBitmapScalingMode(bitmap, BitmapScalingMode.LowQuality);
                RenderOptions.SetBitmapScalingMode(bitmap, BitmapScalingMode.LowQuality); // ! high quality does not really matter for thumbnails

                // ! ignoring cache & OnDemand will break videos
                bitmap.CreateOptions = BitmapCreateOptions.IgnoreColorProfile; // to help with performance
                bitmap.CacheOption = BitmapCacheOption.OnLoad; // ! OnLoad is required for videos

                if (ms != null)
                {
                    bitmap.StreamSource = ms;
                }
                else if (path != "")
                {
                    bitmap.UriSource = new Uri(path); //? uri-source loads the image asynchronously
                }

                bitmap.EndInit();
                bitmap.Freeze(); // prevents unnecessary copying: https://stackoverflow.com/questions/799911/in-what-scenarios-does-freezing-wpf-objects-benefit-performance-greatly
                // --- End Init (Freeze) ---


                //! await Task.Run() will be used outside of this method to capture the Bitmap within the 'calling thread'
                Dispatcher.Invoke(() => // the image must be called on the UI thread which the dispatcher helps us do under this other thread
                {
                    if (isGif)
                    {
                        ImageBehavior.SetAnimatedSource(image, bitmap);
                    }
                    else
                    {
                        image.Source = bitmap;
                    }

                    if (ms != null)
                    {
                        ms.Close();
                        ms.Dispose();
                    }
                });
            }
            catch (Exception e)
            {
                Debug.WriteLine("ERROR: Bitmap Loading Failed: " + e);
            }
        }

        //? by default the image's DPI is used, causing some tooltips to be incredibly small
        //? this fix uses the pixel size instead, bypassing the DPI issue
        private void Tooltip_Image_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is Image image)
            {
                LoadTooltip(ToolTipThumbnailImage);
            }
            else
            {
                Debug.WriteLine("Image failed to send");
            }
        }

        private async void LoadTooltip(Image image)
        {
            //ximage.BeginInit();

            LoadImage(image, true, ThumbnailImage);

            Point mousePos = PointToScreen(Mouse.GetPosition(this));
            Rect mouseRect = new Rect(mousePos, mousePos);

            int wallpaperIndex = -1;
            Screen[] displays = DisplayUtil.Displays.ToArray();
            for (int i = 0; i < DisplayUtil.Displays.Count(); i++)
            {
                if (displays[i].WorkingArea.IntersectsWith(mouseRect))
                {
                    wallpaperIndex = i;
                }
            }

            try // for just in case the casting fails, we can just use the normal size
            {
                async void AttemptSetSize(int retries = 0)
                {
                    if (retries > 3) return; // tried too many times, just return

                    if (wallpaperIndex != -1 && image.Source != null)
                    {
                        int pxWidth = ((BitmapSource)image.Source).PixelWidth;
                        int pxHeight = ((BitmapSource)image.Source).PixelHeight;

                        double wallWidth = MainWindow.Instance.Wallpapers[wallpaperIndex].Width;
                        double wallHeight = MainWindow.Instance.Wallpapers[wallpaperIndex].Height;
                        //xDebug.WriteLine("Pixel Width: " + pxWidth);
                        //xDebug.WriteLine("Pixel Height: " + pxHeight);

                        bool oversizedWidth = pxWidth > wallWidth;
                        bool oversizedHeight = pxHeight > wallHeight;

                        // dynamically resize based on the ratio of the oversized dimension
                        if (oversizedWidth || oversizedHeight) //? ratios are only touched on if one size extends beyond the dimensions of the monitor screen
                        {
                            double ratio;

                            if (pxWidth > pxHeight) // the larger value indicates which direction the image will stretch, indicating which direction we need to shorten
                            {
                                ratio = wallHeight / pxWidth;
                            }
                            else
                            {
                                ratio = wallHeight / pxHeight;
                            }

                            pxWidth = (int)(pxWidth * ratio);
                            pxHeight = (int)(pxHeight * ratio);
                        }

                        image.Width = pxWidth;
                        image.Height = pxHeight;
                        //xDebug.WriteLine("Image Width: " + image.Width); 
                        //xDebug.WriteLine("Image Height: " + image.Height);
                    }
                    else
                    {
                        Debug.WriteLine("Potential invalid image source, try again");

                        await Task.Run(() =>
                        {
                            Thread.Sleep((int)Math.Pow(10, retries + 1)); // increment the delay so that we don't spam retries

                            Dispatcher.Invoke(() =>
                            {
                                AttemptSetSize(++retries); //? sometimes the image source won't load fast enough and we'll need to try again
                            });
                        });
                    }
                }

                // start attempts
                //xAttemptSetSize();
            }
            catch (Exception exception)
            {
                Debug.WriteLine("Tooltip Resizing Failed: " + exception);
            }

            //ximage.EndInit();
            //ximage.UpdateLayout();
        }

        private void Tooltip_Image_OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (sender is Image image)
            {
                UnloadImage(image);
            }
        }

        private void Image_OnLoaded(object sender, RoutedEventArgs e) => LoadImageOrMediaElementOrMpvPlayerHost(sender);
        
        private void Image_OnLoaded_LowQuality(object sender, RoutedEventArgs e)
        {
            ImageModel imageModel = null;
            if (sender is Image image)
            {
                imageModel = image.DataContext switch
                {
                    ImageModel regularImageModel => regularImageModel,
                    ImageSetModel imageSetModel => imageSetModel.GetHighestRankedImage(),
                    _ => null
                };

                if (imageModel != null)
                {
                    Thread thread = new Thread(() => //? this cannot thread over the if statement while the Image object is present
                    {
                        // ? don't load gifs on low quality
                        LoadBitmapImage(image, false/*x, false*/, imageModel.Path, imageModel.ImageSelectorThumbnailHeight);
                        /*x
                        try //? this can accidentally fire off multiple times and cause crashes when trying to load videos (Who still need this for some reason?)
                        {
                            BitmapImage bitmap = new BitmapImage();
                            //xstring path = imageModel.Path;
                            //xFileStream stream = File.OpenRead(path);

                            bitmap.BeginInit();
                            RenderOptions.SetBitmapScalingMode(bitmap, BitmapScalingMode.LowQuality);
                            bitmap.DecodePixelHeight = imageModel.ImageSelectorThumbnailHeight; //! Only set either Height or Width (preferably the larger variant?) to prevent awful stretching!
                                                                                                //x bitmap.DecodePixelWidth = imageModel.ImageSelectorThumbnailWidth;
                            bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache; // to help with performance
                            bitmap.CacheOption = BitmapCacheOption.None;
                            bitmap.UriSource = new Uri(imageModel.Path);
                            bitmap.EndInit();
                            bitmap.Freeze(); // prevents unnecessary copying: https://stackoverflow.com/questions/799911/in-what-scenarios-does-freezing-wpf-objects-benefit-performance-greatly
                                             //xstream.Close();
                                             //xstream.Dispose();

                            Dispatcher.Invoke(() => image.Source = bitmap); // the image must be called on the UI thread which the dispatcher helps us do under this other thread
                        }
                        catch (Exception exception)
                        {
                            Console.WriteLine("ERROR: Bitmap Creation Failed: " + exception);
                            //x throw;
                        }
                        */
                    });

                    thread.IsBackground = true; // stops the thread from continuing to run on closing the application
                    thread.Start();
                }
            }
        }

        private void Image_OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (sender is Image image)
            {
                UnloadImage(image);
            }
        }
        
        private void ConvertBitmapToThumbnailBitmapImage(Image img, Bitmap bitmap, ImageModel imageModel)
        {

            using (MemoryStream ms = new MemoryStream())
            {
                bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
                LoadBitmapImage(img, false/*x, false*/, decodePixelHeight: imageModel.ImageSelectorThumbnailHeight, ms: ms);

                /*x
                BitmapImage image = new BitmapImage();

                image.BeginInit();
                ms.Seek(0, SeekOrigin.Begin);
                RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.LowQuality);
                image.DecodePixelHeight = imageModel.ImageSelectorThumbnailHeight; //! Only set either Height or Width (preferably the larger variant?) to prevent awful stretching!
                image.CreateOptions = BitmapCreateOptions.IgnoreColorProfile; // to help with performance
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = ms;
                image.EndInit();
                image.Freeze();

                return image;
                */
            }
        }

        //? https://stackoverflow.com/questions/3024169/capture-each-wpf-mediaelement-frame - [Go down below the answer for stuff that doesn't seem to use an extension]
        //? https://stackoverflow.com/questions/35380868/extract-frames-from-video-c-sharp - Media Toolkit [LOTS OF ADDITIONAL SOLUTIONS BENEATH TOP ONE]
        private void MediaElement_OnLoaded_GenerateVideoThumbnail(object sender, RoutedEventArgs e)
        {
            if (sender is Image { DataContext: ImageModel imageModel } image)
            {
                Thread thread = new Thread(() =>
                {
                    using (ShellFile shellFile = ShellFile.FromFilePath(imageModel.Path))
                    {
                        using (Bitmap bm = shellFile.Thumbnail.Bitmap)
                        {
                            ConvertBitmapToThumbnailBitmapImage(image, bm, imageModel);

                            /*x
                            BitmapImage bitmapImage = ConvertBitmapToThumbnailBitmapImage(bm, imageModel);
                            Dispatcher.Invoke(() => { return image.Source = bitmapImage; }); // the image must be called on the UI thread which the dispatcher helps us do under this other thread
                            */
                        }
                    }
                });

                // TODO Apply IsBackground to more threads
                thread.IsBackground = true; //? stops the thread from continuing to run on closing the application
                thread.Start();
            }
        }

        private void MediaElement_OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (sender is Image image)
            {
                UnloadImage(image);
            }
        }

        /*x
        private void MpvPlayerHost_OnLoaded(object sender, RoutedEventArgs e) => LoadImageOrMediaElementOrMpvPlayerHost(sender);

        private void MpvPlayerHost_OnUnloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                //! there's also a .Dispose() but i'm sure that'd just delete the player, test at some point
                if (sender is MpvPlayerHost mpvPlayerHost) mpvPlayerHost.Stop();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }
        */

        private void UnloadImage(Image image)
        {
            image.Source = null;
            BindingOperations.ClearAllBindings(image);
            image = null;

            // ! controls will not dispose if any events are still remaining or the control is not null
            // x https://stackoverflow.com/questions/19222400/wpf-user-control-not-being-disposed
            /*x
            image.Loaded -= Tooltip_Image_OnLoaded;
            image.Unloaded -= Tooltip_Image_OnUnloaded;
            BindingOperations.ClearAllBindings(image);
            image = null;
            */

            /*x
            UpdateLayout();

            Thread t = new Thread(new ThreadStart(delegate
            {
                Thread.Sleep(500);
                GC.Collect(); //! This will cause delays regardless of the thread existing
            }));
            t.Start();
            */
        }

        #endregion

        #region Child Window Control
        private void MenuItem_OpenTagWindow_Click(object sender, RoutedEventArgs e) => WindowUtil.PresentTagView(TagPresenter_ViewWindow_Closed_DrawerFix);

        private void MenuItem_MoreSettings_Click(object sender, RoutedEventArgs e) => WindowUtil.PresentSettingsView(SettingsPresenter_ViewWindow_Closed_DrawerFix);

        private void MenuItem_OpenPaginationTest_Click(object sender, RoutedEventArgs e) => WindowUtil.PresentPaginationTestView();

        /// <summary>
        /// Disables the TagBoard on closing the view
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //? Prevents the TagBoard from causing a crash the next time the tag view is opened if the tag view is closed with the TagBoard open
        private void TagPresenter_ViewWindow_Closed_DrawerFix(object sender, EventArgs e)
        {
            TagViewModel.Instance.CloseTagBoard();
            TagViewModel.Instance.CloseFolderPriority();
            TagViewModel.Instance.CloseRankGraph();
        }

        /// <summary>
        /// Disables the Settings Window on closing the view
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //? Prevents the TagBoard from causing a crash the next time the tag view is opened if the tag view is closed with the TagBoard open
        private void SettingsPresenter_ViewWindow_Closed_DrawerFix(object sender, EventArgs e)
        {
            SettingsViewModel.Instance.CloseRankGraph();
        }

        #endregion

        #region Image Selector

        private void ImageSetListBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => TaggingUtil.HighlightTags();

        //? Now that the window scales dynamically you probably won't need font scaling but keep this consideration in mind
        private void ImageSelectorTabListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //x Debug.WriteLine("Image Selection changed"); [This debug statement will cause lag on large selections]
            if (!WallpaperFluxViewModel.Instance.TogglingAllSelections)
            {
                if (e.AddedItems.Count >= 1) // TODO Make this apply to actually 1 image once you fix the other issues with selecting between pages
                {
                    ControlUtil.EnsureSingularSelection<ImageSelectorTabModel, BaseImageModel>(ImageSelectorTabControl.Items, ImageSelectorTabControl.SelectedItem as ITabModel<BaseImageModel>);
                }

                TaggingUtil.HighlightTags();
            }

            /*x
            while (_activeThumbnailThreads.Count > 0)
            {
                _activeThumbnailThreads[0].Interrupt(); // ! don't use abort, it is unsafe
                _activeThumbnailThreads.RemoveAt(0);
            }
            */

            /*x
            // Font Scaling
            if (e.AddedItems.Count > 0)
            {
                if (e.AddedItems.ElementAt(0) is ImageModel imageModel)
                {
                    string path = imageModel.Path;

                    //xSelectedImagePathTextBox.Text = path;

                    Size dimensions;
                    if (!imageModel.IsVideo)
                    {
                        System.Drawing.Image image = System.Drawing.Image.FromFile(path); // TODO The ExternalDisplayUtil can handle this now
                        dimensions = new Size(image.Width, image.Height);
                        image.Dispose();

                        //xSelectedImageDimensionsTextBox.Text = dimensions.Width + "x" + dimensions.Height;
                    }
                    else
                    {
                        // TODO Figure out how to gather the video dimensions (With the below method the dimensions never load in time, or seemingly don't load at all)
                        ///
                        /// MediaElement element = new MediaElement();
                        /// await element.Open(new Uri(path));
                        /// Bitmap bitmap = await element.CaptureBitmapAsync();

                        /// dimensions = new Size(bitmap.Width, bitmap.Height);
                        /// await element.Close();
                        ///

                        //xSelectedImageDimensionsTextBox.Text = "";
                    }
                }
            }
            */
        }

        //? it's a bit clunky to introduce a variable for the width of each individual window  but it works
        //? alternative options were hard to find due to the structure of this segment
        private void ImageSelectorTabControl_OnSizeChanged(object sender, SizeChangedEventArgs e) => UpdateImageSelectorTabWrapperWidth();

        private void ImageSelectorTabControl_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            /*x
            Debug.WriteLine(sender.GetType());
            if (sender is TabControl { SelectedItem: ImageSelectorTabModel tab })
            {
                WallpaperFluxViewModel.Instance.VerifyImageSelectorTab(tab);
            }
            */

            UpdateImageSelectorTabWrapperWidth();

            /*x
            // close all active thumbnail threads
            while (_activeThumbnailThreads.Count > 0)
            {
                Thread thread = _activeThumbnailThreads[0];
                _activeThumbnailThreads.Remove(thread);
                if (thread.IsAlive)
                {
                    //? create an empty thread to force the thread to stop on restart | works better than Abort() or Interrupt()
                    //? this prevents large thumbnails, or primarily videos generating their thumbnails, to hang up the thumbnail generation of the image selector
                    thread = new Thread(() => { });
                    thread.Start();
                }
            }
            */
        }

        private void UpdateImageSelectorTabWrapperWidth()
        {
            WallpaperFluxViewModel viewModel = (WallpaperFluxViewModel)this.DataContext;
            if (viewModel.SelectedImageSelectorTab != null)
            {
                viewModel.SelectedImageSelectorTab.ImageSelectorTabWrapWidth = ImageSelectorTabControl.ActualWidth;
                viewModel.SelectedImageSelectorTab.RaisePropertyChanged(() => viewModel.SelectedImageSelectorTab.ImageSelectorTabWrapWidth);
            }
        }
        #endregion

        private void MediaElement_OnMediaEnded(object sender, RoutedEventArgs e) //? for Window media element only
        {
            if (sender is MediaElement element)
            {
                element.Position = TimeSpan.FromSeconds(0);
                element.Play();
            }
        }

        private void ContextMenuListBox_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Set the Handled property to true to prevent the context menu from closing
            e.Handled = true;
        }

        private void FrameworkElement_DisposeOnUnloaded(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                BindingOperations.ClearAllBindings(element);
                element = null;

                // ! controls will not dispose if any events are still remaining or the control is not null
                // x https://stackoverflow.com/questions/19222400/wpf-user-control-not-being-disposed
                /*x
                image.Loaded -= Tooltip_Image_OnLoaded;
                image.Unloaded -= Tooltip_Image_OnUnloaded;
                BindingOperations.ClearAllBindings(image);
                image = null;
                */
            }
        }

        private Image ToolTipThumbnailImage = new Image();
        private Image ThumbnailImage;
        private Unosquare.FFME.MediaElement ToolTipFfmeMediaElement;
        private void ImageSelector_Image_OnMouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Image image)
            {
                ThumbnailImage = image;
                ImageModel imageModel = GetImageModelFromContext(image);

                if (!imageModel.IsGif)
                {
                    image.ToolTip = ToolTipThumbnailImage;
                    LoadTooltip(ToolTipThumbnailImage);
                }
                else
                {
                    ToolTipFfmeMediaElement = new Unosquare.FFME.MediaElement();
                    ToolTipFfmeMediaElement.LoadedBehavior = MediaPlaybackState.Play;
                    ToolTipFfmeMediaElement.LoopingBehavior = MediaPlaybackState.Play;

                    image.ToolTip = ToolTipFfmeMediaElement;
                    Task.Run( async () =>
                    {
                        await ToolTipFfmeMediaElement.Open(new Uri(imageModel.Path));
                        await ToolTipFfmeMediaElement.Play();
                    }).ConfigureAwait(false);
                }
            }
        }

        private void ImaeSelector_Image_OnMouseLeave(object sender, MouseEventArgs e)
        {
            ThumbnailImage.ToolTip = null;
            ToolTipThumbnailImage.Source = null;
            ToolTipFfmeMediaElement.Stop();
            ToolTipFfmeMediaElement.Dispose();
            /*x
            Task.Run(async () =>
            {
                await ToolTipFfmeMediaElement.Stop();
                ToolTipFfmeMediaElement.Dispose();
            }).ConfigureAwait(false);
            */
        }

        private ImageModel GetImageModelFromContext(Image imageWithContext)
        {
            ImageModel thumbnailSource = imageWithContext.DataContext switch
            {
                ImageModel imageModel => imageModel,
                ImageSetModel imageSet => imageSet.GetHighestRankedImage(),
                _ => null
            };

            return thumbnailSource;
        }
        /*
                                        <Image.ToolTip>
                                            <ContentControl Content = "{Binding Mode=OneTime}" >
                                                < !--Resources-- >
                                                < ContentControl.Resources >
                                                    < !--Static-- >
                                                    < DataTemplate x:Key="StaticTemplate">
                                                        <!--
                                                        <Image Loaded = "Tooltip_Image_OnLoaded"
                                                               Unloaded="Tooltip_Image_OnUnloaded"
                                                               Stretch="Uniform">
                                                        </Image>
                                                        -->

                                                        <!--
                                                        <Image Stretch = "Uniform" >
                                                            < Image.Source >
                                                                < BitmapImage
                                                                    CacheOption="OnLoad"
                                                                    UriSource="{Binding Path, Mode=OneWay}">
                                                                </BitmapImage>
                                                            </Image.Source>
                                                        </Image>
                                                        -->
                                                    </DataTemplate>
                                                    <!-- GIF -->
                                                    <DataTemplate x:Key="GIFTemplate">
                                                        <ffme:MediaElement UnloadedBehavior = "Manual" LoadedBehavior="Play" LoopingBehavior="Play"
                                                                                        Loaded="Tooltip_MediaElement_OnLoaded" Unloaded="Tooltip_MediaElement_OnUnloaded" />
                                                    </DataTemplate>
                                                </ContentControl.Resources>
                                                <!--? Template Control -->
                                                <ContentControl.ContentTemplate>
                                                    <DataTemplate>
                                                        <!-- Default Template -->
                                                        <ContentControl Name = "cc" Content="{Binding Mode=OneTime}" 
                                                                                ContentTemplate="{StaticResource StaticTemplate}"/>
                                                        <!-- Triggers -->
                                                        <DataTemplate.Triggers>
                                                            <DataTrigger Binding = "{Binding IsGif, Mode=OneTime}" Value="True">
                                                                <!-- Sets the ContentTemplate to be GIFTemplate instead of StaticTemplate -->
                                                                <Setter TargetName = "cc" Property="ContentTemplate" Value="{StaticResource GIFTemplate}"/>
                                                            </DataTrigger>
                                                        </DataTemplate.Triggers>
                                                    </DataTemplate>
                                                </ContentControl.ContentTemplate>
                                            </ContentControl>
                                        </Image.ToolTip>


        */
    }
}
