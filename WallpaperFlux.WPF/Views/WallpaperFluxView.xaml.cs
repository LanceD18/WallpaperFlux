using System;
using System.Collections.Generic;
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
using MvvmCross.Base;
using MvvmCross.Binding.Extensions;
using MvvmCross.Platforms.Wpf.Presenters.Attributes;
using MvvmCross.Platforms.Wpf.Views;
using MvvmCross.ViewModels;
using Unosquare.FFME.Common;
using WallpaperFlux.Core.Models;
using WallpaperFlux.Core.Models.Controls;
using WallpaperFlux.Core.Models.Theme;
using WallpaperFlux.Core.ViewModels;
using WallpaperFlux.WPF.Util;
using WallpaperFlux.WPF.Windows;
using WpfAnimatedGif;
using Image = System.Windows.Controls.Image;
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
        public ViewPresenter TagPresenter;
        public ViewPresenter SettingsPresenter;

        private List<Thread> _ActiveThumbnailThreads = new List<Thread>(); //? kills thumbnail threads on page load, intended to stop videos from clogging the task runners

        public WallpaperFluxView()
        {
            InitializeComponent();
        }

        private void WallpaperFluxView_OnSizeChanged_UpdateInspectorHeight(object sender, SizeChangedEventArgs e) => WallpaperFluxViewModel.Instance.SetInspectorHeight(ActualHeight - 75);

        #region MediaElement & Images

        private void Inspector_OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e) => LoadImageOrMediaElementOrMpvPlayerHost(sender);

        private void LoadImageOrMediaElementOrMpvPlayerHost(object sender)
        {
            if (sender is Image image)
            {
                LoadImage(image);
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

        private void LoadImage(Image image)
        {
            if (image.DataContext is ImageModel imageModel)
            {
                Task.Run(() =>
                {
                    if (!FileUtil.Exists(imageModel.Path)) return;

                    try
                    {
                        //xFileStream stream = File.OpenRead(path);

                        LoadBitmapImage(image, imageModel.IsGif, path: imageModel.Path);
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine("ERROR: Image Loading Failed: " + e);
                    }
                });
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
        private void LoadBitmapImage(Image image, bool isGif, string path = "", FileStream stream = null)
        {
            // TODO THIS METHOD IS BEING CALLED MULTIPLE TIMES PER INSPECTOR SWITCH, FIX
            try //? this can accidentally fire off multiple times and cause crashes when trying to load videos (Who still need this for some reason?)
            {
                BitmapImage bitmap = new BitmapImage();

                bitmap.BeginInit();
                bitmap.CreateOptions = BitmapCreateOptions.IgnoreColorProfile; // to help with performance
                bitmap.CacheOption = BitmapCacheOption.OnLoad;

                if (stream != null)
                {
                    bitmap.StreamSource = stream;
                }
                else if (path != "")
                {
                    bitmap.UriSource = new Uri(path); //? uri-source loads the image asynchronously
                }

                bitmap.EndInit();
                bitmap.Freeze(); // prevents unnecessary copying: https://stackoverflow.com/questions/799911/in-what-scenarios-does-freezing-wpf-objects-benefit-performance-greatly

                //! Task.Run() will be used outside of this method to capture the Bitmap within the 'calling thread'
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

                    if (stream != null)
                    {
                        stream.Close();
                        stream.Dispose();
                    }

                });
            }
            catch (Exception e)
            {
                Debug.WriteLine("ERROR: Bitmap Loading Failed: " + e);
            }
        }

        private void Image_OnLoaded(object sender, RoutedEventArgs e) => LoadImageOrMediaElementOrMpvPlayerHost(sender);
        
        private void Image_OnLoaded_LowQuality(object sender, RoutedEventArgs e)
        {
            if (sender is Image { DataContext: ImageModel imageModel } image)
            {
                Thread thread = new Thread(() => //? this cannot thread over the if statement while the Image object is present
                {
                    try //? this can accidentally fire off multiple times and cause crashes when trying to load videos (Who still need this for some reason?)
                    {
                        BitmapImage bitmap = new BitmapImage();
                        //xstring path = imageModel.Path;
                        //xFileStream stream = File.OpenRead(path);

                        bitmap.BeginInit();
                        RenderOptions.SetBitmapScalingMode(bitmap, BitmapScalingMode.LowQuality);
                        bitmap.DecodePixelHeight = imageModel.ImageSelectorThumbnailHeight; //! Only set either Height or Width (preferably the larger variant?) to prevent awful stretching!
                        //x bitmap.DecodePixelWidth = imageModel.ImageSelectorThumbnailWidth;
                        bitmap.CreateOptions = BitmapCreateOptions.IgnoreColorProfile; // to help with performance
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
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
                });

                thread.IsBackground = true; // stops the thread from continuing to run on closing the application
                thread.Start();
                _ActiveThumbnailThreads.Add(thread);
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
                    using (Engine engine = new Engine())
                    {
                        MediaFile video = new MediaFile(imageModel.Path);
                        engine.GetMetadata(video);
                        
                        //? several videos show nothing on second 0, creating an empty thumbnail
                        ConversionOptions options = new ConversionOptions { Seek = TimeSpan.FromSeconds(video.Metadata.Duration.Seconds * 0.05) };

                        string vidThumbnailPath = AppDomain.CurrentDomain.BaseDirectory + "vidThumbnail.jpg";
                        int collisions = 0;
                        
                        //? in most cases the files will be generated fast enough to avoid needing this, but in some rare cases videos will end up with the wrong thumbnail file
                        while (FileUtil.Exists(vidThumbnailPath))
                        {
                            vidThumbnailPath = AppDomain.CurrentDomain.BaseDirectory + "vidThumbnail" + collisions + ".jpg";
                            collisions++;
                        }

                        MediaFile outputFile = new MediaFile(vidThumbnailPath);

                        engine.GetThumbnail(video, outputFile, options);
                        
                        FileStream stream = File.OpenRead(outputFile.Filename);
                        
                        LoadBitmapImage(image, false, stream: stream);

                        if (FileUtil.Exists(vidThumbnailPath))
                        {
                            try
                            {
                                FileSystem.DeleteFile(vidThumbnailPath);
                            }
                            catch (Exception exception)
                            {
                                Debug.WriteLine("Thumbnail Deletion Fail: " + exception);
                            }
                        }
                    }
                });

                thread.IsBackground = true; // stops the thread from continuing to run on closing the application
                thread.Start();
                _ActiveThumbnailThreads.Add(thread);
            }
        }

        private void MediaElement_OnLoaded(object sender, RoutedEventArgs e) => LoadImageOrMediaElementOrMpvPlayerHost(sender);

        private void MediaElement_OnUnloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is MediaElement element) element.Close();
                if (sender is Unosquare.FFME.MediaElement elementFFME) elementFFME.Close();
                //! Dispose() will freeze the program
                //xelement?.Dispose();
            }
            catch (Exception exception)
            {
                Console.WriteLine("ERROR: Element Unload Failed: " + exception);
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
        #endregion

        #region Child Window Control
        private void MenuItem_OpenTagWindow_Click(object sender, RoutedEventArgs e)
        {
            WindowUtil.PresentWindow(ref TagPresenter, typeof(TagView), typeof(TagViewModel),
                WindowUtil.TAGGING_WINDOW_WIDTH, WindowUtil.TAGGING_WINDOW_HEIGHT, "Tag View", false);

            //! Keep in mind that the ViewWindow will be destroyed upon closing the TagView, so yes we need to add the event again
            //? Prevents the TagBoard from causing a crash the next time the tag view is opened if the tag view is closed with the TagBoard open
            TagPresenter.ViewWindow.Closing += TagPresenter_ViewWindow_Closed_TagBoardFix;
        }

        private void MenuItem_MoreSettings_Click(object sender, RoutedEventArgs e) => 
            WindowUtil.PresentWindow(ref SettingsPresenter, typeof(SettingsView), typeof(SettingsViewModel),
                WindowUtil.SETTINGS_WINDOW_WIDTH, WindowUtil.SETTINGS_WINDOW_HEIGHT, "Settings", false);

        /// <summary>
        /// Disables the TagBoard on closing the view
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //? Prevents the TagBoard from causing a crash the next time the tag view is opened if the tag view is closed with the TagBoard open
        private void TagPresenter_ViewWindow_Closed_TagBoardFix(object sender, EventArgs e) => TagViewModel.Instance.TagboardToggle = false;
        #endregion

        #region Image Selector
        //? Now that the window scales dynamically you probably won't need font scaling but keep this consideration in mind
        private void ImageSelectorTabListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Debug.WriteLine("Selection changed");
            return;
            // Font Scaling
            if (e.AddedItems.Count > 0)
            {
                if (e.AddedItems.ElementAt(0) is ImageModel imageModel)
                {
                    string path = imageModel.Path;

                    SelectedImagePathTextBox.Text = path;

                    Size dimensions;
                    if (!imageModel.IsVideo)
                    {
                        System.Drawing.Image image = System.Drawing.Image.FromFile(path); // TODO The ExternalDisplayUtil can handle this now
                        dimensions = new Size(image.Width, image.Height);
                        image.Dispose();

                        SelectedImageDimensionsTextBox.Text = dimensions.Width + "x" + dimensions.Height;
                    }
                    else
                    {
                        // TODO Figure out how to gather the video dimensions (With the below method the dimensions never load in time, or seemingly don't load at all)
                        /*
                        MediaElement element = new MediaElement();
                        await element.Open(new Uri(path));
                        Bitmap bitmap = await element.CaptureBitmapAsync();

                        dimensions = new Size(bitmap.Width, bitmap.Height);
                        await element.Close();
                        */

                        SelectedImageDimensionsTextBox.Text = "";
                    }
                }
            }
        }

        //? it's a bit clunky to introduce a variable for the width of each individual window  but it works
        //? alternative options were hard to find due to the structure of this segment
        private void ImageSelectorTabControl_OnSizeChanged(object sender, SizeChangedEventArgs e) => UpdateImageSelectorTabWrapperWidth();

        private void ImageSelectorTabControl_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Debug.WriteLine(sender.GetType());
            if (sender is TabControl { SelectedItem: ImageSelectorTabModel tab })
            {
                WallpaperFluxViewModel.Instance.VerifyImageSelectorTab(tab);
            }

            UpdateImageSelectorTabWrapperWidth();

            /*x
            // close all active thumbnail threads
            while (_ActiveThumbnailThreads.Count > 0)
            {
                Thread thread = _ActiveThumbnailThreads[0];
                _ActiveThumbnailThreads.Remove(thread);
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

        // this captures the selection range of the entire listbox item
        private void ImageSelector_ListBoxItem_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            ControlUtil.EnsureSingularSelection<ImageSelectorTabModel, ImageModel>(ImageSelectorTabControl.Items, ImageSelectorTabControl.SelectedItem as ITabModel<ImageModel>);
        }
    }
}
