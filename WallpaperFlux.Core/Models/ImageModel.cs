using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using AdonisUI.Controls;
using LanceTools.DiagnosticsUtil;
using Microsoft.WindowsAPICodePack.Dialogs;
using MvvmCross.Commands;
using WallpaperFlux.Core.External;
using WallpaperFlux.Core.Util;

namespace WallpaperFlux.Core.Models
{
    //TODO You should verify if the extension is valid (Look into the methods you used for this in WallpaperManager to determine said extensions)
    public class ImageModel
    {
        // Properties
        public string Path { get; set; }

        public int Rank { get; set; }

        public bool Active { get; set; }

        // Video Properties
        public double Volume { get; set; } = 0.5;

        public double Speed { get; set; }

        // Type Checkers
        public bool IsStatic => !(new FileInfo(Path).Extension == ".gif" || WallpaperUtil.IsSupportedVideoType(Path));
        
        public bool IsGIF => new FileInfo(Path).Extension == ".gif";

        public bool IsVideo => WallpaperUtil.IsSupportedVideoType(Path);

        // Commands
        public IMvxCommand ViewFileCommand { get; set; }

        public IMvxCommand OpenFileCommand { get; set; }

        public IMvxCommand SetWallpaperCommand { get; set; }

        // IoC Property
        private IExternalImageSource _imageSource;

        public IExternalImageSource ImageSource
        {
            get
            {
                _imageSource.InitCompressedSource(Path, 200, 200);
                return _imageSource;
            }
            set { _imageSource = value; }
        }

        //xpublic IExternalImageSource ImageSource { get; set; }

        // TODO Find a way to place the following values in WallpaperFluxViewModel or some alternative
        // ----- XAML Values -----
        public int ImageSelectorSettingsHeight => 25;

        public int ImageSelectorThumbnailHeight => 150;
        public int ImageSelectorThumbnailWidth => 150;

        public int ImageSelectorThumbnailWidthVideo => ImageSelectorThumbnailWidth - 20; // until the GroupBox is no longer needed this will account for it

        public ImageModel(IExternalImageSource imageSource)
        {
            ViewFileCommand = new MvxCommand(ViewFile);
            OpenFileCommand = new MvxCommand(OpenFile);
            SetWallpaperCommand = new MvxCommand(SetWallpaper);

            ImageSource = imageSource;
        }

        #region Commands
        // opens the file's folder in the explorer and selects it to navigate the scrollbar to the file
        public void ViewFile()
        {
            if (!ValidationUtil.FileExists(Path)) return;
            ProcessUtil.SelectFile(Path);
        }

        // opens the file
        public void OpenFile()
        {
            if (!ValidationUtil.FileExists(Path)) return;
            ProcessUtil.OpenFile(Path);
        }

        private const string DISPLAY_DEFAULT_ID = "display";
        public void SetWallpaper()
        {
            int displayIndex = 0;
            if (WallpaperUtil.DisplayCount > 1)
            {
                // Create MessageBox
                IMessageBoxButtonModel[] buttons = new IMessageBoxButtonModel[WallpaperUtil.DisplayCount];
                for (int i = 0; i < buttons.Length; i++)
                {
                    buttons[i] = MessageBoxButtons.Custom("Display " + (i + 1), DISPLAY_DEFAULT_ID + i);
                }

                MessageBoxModel messageBox = new MessageBoxModel
                {
                    Text = "Choose a display",
                    Caption = "Choose an option",
                    Icon = MessageBoxImage.Question,
                    Buttons = buttons
                };

                // Display MessageBox
                MessageBox.Show(messageBox);

                // Evaluate MessageBox
                for (int i = 0; i < buttons.Length; i++)
                {
                    if ((string)messageBox.ButtonPressed.Id == (DISPLAY_DEFAULT_ID + i))
                    {
                        displayIndex = i;
                        break;
                    }
                }
            }

            WallpaperUtil.SetWallpaper(displayIndex, Path);
        }
        #endregion
    }
}
