using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using LanceTools.IO;
using LanceTools.Util;
using LanceTools.WPF.Adonis.Util;
using Microsoft.VisualBasic.FileIO;
using MvvmCross;
using MvvmCross.Core;
using MvvmCross.Platforms.Wpf.Views;
using Unosquare.FFME;
using WallpaperFlux.Core;
using WallpaperFlux.Winform.Util;
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

#if (!DEBUG)
            this.Dispatcher.UnhandledException += OnDispatcherUnhandledException;
#endif
        }

        // called before OnStartup
        protected override void RegisterSetup()
        {
            this.RegisterSetupType<Setup>();
        }

        private void InitializeFFmpeg()
        {
            // Get Roaming Folder
            string roamingFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string wallpaperFluxApplicationDataFolder = roamingFolder + "\\WallpaperFlux";

            Library.FFmpegDirectory = wallpaperFluxApplicationDataFolder + "\\FFmpeg\\ffmpeg-4.4-full_build-shared\\bin";
            Library.LoadFFmpeg();

            MpvUtil.MpvPath = wallpaperFluxApplicationDataFolder + "\\mpv\\mpv-1.dll";

            MediaElement.FFmpegMessageLogged += (s, ev) => Debug.WriteLine(ev.Message);
        }

        void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            // TODO Attempt to make a backup/autosave
            MessageBoxUtil.ShowError("Unhandled exception occurred: \n" + e.Exception.Message);
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
        private void OnKeyEnterDown_LoseFocus(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // Kill logical focus
                FocusManager.SetFocusedElement(FocusManager.GetFocusScope(sender as TextBox), null);

                // Kill keyboard focus
                Keyboard.ClearFocus();
            }
        }
        #endregion

        #region TextBox
        // Selects all text on gaining focus
        private async void TextBox_GotFocus_FocusText(object sender, RoutedEventArgs e)
        {
            await Application.Current.Dispatcher.InvokeAsync(((TextBox)sender).SelectAll);
        }

        // Selects all text on mouse down
        private async void TextBox_MouseEvent_FocusText(object sender, MouseButtonEventArgs e)
        {
            await Application.Current.Dispatcher.InvokeAsync(((TextBox)sender).SelectAll);
        }

        // Constrains text preview to numbers only
        private void OnPreviewTextInput_PositiveNumbersOnly(object sender, TextCompositionEventArgs e) => e.Handled = RegexUtil.IsPositiveNumber(e.Text);

        // Constrains text preview to numbers and decimal places only
        private void OnPreviewTextInput_PositiveNumbersAndDecimalsOnly(object sender, TextCompositionEventArgs e) => e.Handled = RegexUtil.IsPositiveDecimalNumber(e.Text);

        // Constrains text preview to numbers only, allows negative input
        private void OnPreviewTextInput_NumbersOnly(object sender, TextCompositionEventArgs e) => e.Handled = RegexUtil.IsNumber(e.Text);

        // Constrains text preview to numbers and decimal places only, allows negative input
        private void OnPreviewTextInput_NumbersAndDecimalsOnly(object sender, TextCompositionEventArgs e) => e.Handled = RegexUtil.IsDecimalNumber(e.Text);

        #endregion
    }
}
