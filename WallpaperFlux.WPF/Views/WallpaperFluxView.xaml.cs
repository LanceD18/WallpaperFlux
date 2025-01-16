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

        private Image ToolTipThumbnailImage = new Image();
        private Image ThumbnailImage;
        private List<Unosquare.FFME.MediaElement> ToolTipFfmeMediaElements = new List<Unosquare.FFME.MediaElement>();
        private MediaElement ToolTipMediaElement;
        private Thread ToolTipImageThread;

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
                LoadImage(image, false, image, false);
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

        private async void LoadImage(Image image, bool highQuality, Image imageWithContext, bool isToolTip)
        {
            ImageModel thumbnailSource = imageWithContext.DataContext switch
            {
                ImageModel imageModel => imageModel,
                ImageSetModel imageSet => imageSet.GetHighestRankedImage(),
                _ => null
            };

            if (thumbnailSource == null) return; // ! failed

            // https://devblogs.microsoft.com/dotnet/configureawait-faq/#can-i-use-task.run-to-avoid-using-configureawait(false)
            await Task.Run(() =>
            {
                if (isToolTip)
                {
                    if (ToolTipImageThread is { IsAlive: true }) ToolTipImageThread.Join(); // ? checks if ToolTipImageThread is null & alive
                    if (imageWithContext == CorrectToolTipStaticImage)
                    {
                        ToolTipImageThread = new Thread(() => { FinishLoadImage(image, thumbnailSource, highQuality); });

                        try
                        {
                            ToolTipImageThread.Start();
                        }
                        catch
                        {
                            // thread failed to start
                        }
                    }
                }
                else
                {
                    FinishLoadImage(image, thumbnailSource, highQuality);
                }

            }).ConfigureAwait(false);
        }

        private void FinishLoadImage(Image image, ImageModel thumbnailSource, bool highQuality)
        {
            if (!FileUtil.Exists(thumbnailSource.Path)) return;

            try
            {
                LoadBitmapImage(image, thumbnailSource.IsGif, highQuality, path: thumbnailSource.Path);
            }
            catch (Exception e)
            {
                Debug.WriteLine("ERROR: Image Loading Failed: " + e);
            }
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
        private void LoadBitmapImage(Image image, bool isGif, bool highQuality, string path = "", int decodePixelHeight = 0, MemoryStream ms = null)
        {
            // TODO THIS METHOD IS BEING CALLED MULTIPLE TIMES PER INSPECTOR SWITCH, FIX
            try // ! this can accidentally fire off multiple times and cause crashes when trying to load videos
            {
                BitmapImage bitmap = new BitmapImage();

                // --- Begin Init ---
                bitmap.BeginInit();
                if (ms != null) ms.Seek(0, SeekOrigin.Begin);
                if (decodePixelHeight != 0) bitmap.DecodePixelHeight = decodePixelHeight; // ! only set either Height of Width (preferably whichever is larger) to prevent stretching
                if (highQuality) RenderOptions.SetBitmapScalingMode(bitmap, BitmapScalingMode.HighQuality);
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
                        // TODO Consider overriding the current MediaElement implementation with this (would just need to make an Image object for Gifs
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

        private void LoadTooltip(Image image)
        {
            //ximage.BeginInit();

            LoadImage(image, true, ThumbnailImage, true);

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
                async void AttemptSetSize(Image imageToResize, Image imageWithContext, int retries = 0)
                {
                    if (retries > 100) return; // tried too many times, just return

                    if (wallpaperIndex != -1 && imageToResize != null && imageToResize.Source != null && imageWithContext == CorrectToolTipStaticImage)
                    {
                        int pxWidth = ((BitmapSource)imageToResize.Source).PixelWidth;
                        int pxHeight = ((BitmapSource)imageToResize.Source).PixelHeight;

                        // ? if the image is too wide the ToolTip will fail to render most of the image regardless of aspect ratio adjustments
                        double wallWidth = MainWindow.Instance.Wallpapers[wallpaperIndex].Width / 1.5f;
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
                                ratio = wallWidth / pxWidth;
                            }
                            else
                            {
                                ratio = wallHeight / pxHeight;
                            }

                            // ? depending on the aspect ratio of the image and the monitor
                            // ? we may need to use the smaller side as it can stretch beyond the Tooltip range
                            if (pxHeight * ratio > wallHeight)
                            {
                                ratio = wallHeight / pxHeight;
                            }
                            else if (pxWidth * ratio > wallWidth)
                            {
                                ratio = wallWidth / pxWidth;
                            }

                            pxWidth = (int)(pxWidth * ratio);
                            pxHeight = (int)(pxHeight * ratio);
                        }

                        if (imageWithContext != CorrectToolTipStaticImage) return; // ? new image loading, abort
                        imageToResize.Width = pxWidth;
                        imageToResize.Height = pxHeight;
                        //xDebug.WriteLine("Image Width: " + imageToResize.Width); 
                        //xDebug.WriteLine("Image Height: " + imageToResize.Height);
                    }
                    else
                    {
                        Debug.WriteLine("Potential invalid image source, try again");
                        //xif (image != null && image.Source == null) retries -= 1;
                        
                        await Task.Run(() =>
                        {
                            Thread.Sleep(100); // increment the delay so that we don't spam retries
                            if (imageWithContext != CorrectToolTipStaticImage) return; // ? new image loading, abort

                            Dispatcher.Invoke(() =>
                            {
                                AttemptSetSize(ToolTipThumbnailImage, imageWithContext, ++retries); //? sometimes the image source won't load fast enough and we'll need to try again
                            });
                        });
                    }
                }

                // start attempts
                AttemptSetSize(ToolTipThumbnailImage, ThumbnailImage);
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
                            LoadBitmapImage(image, false , false, imageModel.Path, imageModel.ImageSelectorThumbnailHeight);
                        }
                        else
                        {
                            ConvertBitmapToThumbnailBitmapImage(image, imageModel);
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
        
        private void ConvertBitmapToThumbnailBitmapImage(Image img, ImageModel imageModel)
        {
            using (ShellFile shellFile = ShellFile.FromFilePath(imageModel.Path))
            {
                using (Bitmap bm = shellFile.Thumbnail.Bitmap)
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        bm.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
                        LoadBitmapImage(img, false , false, decodePixelHeight: imageModel.ImageSelectorThumbnailHeight, ms: ms);
                    }
                }
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
                    ConvertBitmapToThumbnailBitmapImage(image, imageModel);
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
        }

        //? it's a bit clunky to introduce a variable for the width of each individual window  but it works
        //? alternative options were hard to find due to the structure of this segment
        private void ImageSelectorTabControl_OnSizeChanged(object sender, SizeChangedEventArgs e) => UpdateImageSelectorTabWrapperWidth();

        private void ImageSelectorTabControl_OnSelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateImageSelectorTabWrapperWidth();

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
                // ! controls will not dispose if any events are still remaining or the control is not null
                // x https://stackoverflow.com/questions/19222400/wpf-user-control-not-being-disposed
                BindingOperations.ClearAllBindings(element);
                element = null;
            }
        }

        private Image CorrectToolTipStaticImage;
        private void ImageSelector_Image_OnMouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Image image)
            {
                ThumbnailImage = image;
                ImageModel imageModel = GetImageModelFromContext(image);

                if (imageModel.IsStatic)
                {
                    CorrectToolTipStaticImage = image; // setting this outside of a thread ensures that the value is always the latest image set
                    image.ToolTip = ToolTipThumbnailImage;
                    LoadTooltip(ToolTipThumbnailImage);
                }
                else
                {
                    if (!imageModel.IsWebm)
                    {
                        ToolTipMediaElement.Volume = imageModel.ActualVolume;
                        ToolTipMediaElement.Source = new Uri(imageModel.Path);
                        ToolTipMediaElement.IsEnabled = true;

                        image.ToolTip = ToolTipMediaElement;
                    }
                }
            }
        }

        private void ImaeSelector_Image_OnMouseLeave(object sender, MouseEventArgs e)
        {
            ThumbnailImage.ToolTip = null;
            ToolTipThumbnailImage.Source = null;

            ToolTipMediaElement.Stop();
            ToolTipMediaElement.Close();
            ToolTipMediaElement.IsEnabled = false;
            ToolTipMediaElement.Source = null;
        }

        private void InitToolTipItems()
        {
            ToolTipThumbnailImage = new Image();

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

        private void ImageSelector_Webm_ContentControl_OnMouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is ContentControl control)
            {
                if (control.DataContext is ImageModel imageModel)
                {
                    if (!imageModel.IsWebm)
                    {
                        control.ToolTip = null;
                    }
                }
            }
        }
    }
}
