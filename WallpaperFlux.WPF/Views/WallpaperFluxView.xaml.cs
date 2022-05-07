using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MvvmCross.Binding.Extensions;
using MvvmCross.Platforms.Wpf.Presenters.Attributes;
using MvvmCross.Platforms.Wpf.Views;
using MvvmCross.ViewModels;
using WallpaperFlux.Core.Models;
using WallpaperFlux.Core.Models.Controls;
using WallpaperFlux.Core.Models.Theme;
using WallpaperFlux.Core.ViewModels;
using WallpaperFlux.WPF.Util;
using WallpaperFlux.WPF.Windows;
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

        #region MediaElement
        private void MediaElement_OnLoaded(object sender, RoutedEventArgs e)
        {
            MediaElement element = sender as MediaElement;

            if (element?.DataContext is ImageModel elementImage)
            {
                element.Open(new Uri(elementImage.Path));
            }
        }

        private void MediaElement_OnLoaded_SimulateThumbnail(object sender, RoutedEventArgs e)
        {
            MediaElement element = sender as MediaElement;

            if (element?.DataContext is ImageModel elementImage)
            {
                //TODO Consider using this option: Bitmap bitmap = await element.CaptureBitmapAsync();
                element.Open(new Uri(elementImage.Path));
                element.Pause();
            }
        }

        private void MediaElement_OnUnloaded(object sender, RoutedEventArgs e)
        {
            MediaElement element = sender as MediaElement;
            element?.Close();
        }
        #endregion

        #region Menu Items

        #region Child Window Control
        private void MenuItem_OpenTagWindow_Click(object sender, RoutedEventArgs e)
        {
            bool initializing = false;

            PresentWindow(ref TagPresenter, typeof(TagView), typeof(TagViewModel),
                WindowUtil.TAGGING_WINDOW_WIDTH, WindowUtil.TAGGING_WINDOW_HEIGHT, "Tag View", false);

            //! Keep in mind that the ViewWindow will be destroyed upon closing the TagView, so yes we need to add the event again
            //? prevents the TagBoard from causing a crash the next time the tag view is opened if the tag view is closed with it open
            TagPresenter.ViewWindow.Closed += TagPresenter_ViewWindow_Closed_TagBoardFix;
        }

        private void MenuItem_MoreSettings_Click(object sender, RoutedEventArgs e) => 
            PresentWindow(ref SettingsPresenter, typeof(SettingsView), typeof(SettingsViewModel),
                WindowUtil.SETTINGS_WINDOW_WIDTH, WindowUtil.SETTINGS_WINDOW_HEIGHT, "Settings", false);

        private void TagPresenter_ViewWindow_Closed_TagBoardFix(object sender, EventArgs e) => TagViewModel.Instance.TagboardToggle = false;

        private void PresentWindow(ref ViewPresenter presenter, Type viewType, Type viewModelType, float width, float height, string title, bool modal)
        {
            if (presenter == null || presenter.ViewWindow == null) // for the case where either the presenter or the view itself do not exist
            {
                presenter = new ViewPresenter(viewType, viewModelType, width, height, title, modal);
            }
            else // if the window is already open, just focus it
            {
                presenter.ViewWindow.Focus();
            }
        }
        #endregion

        #endregion

        #region Image Selector Tab
        //? Now that the window scales dynamically you probably won't need font scaling but keep this consideration in mind
        private async void ImageSelectorTabListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
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

        private void ImageSelector_ListBoxItem_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            ControlUtil.EnsureSingularSelection(ImageSelectorTabControl.Items.OfType<ImageSelectorTabModel>().ToArray(), ImageSelectorTabControl.SelectedItem as ITabModel<ImageModel>);
        }
    }
}
