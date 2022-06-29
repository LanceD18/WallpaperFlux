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
using MediaToolkit.Model;
using MediaToolkit.Options;
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
using Image = System.Windows.Controls.Image;
using MediaElement = Unosquare.FFME.MediaElement;
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

        public WallpaperFluxView()
        {
            InitializeComponent();
        }

        private void WallpaperFluxView_OnSizeChanged_UpdateInspectorHeight(object sender, SizeChangedEventArgs e) => WallpaperFluxViewModel.Instance.SetInspectorHeight(ActualHeight - 75);

        #region MediaElement & Images

        private void Inspector_OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            LoadImageOrMediaElement(sender);
        }

        private void LoadImageOrMediaElement(object sender)
        {
            if (sender is Image image)
            {
                LoadImage(image);
            }
            else if (sender is MediaElement element)
            {
                LoadMediaElement(element);
            }
        }

        private void LoadImage(Image image)
        {
            if (image.DataContext is ImageModel imageModel)
            {
                BitmapImage bitmap = new BitmapImage();
                string path = imageModel.Path;
                FileStream stream = File.OpenRead(path);

                LoadBitmapImage(bitmap, stream, image);
            }
        }

        private void LoadMediaElement(MediaElement element)
        {
            if (element.DataContext is ImageModel elementImage)
            {
                try
                {
                    element.Open(new Uri(elementImage.Path));
                }
                catch (Exception e)
                {
                    Console.WriteLine("ERROR: Element Loading Failed: " + e);
                }
            }
        }

        //? https://stackoverflow.com/questions/8352787/how-to-free-the-memory-after-the-bitmapimage-is-no-longer-needed
        //? https://stackoverflow.com/questions/2631604/get-absolute-file-path-from-image-in-wpf
        //? prevents the application from continuing to 'use' the image after loading it in, which also saves some memory
        private void LoadBitmapImage(BitmapImage bitmap, FileStream stream, Image image)
        {
            // TODO THIS METHOD IS BEING CALLED MULTIPLE TIMES PER INSPECTOR SWITCH, FIX
            try //? this can accidentally fire off multiple times and cause crashes when trying to load videos (Who still need this for some reason?)
            {
                bitmap.BeginInit();
                bitmap.CreateOptions = BitmapCreateOptions.IgnoreColorProfile; // to help with performance
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = stream;
                bitmap.EndInit();
                bitmap.Freeze(); // prevents unnecessary copying: https://stackoverflow.com/questions/799911/in-what-scenarios-does-freezing-wpf-objects-benefit-performance-greatly
                stream.Close();
                stream.Dispose();

                image.Source = bitmap;
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR: Bitmap Creation Failed: " + e);
                //x throw;
            }

        }

        private void Image_OnLoaded(object sender, RoutedEventArgs e) => LoadImageOrMediaElement(sender);

        //? https://stackoverflow.com/questions/3024169/capture-each-wpf-mediaelement-frame - [Go down below the answer for stuff that doesn't seem to use an extension]
        //? https://stackoverflow.com/questions/35380868/extract-frames-from-video-c-sharp - Media Toolkit [LOTS OF ADDITIONAL SOLUTIONS BENEATH TOP ONE]
        private void MediaElement_OnLoaded_GenerateVideoThumbnail(object sender, RoutedEventArgs e)
        {
            if (sender is Image { DataContext: ImageModel imageModel } image)
            {
                using (var engine = new MediaToolkit.Engine())
                {
                    var video = new MediaFile(imageModel.Path);

                    engine.GetMetadata(video);

                    var options = new ConversionOptions { Seek = TimeSpan.FromSeconds(0) };
                    var outputFile = new MediaFile(AppDomain.CurrentDomain.BaseDirectory + "vidThumbnail.jpeg");

                    engine.GetThumbnail(video, outputFile, options);

                    BitmapImage bitmap = new BitmapImage();
                    FileStream stream = File.OpenRead(outputFile.Filename);

                    LoadBitmapImage(bitmap, stream, image);
                }
            }
        }

        private void MediaElement_OnLoaded(object sender, RoutedEventArgs e) => LoadImageOrMediaElement(sender);

        private void MediaElement_OnUnloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is MediaElement element) element.Close();
                //! Dispose() will freeze the program
                //xelement?.Dispose();
            }
            catch (Exception exception)
            {
                Console.WriteLine("ERROR: Element Unload Failed: " + exception);
            }
        }
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

        private void ImageSelector_ListBoxItem_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            ControlUtil.EnsureSingularSelection<ImageSelectorTabModel, ImageModel>(ImageSelectorTabControl.Items, ImageSelectorTabControl.SelectedItem as ITabModel<ImageModel>);
        }
        #endregion
    }
}
