using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Xml;
using AdonisUI.Controls;
using Microsoft.WindowsAPICodePack.Dialogs;
using Microsoft.WindowsAPICodePack.Shell.PropertySystem;
using MvvmCross;
using MvvmCross.Binding.BindingContext;
using MvvmCross.Commands;
using MvvmCross.Core;
using MvvmCross.ViewModels;
using WallpaperFlux.Core.External;
using WallpaperFlux.Core.Models;
using WallpaperFlux.Core.Models.Controls;
using WallpaperFlux.Core.Models.Theme;
using WallpaperFlux.Core.Util;

namespace WallpaperFlux.Core.ViewModels
{
    public class WallpaperFluxViewModel : MvxViewModel
    {
        private ThemeModel Theme;

        public WallpaperFluxViewModel()
        {
            FolderUtil.LinkThemeImageFolders(ImageFolders);

            InitializeDisplaySettings();

            // TODO Create a static class that holds the commands for this ViewModel
            NextWallpaperCommand = new MvxCommand(NextWallpaper);
            AddFolderCommand = new MvxCommand(PromptAddFolder);
            RemoveFolderCommand = new MvxCommand(RemoveFolder);
            SyncCommand = new MvxCommand(Sync);
            SelectImagesCommand = new MvxCommand(SelectImages);
        }

        async void InitializeDisplaySettings() // waits for the DisplayCount to be set before initializing the display settings
        {
            await Task.Run(() =>
            {
                while (WallpaperUtil.DisplayCount == 0)
                {
                    Thread.Sleep(10);
                }

                //! do not directly add to the DisplaySettings itself, this threaded behavior will mistakenly add an extra object unless it's slept for a certain period
                MvxObservableCollection<DisplayModel> initSettings = new MvxObservableCollection<DisplayModel>();
                for (int i = 0; i < WallpaperUtil.DisplayCount; i++)
                {
                    initSettings.Add(new DisplayModel(Mvx.IoCProvider.Resolve<ITimer>(), NextWallpaper)
                    {
                        DisplayInterval = 0,
                        DisplayIndex = i,
                        DisplayIntervalType = IntervalType.Seconds,
                        DisplayStyle = WallpaperStyle.Stretch
                    });
                }

                //! do not directly add to the DisplaySettings itself, this threaded behavior will mistakenly add an extra object unless it's slept for a certain period
                DisplaySettings = initSettings;

            }).ConfigureAwait(false);
        }

        //-----View Variables-----

        public int MaxToolTipMilliseconds { get; set; } = int.MaxValue;

        /// Image Folders
        private MvxObservableCollection<FolderModel> _imageFolders = new MvxObservableCollection<FolderModel>();

        public MvxObservableCollection<FolderModel> ImageFolders
        {
            get => _imageFolders;
            set => SetProperty(ref _imageFolders, value);
        }

        /// Selected Image Folder
        private FolderModel _selectedImageFolder;

        public FolderModel SelectedImageFolder
        {
            get { return _selectedImageFolder; }
            set
            {
                SetProperty(ref _selectedImageFolder, value);
                RaisePropertyChanged(() => CanRemoveWallpaper);
            }
        }

        /// Display Settings
        private MvxObservableCollection<DisplayModel> _displaySettings = new MvxObservableCollection<DisplayModel>();

        public MvxObservableCollection<DisplayModel> DisplaySettings
        {
            get => _displaySettings;
            set => SetProperty(ref _displaySettings, value);
        }

        /// Selected Display Setting
        private DisplayModel _selectedDisplaySetting; // used for syncing

        public DisplayModel SelectedDisplaySetting
        {
            get => _selectedDisplaySetting;
            set
            {
                SetProperty(ref _selectedDisplaySetting, value);
                RaisePropertyChanged(() => CanSync);
            }
        }

        private MvxObservableCollection<ImageSelectorTabModel> _imageSelectorTabs;

        public MvxObservableCollection<ImageSelectorTabModel> ImageSelectorTabs
        {
            get { return _imageSelectorTabs; }
            set
            {
                SetProperty(ref _imageSelectorTabs, value);
                Debug.WriteLine("Updating ImageSelectorTabs");
            }
        }

        private object _selectedImageSelectorTab;

        public object SelectedImageSelectorTab
        {
            get => _selectedImageSelectorTab;
            set
            {
                SetProperty(ref _selectedImageSelectorTab, value);
            }
        }

        //-----Enablers-----
        public bool CanNextWallpaper => ImageFolders.Count > 0;

        public bool CanPreviousWallpaper => false;

        public bool CanRemoveWallpaper => SelectedImageFolder != null;

        public bool CanSync => SelectedDisplaySetting != null;

        public bool CanSelectImages => ImageFolders.Count > 0;

        #region Commands
        //-----Commands-----

        //  -----Command Properties-----
        public IMvxCommand NextWallpaperCommand { get; set; }

        public IMvxCommand AddFolderCommand { get; set; }

        public IMvxCommand RemoveFolderCommand { get; set; }

        public IMvxCommand SyncCommand { get; set; }

        public IMvxCommand SelectImagesCommand { get; set; }

        //  -----Command Methods-----
        #region Wallpaper Setters
        public void NextWallpaper()
        {
            // TODO Turn this into a general method, refer to WallpaperManager.Pathing
            if (WallpaperUtil.Images.Count > 0)
            {
                for (int i = 0; i < WallpaperUtil.DisplayCount; i++)
                {
                    NextWallpaper(i, false);
                }
            }
            else //TODO Consider just turning this into a tooltip & disabling the button if the conditional tooltip is possible
            {
                MessageBoxModel messageBox = new MessageBoxModel
                {
                    Text = "Cannot change wallpaper, there are no active images in your theme",
                    Caption = "Error",
                    Icon = MessageBoxImage.Error,
                    Buttons = new[] { MessageBoxButtons.Ok() }
                };

                MessageBox.Show(messageBox);
            }
        }

        public void NextWallpaper(int displayIndex, bool isCallerTimer)
        {
            if (WallpaperUtil.Images.Count > 0)
            {
                Debug.WriteLine(displayIndex);
                WallpaperUtil.SetWallpaper(displayIndex);
            }
        }
        #endregion

        #region Image Folders
        public void PromptAddFolder()
        {
            using (CommonOpenFileDialog dialog = new CommonOpenFileDialog())
            {
                // dialog properties
                dialog.Multiselect = true;
                dialog.IsFolderPicker = true;

                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    if (!ImageFolders.ContainsImageFolder(dialog.FileName))
                    {
                        AddFolder(dialog.FileName);
                    }
                    else
                    {
                        MessageBoxUtil.ShowError("You have already included this folder into your theme");
                    }
                }
            }
        }

        public void AddFolder(string path)
        {
            ImageFolders.Add(new FolderModel(path, true));

            RaisePropertyChanged(() => CanNextWallpaper);
            RaisePropertyChanged(() => CanSelectImages);
        }

        public void RemoveFolder()
        {
            Debug.WriteLine("Removing: " + SelectedImageFolder.Path);
            ImageFolders.Remove(SelectedImageFolder);
            ImageFolders.ValidateImageFolders();
        }
        #endregion

        #region Display Settings
        public void Sync()
        {
            if (SelectedDisplaySetting != null)
            {
                foreach (DisplayModel displaySetting in DisplaySettings)
                {
                    if (displaySetting != SelectedDisplaySetting)
                    {
                        displaySetting.SyncModel(SelectedDisplaySetting);
                    }
                }
            }
        }
        #endregion

        #region Image Selector
        private const string EXPLORER_BUTTON_ID = "explorer";
        private const string OTHER_BUTTON_ID = "other";
        public void SelectImages()
        {
            if (_imageSelectorTabs == null)
            {
                _imageSelectorTabs = new MvxObservableCollection<ImageSelectorTabModel>();
            }

            MessageBoxModel messageBox = new MessageBoxModel
            {
                Text = "Choose a selection type",
                Caption = "Choose an option",
                Icon = MessageBoxImage.Question,
                Buttons = new[] { MessageBoxButtons.Custom("File Explorer", EXPLORER_BUTTON_ID), MessageBoxButtons.Custom("Other...", OTHER_BUTTON_ID) }
            };

            MessageBox.Show(messageBox);

            if (messageBox.ButtonPressed.Id == EXPLORER_BUTTON_ID)
            {
                using (CommonOpenFileDialog dialog = new CommonOpenFileDialog())
                {
                    //dialog.InitialDirectory = lastSelectedPath;
                    dialog.Multiselect = true;
                    dialog.Title = "Select an Image";
                    dialog.AddImageFilesFilterToDialog();

                    if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                    {
                        RebuildImageSelector(dialog.FileNames.ToArray(), false);
                        //xRebuildImageSelector(dialog.FileNames, false); // this will be in order by default
                    }
                }
            }
            else if (messageBox.ButtonPressed.Id == OTHER_BUTTON_ID)
            {
                // do thing
            }
        }

        private readonly int IMAGES_PER_PAGE = 20;
        public void RebuildImageSelector(string[] selectedImages, bool reverseOrder)
        {
            //-----Checking Conditions-----
            if (selectedImages == null)
            {
                MessageBoxUtil.ShowError("No images were selected");
                return;
            }

            // Check for null images (The EnableDetectionOfInactiveImages function is handled further down)
            int invalidCounter = 0;
            string invalidImageString = "";
            foreach (string image in selectedImages)
            {
                if (image == null)
                {
                    invalidCounter++;
                    invalidImageString += image + "\n";
                }
            }

            if (invalidCounter == selectedImages.Length)
            {
                MessageBoxUtil.ShowError("All selected images do not exist");
                return;
            }

            if (invalidCounter > 0)
            {
                MessageBoxUtil.ShowError("Some of the images selected do not exist: \n" + invalidImageString);
                return;
            }

            //-----Rebuild-----
            ImageSelectorTabs.Clear();

            int tabCount = (selectedImages.Length / IMAGES_PER_PAGE) + 1;

            int imageIndex = 0;

            for (int i = 0; i < tabCount; i++)
            {
                ImageSelectorTabModel tabModel = new ImageSelectorTabModel
                {
                    TabIndex = (i + 1).ToString()
                };

                for (int j = 0; j < IMAGES_PER_PAGE; j++)
                {
                    Debug.WriteLine((j + imageIndex) + " | " + (selectedImages.Length));
                    if (j + imageIndex < selectedImages.Length)
                    {
                        Debug.WriteLine("Path: " + selectedImages[j + imageIndex]);
                        tabModel.Images.Add(new ImageModel(Mvx.IoCProvider.Resolve<IExternalImageSource>()) {Path = selectedImages[j + imageIndex]});
                    }
                    else
                    {
                        break;
                    }
                }

                tabModel.RaisePropertyChangedImages();

                imageIndex += IMAGES_PER_PAGE;

                ImageSelectorTabs.Add(tabModel);
            }

            RaisePropertyChanged(() => ImageSelectorTabs);
            SelectedImageSelectorTab = ImageSelectorTabs[0];
        }
        #endregion
        #endregion
    }
}