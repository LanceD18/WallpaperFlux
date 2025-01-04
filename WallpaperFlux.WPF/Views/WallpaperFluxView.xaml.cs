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
using System.Windows.Controls.Primitives;
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
using MenuItem = System.Windows.Controls.MenuItem;
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

            InitToolTipItems();
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
                    //xelement.ClearValue(MediaElement.SourceProperty);
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
            ImageModel imageModel;
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
                        if (!imageModel.IsVideo)
                        {
                            // ? don't load gifs on low quality
                            LoadBitmapImage(image, false /*x, false*/, imageModel.Path, imageModel.ImageSelectorThumbnailHeight);
                        }
                        else
                        {
                            using (ShellFile shellFile = ShellFile.FromFilePath(imageModel.Path))
                            {
                                using (Bitmap bm = shellFile.Thumbnail.Bitmap)
                                {
                                    ConvertBitmapToThumbnailBitmapImage(image, bm, imageModel);
                                }
                            }
                        }
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
                //xelement.Position = TimeSpan.FromSeconds(0);
                element.Position = new TimeSpan(0, 0, 1);
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
        private List<Unosquare.FFME.MediaElement> ToolTipFfmeMediaElements = new List<Unosquare.FFME.MediaElement>();
        private MediaElement ToolTipMediaElement;
        private void ImageSelector_Image_OnMouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Image image)
            {
                ThumbnailImage = image;
                ImageModel imageModel = GetImageModelFromContext(image);

                if (imageModel.IsStatic)
                {
                    image.ToolTip = ToolTipThumbnailImage;
                    LoadTooltip(ToolTipThumbnailImage);
                }
                else if (imageModel.IsWebm)
                {
                    return;
                    var element = new Unosquare.FFME.MediaElement
                    {
                        LoadedBehavior = MediaPlaybackState.Play,
                        LoopingBehavior = MediaPlaybackState.Play,
                    };
                    ToolTipFfmeMediaElements.Add(element);
                    element.RendererOptions.UseLegacyAudioOut = true;
                    element.MessageLogged += ToolTipFfmeMediaElementOnMessageLogged;

                    element.Volume = imageModel.ActualVolume;
                    element.Open(new Uri(imageModel.Path));
                    element.IsEnabled = true;

                    image.ToolTip = element;
                    /*x
                    Task.Run( async () =>
                    {
                        await ToolTipFfmeMediaElement.Open(new Uri(imageModel.Path));
                        //xawait ToolTipFfmeMediaElement.ChangeMedia();
                    }).ConfigureAwait(false);
                    */
                }
                else
                {
                    ToolTipMediaElement.Volume = imageModel.ActualVolume;
                    ToolTipMediaElement.Source = new Uri(imageModel.Path);
                    ToolTipMediaElement.IsEnabled = true;

                    image.ToolTip = ToolTipMediaElement;
                }
            }
        }

        private void ImaeSelector_Image_OnMouseLeave(object sender, MouseEventArgs e)
        {
            ThumbnailImage.ToolTip = null;
            ToolTipThumbnailImage.Source = null;

            //xUnloadMediaElement(ToolTipFfmeMediaElement);

            /*x
            for (var i = 0; i < ToolTipFfmeMediaElements.Count; i++)
            {
                var element = ToolTipFfmeMediaElements[i];
                if (element.IsOpening)
                {
                    element.Stop();
                    Task.Run(() =>
                    {
                        Thread.Sleep(1000);
                        element.Close();
                    });
                }
                else
                {
                    if (element.IsOpen)
                    {
                        element.IsEnabled = false;
                        element.ClearValue(MediaElement.SourceProperty);
                        element.Close();
                    }

                    ToolTipFfmeMediaElements.RemoveAt(i);
                    i--;
                }
            }
            */

            //xToolTipFfmeMediaElement.Close();

            ToolTipMediaElement.Stop();
            ToolTipMediaElement.Close();
            ToolTipMediaElement.IsEnabled = false;
            ToolTipMediaElement.Source = null;

            //xToolTipFfmeMediaElement.Dispose();
            /*x
            Task.Run(async () =>
            {
                await ToolTipFfmeMediaElement.Stop();
                ToolTipFfmeMediaElement.Dispose();
            }).ConfigureAwait(false);
            */
        }

        private void InitToolTipItems()
        {
            ToolTipThumbnailImage = new Image();

            /*x
            ToolTipFfmeMediaElement = new Unosquare.FFME.MediaElement
            {
                LoadedBehavior = MediaPlaybackState.Play,
                LoopingBehavior = MediaPlaybackState.Play,
            };
            ToolTipFfmeMediaElement.RendererOptions.UseLegacyAudioOut = true;
            ToolTipFfmeMediaElement.MessageLogged += ToolTipFfmeMediaElementOnMessageLogged;
            */

            ToolTipMediaElement = new MediaElement
            {
                LoadedBehavior = MediaState.Play,
                UnloadedBehavior = MediaState.Manual,
            };
            ToolTipMediaElement.MediaEnded += MediaElement_OnMediaEnded;
        }

        private void ToolTipFfmeMediaElementOnMessageLogged(object sender, MediaLogMessageEventArgs e)
        {
            if (e.MessageType == MediaLogMessageType.Trace)
                return;

            Debug.WriteLine($"ToolTipFfmeMediaElement: {e.MessageType} | {e.Message}");
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

        private void ImageSelector_GroupBox_OnLoaded(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
            if (sender is GroupBox groupBox)
            {
                BaseImageModel bImageModel = groupBox.DataContext as BaseImageModel;
                ContextMenu cMenu = new ContextMenu();

                MenuItem setWallpaperItem = new MenuItem() { Header = "Set As Wallpaper", Command = bImageModel.SetWallpaperCommand };

                if (bImageModel is ImageModel imageModel)
                {
                    MenuItem openFileItem = new MenuItem() { Header = "Open File", Command = imageModel.OpenFileCommand };
                    MenuItem openFileLocationItem = new MenuItem() { Header = "Open File Location", Command = imageModel.ViewFileCommand };
                    Separator s1 = new Separator();
                    MenuItem renameItem = new MenuItem() { Header = "Rename", Command = imageModel.RenameImageCommand };
                    MenuItem moveItem = new MenuItem() { Header = "Move", Command = imageModel.MoveImageCommand };
                    MenuItem deleteItem = new MenuItem() { Header = "Delete", Command = imageModel.DeleteImageCommand };
                    //xMenuItem rankItem = new MenuItem() { Header = "Rank", Command = imageModel.RankImageCommand }; Not really needed, doesn't help with group ranking
                    Separator s2 = new Separator();
                    MenuItem setTagsToTagboardItem = new MenuItem() { Header = "Set Tags to Tagboard", Command = imageModel.SetTagsToTagBoardCommand };
                    MenuItem pasteTagboardToImageItem = new MenuItem() { Header = "Paste Tagboard to Image", Command = imageModel.PasteTagBoardCommand };
                    MenuItem pasteTagsToTagboardItem = new MenuItem() { Header = "Paste Tags to Tagboard", Command = imageModel.PasteTagsToTagBoardCommand };
                    Separator s3 = new Separator();

                    cMenu.Items.Add(openFileItem);
                    cMenu.Items.Add(openFileLocationItem);
                    cMenu.Items.Add(setWallpaperItem);
                    cMenu.Items.Add(s1);
                    cMenu.Items.Add(renameItem);
                    cMenu.Items.Add(moveItem);
                    cMenu.Items.Add(deleteItem);
                    cMenu.Items.Add(s2);
                    cMenu.Items.Add(setTagsToTagboardItem);
                    cMenu.Items.Add(pasteTagboardToImageItem);
                    cMenu.Items.Add(pasteTagsToTagboardItem);
                    cMenu.Items.Add(s3);

                    if (imageModel.IsAnimated)
                    {
                        MenuItem speedItem = new MenuItem() { Header = "Speed", Command = null };
                        MenuItem overrideMaxLoopCountItem = new MenuItem() { Header = "Override Max Loop Count", Command = null };
                        MenuItem overrideMaxVideoTimerItem = new MenuItem() { Header = "Override Max video Timer", Command = null };

                        cMenu.Items.Add(speedItem);
                        cMenu.Items.Add(overrideMaxLoopCountItem);
                        cMenu.Items.Add(overrideMaxVideoTimerItem);
                    }

                    if (imageModel.IsVideo)
                    {
                        MenuItem volumeItem = new MenuItem() { Header = "Volume", Command = null };
                        cMenu.Items.Add(volumeItem);
                    }
                }
                else if (bImageModel is ImageSetModel setModel)
                {
                    cMenu.Items.Add(setWallpaperItem);

                    MenuItem setTypeItem = new MenuItem() { Header = "Set Type" };
                    MenuItem setTypeOptionsItem = new MenuItem();

                    // ? if referencing this in the future for the enumeration, don't forget that this used to be done like this: (where extensions:Enumeration comes from LanceTools)
                    /*
                        < ComboBox ItemsSource = "{Binding Source={extensions:Enumeration {x:Type coreUtil:ImageSetType}}}"
                                               DisplayMemberPath = "Description"
                                               SelectedValue = "{Binding SetType, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"
                                               SelectedValuePath = "Value" >
                    */
                    ComboBox setTypeOptionsBox = new ComboBox() { ToolTip = "Set Type" };
                    Binding selectedValueBinding = new Binding()
                    {
                        Path = new PropertyPath(typeof(ImageSetModel).GetProperty(nameof(ImageSetModel.SetType))),
                        Mode = BindingMode.TwoWay,
                        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                    };
                    BindingOperations.SetBinding(setTypeOptionsBox, Selector.SelectedValueProperty, selectedValueBinding);
                    foreach (ImageSetType setType in Enum.GetValues(typeof(ImageSetType))) setTypeOptionsBox.Items.Add(setType);

                    setTypeOptionsItem.Header = setTypeOptionsBox;
                    setTypeItem.Items.Add(setTypeOptionsItem);

                    MenuItem setDependentOptionsItem = new MenuItem();
                    ContentControl setOptionsControl = new ContentControl();
                    ResourceDictionary setOptionsResources = new ResourceDictionary();

                    DataTemplate altTemplate = new DataTemplate();
                    DataTemplate animateTemplate = new DataTemplate();
                    DataTemplate mergeTemplate = new DataTemplate();

                    FrameworkElementFactory altItem = new FrameworkElementFactory(typeof(MenuItem));
                    altItem.SetValue(HeaderedItemsControl.HeaderProperty, "Alt");
                    FrameworkElementFactory altPanel = new FrameworkElementFactory(typeof(StackPanel));
                    altPanel.AppendChild(altItem);
                    altTemplate.VisualTree = altPanel;

                    FrameworkElementFactory animateItem = new FrameworkElementFactory(typeof(MenuItem));
                    animateItem.SetValue(HeaderedItemsControl.HeaderProperty, "Animate");
                    FrameworkElementFactory animatePanel = new FrameworkElementFactory(typeof(StackPanel));
                    animatePanel.AppendChild(animateItem);
                    animateTemplate.VisualTree = animatePanel;

                    FrameworkElementFactory mergeItem = new FrameworkElementFactory(typeof(MenuItem));
                    mergeItem.SetValue(HeaderedItemsControl.HeaderProperty, "Merge");
                    FrameworkElementFactory mergePanel = new FrameworkElementFactory(typeof(StackPanel));
                    mergePanel.AppendChild(mergeItem);
                    mergeTemplate.VisualTree = mergePanel;

                    setOptionsResources.Add(ImageSetType.Alt, altTemplate);
                    setOptionsResources.Add(ImageSetType.Animate, animateTemplate);
                    setOptionsResources.Add(ImageSetType.Merge, mergeTemplate);

                    setOptionsControl.Resources = setOptionsResources;

                    DataTemplate setOptionsContentTemplate = new DataTemplate();
                    FrameworkElementFactory contentTemplateControl = new FrameworkElementFactory(typeof(ContentControl));
                    contentTemplateControl.SetValue( ContentControl.NameProperty, "cc");
                    contentTemplateControl.SetValue(ContentControl.ContentProperty, setModel);
                    contentTemplateControl.SetValue(ContentControl.ContentTemplateProperty, altTemplate);
                    setOptionsContentTemplate.VisualTree = contentTemplateControl;

                    DataTrigger animatedTrigger = new DataTrigger();
                    Setter animatedSetter = new Setter(ContentTemplateProperty, animateTemplate, "cc");
                    animatedTrigger.Setters.Add(animatedSetter);
                    setOptionsContentTemplate.Triggers.Add(animatedTrigger);

                    DataTrigger mergeTrigger = new DataTrigger();
                    Setter mergeSetter = new Setter(ContentTemplateProperty, mergeTemplate, "cc");
                    animatedTrigger.Setters.Add(mergeSetter);
                    setOptionsContentTemplate.Triggers.Add(mergeTrigger);


                    setOptionsControl.ContentTemplate = setOptionsContentTemplate;

                    setDependentOptionsItem.Header = setOptionsControl;
                    setTypeItem.Items.Add(setDependentOptionsItem);
                    cMenu.Items.Add(setTypeItem);
                }

                groupBox.ContextMenu = cMenu;
            }
        }
    }
}

/*
                                    < MenuItem.Header >
                                        < ContentControl Content = "{Binding Mode=OneTime}" HorizontalAlignment = "Center" >
   
                                               < ContentControl.Resources >
   
                                                   < !-- ? ImageModelTemplate - Context Menu-- >
   
                                                   < DataTemplate x: Key = "ImageModelTemplate" DataType = "{x:Type models:ImageModel}" >
       
                                                           < ContextMenu >
       
                                                               < MenuItem Header = "Open File" mvx: Bi.nd = "Command OpenFileCommand" />
           
                                                                   < MenuItem Header = "Open File Location" mvx: Bi.nd = "Command ViewFileCommand" />
               
                                                                       < MenuItem Header = "Set As Wallpaper" mvx: Bi.nd = "Command SetWallpaperCommand" />
                   
                                                                           < Separator />
                   
                                                                           < MenuItem Header = "Rename" Command = "{Binding RenameImageCommand}" />
                      
                                                                              < MenuItem Header = "Move" Command = "{Binding MoveImageCommand}" />
                         
                                                                                 < MenuItem Header = "Delete" Command = "{Binding DeleteImageCommand}" />
                            
                                                                                    < MenuItem Header = "Rank" Command = "{Binding RankImageCommand}" />
                               
                                                                                       < Separator />
                               
                                                                                       < MenuItem Header = "Set Tags to Tagboard" Command = "{Binding SetTagsToTagBoardCommand}" />
                                  
                                                                                          < MenuItem Header = "Paste Tagboard to Image" Command = "{Binding PasteTagBoardCommand}" />
                                     
                                                                                             < MenuItem Header = "Paste Tags to Tagboard" Command = "{Binding PasteTagsToTagBoardCommand}" />
                                        
                                                                                                < Separator />
                                        
                                                                                                < MenuItem Header = "Enabled" StaysOpenOnClick = "True" IsCheckable = "True"
                                                                                IsChecked = "{Binding Enabled, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"
                                                                                ToolTip = "Disabling an imageSet will stop it from appearing here, 
                                                                                      if you want to see the imageSet again select Enable Detection of Inactive Images"/>
                                                        <Separator/>
                                                        <MenuItem Header="Volume" mvx:Bi.nd = "Command SetVolumeCommand"
                                                                      Visibility = "{Binding IsVideo,
                                                                Converter ={ StaticResource BooleanToVisibilityConverter}}"/>
< MenuItem Header = "Speed" mvx: Bi.nd = "Command SetSpeedCommand"
                                                                      Visibility = "{Binding IsGif,
                                                                Converter ={ StaticResource BooleanToVisibilityConverter}}"/>
< MenuItem Header = "Override Max Loop Count"
                                                                      Visibility = "{Binding IsGif,
                                                                Converter ={ StaticResource BooleanToVisibilityConverter}}"/>
< MenuItem Header = "Override Max Video Timer"
                                                                      Visibility = "{Binding IsGif, 
                                                                Converter ={ StaticResource BooleanToVisibilityConverter}}"/>
</ ContextMenu >

</ DataTemplate >


< !-- ? ImageSetModelTemplate - Context Menu-- >

< DataTemplate x: Key = "ImageSetModelTemplate" DataType = "{x:Type models:ImageSetModel}" >

< ContextMenu >

< MenuItem Header = "Set As Wallpaper" mvx: Bi.nd = "Command SetWallpaperCommand" />


< Separator />


< !--? Set Type-- >

< MenuItem Header = "Set Type" >

< MenuItem StaysOpenOnClick = "True" >

< MenuItem.Header >

< ComboBox ItemsSource = "{Binding Source={extensions:Enumeration {x:Type coreUtil:ImageSetType}}}"
                                                                              DisplayMemberPath = "Description"
                                                                              SelectedValue = "{Binding SetType, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"
                                                                              SelectedValuePath = "Value" >
                                                                        < ComboBox.ToolTip >
                                                                            < ToolTip Content = "Set Type" />
 
                                                                         </ ComboBox.ToolTip >
 
                                                                     </ ComboBox >
 
                                                                 </ MenuItem.Header >
 
                                                             </ MenuItem >
 

                                                             < MenuItem >
 
                                                                 < MenuItem.Header >
 
                                                                     < !--? /// Set-Dependent Options /// -->
                                                                    < ContentControl Content = "{Binding}" HorizontalAlignment = "Center" >
   
                                                                           < !--Resources-- >
   
                                                                           < ContentControl.Resources >
   
                                                                               < !--Remember that the Binding context here refers to what's given in the Content of the ContentTemplate, not what ReSharper says -->

                                                                            <DataTemplate x:Key = "Alt" DataType = "{x:Type models:ImageSetModel}" />
  

                                                                              < DataTemplate x: Key = "Animate" DataType = "{x:Type models:ImageSetModel}" >
      
                                                                                      < StackPanel Orientation = "Vertical" >
       

                                                                                           < MenuItem StaysOpenOnClick = "True" >
        
                                                                                                < MenuItem.Header >
        
                                                                                                    < StackPanel Orientation = "Horizontal" >
         
                                                                                                         < TextBlock Text = "Speed: " VerticalAlignment = "Center" />
            
                                                                                                            < TextBox Text = "{Binding Speed}" VerticalAlignment = "Center" >
               
                                                                                                                   < TextBox.Style >
               
                                                                                                                       < Style TargetType = "{x:Type TextBox}"
                                                                                                            BasedOn = "{StaticResource TextBoxNumInputDecimal}" />
                                                                                                    </ TextBox.Style >
                                                                                                </ TextBox >
                                                                                            </ StackPanel >
                                                                                        </ MenuItem.Header >
                                                                                    </ MenuItem >

                                                                                    < MenuItem Header = "Fraction Intervals" IsCheckable = "True" StaysOpenOnClick = "True"
                                                                                        IsChecked = "{Binding FractionIntervals}"
                                                                            ToolTip = "Interval amount is determined by the wallpaper change interval (Multiplied by Speed)" />
                                                                                    < MenuItem Header = "Static Intervals" IsCheckable = "True" StaysOpenOnClick = "True"
                                                                                        IsChecked = "{Binding StaticIntervals}"
                                                                            ToolTip = "Speed will instead represent the length of each frame" />
                                                                                    < MenuItem Header = "Weighted Intervals" IsCheckable = "True" StaysOpenOnClick = "True"
                                                                                        IsChecked = "{Binding WeightedIntervals}"
                                                                            ToolTip = "Gives each imageSet an interval weighted to their rank (Requires Static or Fraction intervals to function)" />

                                                                                    < Separator />

                                                                                    < MenuItem Header = "Override Minimum Loops" IsCheckable = "True" StaysOpenOnClick = "True"
                                                                                            IsChecked = "{Binding OverrideMinimumLoops}" />
                                                                                    < MenuItem StaysOpenOnClick = "True" IsEnabled = "{Binding OverrideMinimumLoops}" >
   
                                                                                           < MenuItem.Header >
   
                                                                                               < StackPanel Orientation = "Horizontal" >
    
                                                                                                    < TextBlock Text = "Min Loops: " VerticalAlignment = "Center" />
       
                                                                                                       < TextBox Text = "{Binding MinimumLoops}" VerticalAlignment = "Center" >
          
                                                                                                              < TextBox.Style >
          
                                                                                                                  < Style TargetType = "{x:Type TextBox}"
                                                                                                                BasedOn = "{StaticResource TextBoxNumInput}" />
                                                                                                    </ TextBox.Style >
                                                                                                </ TextBox >
                                                                                            </ StackPanel >
                                                                                        </ MenuItem.Header >
                                                                                    </ MenuItem >

                                                                                    < Separator />

                                                                                    < MenuItem Header = "Override Maximum Time" IsCheckable = "True" StaysOpenOnClick = "True"
                                                                                            IsChecked = "{Binding OverrideMaximumTime}" />
                                                                                    < MenuItem StaysOpenOnClick = "True" IsEnabled = "{Binding OverrideMaximumTime}" >
   
                                                                                           < MenuItem.Header >
   
                                                                                               < StackPanel Orientation = "Horizontal" >
    
                                                                                                    < TextBlock Text = "Max Time: " VerticalAlignment = "Center" />
       
                                                                                                       < TextBox Text = "{Binding MaximumTime}" VerticalAlignment = "Center" >
          
                                                                                                              < TextBox.Style >
          
                                                                                                                  < Style TargetType = "{x:Type TextBox}"
                                                                                                                BasedOn = "{StaticResource TextBoxNumInput}" />
                                                                                                    </ TextBox.Style >
                                                                                                </ TextBox >
                                                                                            </ StackPanel >
                                                                                        </ MenuItem.Header >
                                                                                    </ MenuItem >
                                                                                </ StackPanel >
                                                                            </ DataTemplate >

                                                                            < DataTemplate x: Key = "Merge" DataType = "{x:Type models:ImageSetModel}" />
    
                                                                            </ ContentControl.Resources >
    

                                                                            < !-- // Template Control // -->

                                                                            < ContentControl.ContentTemplate >
    
                                                                                < DataTemplate >
    
                                                                                    < !--Default Control-- >
    
                                                                                    < ContentControl Name = "cc" Content = "{Binding}" ContentTemplate = "{StaticResource Alt}" />
         
                                                                                         < !--Triggers-- >
         
                                                                                         < DataTemplate.Triggers >
         
                                                                                             < !--If IsVideo, use the VideoTemplate instead -->
                                                                                    <DataTrigger Binding="{Binding IsAnimated}" Value="True">
                                                                                        <Setter TargetName="cc" Property="ContentTemplate" Value="{StaticResource Animate}"/>
                                                                                    </DataTrigger>
                                                                                    <!-- TODO
                                                                        <DataTrigger Binding="{Binding IsMerged}" Value="True">
                                                                            <Setter TargetName="cc" Property="ContentTemplate" Value="{StaticResource RelatedImageTemplate}"/>
                                                                        </DataTrigger>
                                                                        -->
                                                                                </DataTemplate.Triggers>
                                                                            </DataTemplate>
                                                                        </ContentControl.ContentTemplate>
                                                                    </ContentControl>
                                                                </MenuItem.Header>
                                                            </MenuItem>
                                                        </MenuItem>

                                                        <Separator/>

                                                        <!--? Ranking Format -->
                                                        <MenuItem Header="Ranking Format" IsEnabled="False"/>
                                                        <MenuItem Header="Average" IsChecked="{Binding UsingAverageRank, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" 
                                                                  StaysOpenOnClick="True" IsCheckable="True"
                                                                  IsEnabled="{Binding UsingAverageRank, Mode=OneWay, UpdateSourceTrigger=PropertyChanged, 
                                                            Converter={StaticResource BooleanInverterConverter}}"/>
                                                        < MenuItem Header = "Override" IsChecked = "{Binding UsingOverrideRank, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"
                                                                  StaysOpenOnClick = "True" IsCheckable = "True"
                                                                  IsEnabled = "{Binding UsingOverrideRank, Mode=OneWay, UpdateSourceTrigger=PropertyChanged, 
                                                            Converter ={ StaticResource BooleanInverterConverter}}"/>
< MenuItem Header = "Weighted Average" IsChecked = "{Binding UsingWeightedAverage, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"
                                                                  StaysOpenOnClick = "True" IsCheckable = "True"
                                                                  IsEnabled = "{Binding UsingWeightedAverage, Mode=OneWay, UpdateSourceTrigger=PropertyChanged, 
                                                            Converter ={ StaticResource BooleanInverterConverter}}"/>
< MenuItem Header = "Weighted Override" IsChecked = "{Binding UsingWeightedOverride, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"
                                                                  StaysOpenOnClick = "True" IsCheckable = "True"
                                                                  IsEnabled = "{Binding UsingWeightedOverride, Mode=OneWay, UpdateSourceTrigger=PropertyChanged, 
                                                            Converter ={ StaticResource BooleanInverterConverter}}"/>
< MenuItem Header = "{Binding OverrideRankWeightText, Mode=OneWay, UpdateSourceTrigger=PropertyChanged}" IsEnabled = "False" />

< Slider Value = "{Binding OverrideRankWeight}" IsEnabled = "{Binding UsingWeightedOverride}" Minimum = "0" Maximum = "100" />


< Separator />


< MenuItem Header = "Enabled" StaysOpenOnClick = "True" IsCheckable = "True"
                                                                  IsChecked = "{Binding Enabled, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"
                                                                  ToolTip = "Disabling a set will stop it from appearing here, 
                                                                                      if you want to see the set again select Enable Detection of Inactive Images"/>
                                                        <MenuItem Header="Retain Image Independence" StaysOpenOnClick="True" IsCheckable="True"
                                                                  IsChecked="{Binding RetainImageIndependence, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"
                                                                  ToolTip="Allows independent images and the set to co-exist in the theme"/>
                                                    </ContextMenu>
                                                </DataTemplate>
                                            </ContentControl.Resources>

                                            <!--? // Template Control - Context Menu // -->
                                            <ContentControl.ContentTemplate>
                                                <DataTemplate>
                                                    <!-- Default Control -->
                                                    <ContentControl Name="cc" Content="{Binding}" ContentTemplate="{StaticResource ImageModelTemplate}"/>
                                                    <!-- Triggers -->
                                                    <DataTemplate.Triggers>
                                                        <!-- If IsVideo, use the VideoTemplate instead -->
                                                        <DataTrigger Binding="{Binding IsImageSet}" Value="True">
                                                            <Setter TargetName="cc" Property="ContentTemplate" Value="{StaticResource ImageSetModelTemplate}"/>
                                                        </DataTrigger>
                                                    </DataTemplate.Triggers>
                                                </DataTemplate>
                                            </ContentControl.ContentTemplate>
                                        </ContentControl>
                                    </MenuItem.Header>

*/