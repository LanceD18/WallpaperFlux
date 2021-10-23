using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MvvmCross.Binding.Extensions;
using MvvmCross.Platforms.Wpf.Presenters.Attributes;
using MvvmCross.Platforms.Wpf.Views;
using MvvmCross.ViewModels;
using WallpaperFlux.Core.Models;
using WallpaperFlux.Core.Models.Theme;
using WallpaperFlux.Core.ViewModels;
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
        public WallpaperFluxView()
        {
            InitializeComponent();
        }

        private async void TextBox_GotFocus_FocusText(object sender, RoutedEventArgs e)
        {
            await Application.Current.Dispatcher.InvokeAsync(((TextBox) sender).SelectAll);
        }

        //TODO The below undoes MVVM, try to fix it in the future
        //TODO Implement a Font Scaler
        private async void ImageSelectorTabListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                if (e.AddedItems.ElementAt(0) is ImageModel imageModel)
                {
                    string path = imageModel.Path;

                    SelectedImagePathTextBox.Text = path;

                    Size dimensions;
                    if (!imageModel.IsVideo)
                    {
                        System.Drawing.Image image = System.Drawing.Image.FromFile(path);
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

        private void MediaElement_OnLoaded(object sender, RoutedEventArgs e)
        {
            MediaElement element = sender as MediaElement;
            element?.Open(new Uri((element.DataContext as ImageModel)?.Path ?? string.Empty));
        }

        private void MediaElement_OnLoaded_SimulateThumbnail(object sender, RoutedEventArgs e)
        {
            MediaElement element = sender as MediaElement;

            if (sender is MediaElement)
            {
                //TODO Consider using this option: Bitmap bitmap = await element.CaptureBitmapAsync();
                element.Open(new Uri((element.DataContext as ImageModel)?.Path ?? string.Empty));
                element.Pause();
            }
        }

        private void MediaElement_OnUnloaded(object sender, RoutedEventArgs e)
        {
            MediaElement element = sender as MediaElement;
            element?.Close();
        }

        // TODO Change the name of this to match its final form eventually
        private void TextBox_OnKeyEnterDown_LoseFocus(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                FrequencyModel viewModel = (sender as TextBox).DataContext as FrequencyModel;
                Debug.WriteLine("w0");

                if (viewModel.ConfirmFrequencyCommand.CanExecute(sender))
                {
                    viewModel.ConfirmFrequencyCommand.Execute(sender);
                }
                //MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            }
        }
    }
}
