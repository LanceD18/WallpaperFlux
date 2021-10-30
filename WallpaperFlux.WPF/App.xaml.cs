using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using MvvmCross;
using MvvmCross.Core;
using MvvmCross.Platforms.Wpf.Views;
using Unosquare.FFME;
using WallpaperFlux.Core;
using WallpaperFlux.WPF.Util;
using MediaElement = Unosquare.FFME.MediaElement;

namespace WallpaperFlux.WPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : MvxApplication
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            InitializeFFmpeg();

            base.OnStartup(e);
        }

        // called before OnStartup
        protected override void RegisterSetup()
        {
            this.RegisterSetupType<Setup>();
        }

        private void InitializeFFmpeg()
        {
            Library.FFmpegDirectory = @"F:\Program Libraries\ffmpeg\ffmpeg-4.4-full_build-shared\bin";
            Library.LoadFFmpeg();
            MediaElement.FFmpegMessageLogged += (s, ev) =>
            {
                System.Diagnostics.Debug.WriteLine(ev.Message);
            };
        }

        #region Generic Control
        private void OnKeyEnterDown_GiveParentFocus(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ((sender as Control).Parent as Panel).Focusable = true;
                ((sender as Control).Parent as Panel).Focus();
            }
        }

        //? You will have to make sure that the parent is tagged with Focusable="True" (Able to apply this within the xaml)
        private void OnKeyEnterDown_FocusParent(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ((sender as Control).Parent as Panel).Focus();
            }
        }
        #endregion

        #region TextBox
        private async void TextBox_GotFocus_FocusText(object sender, RoutedEventArgs e)
        {
            await Application.Current.Dispatcher.InvokeAsync(((TextBox)sender).SelectAll);
        }

        private void OnPreviewTextInput_NumbersOnly(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
        #endregion
    }
}
