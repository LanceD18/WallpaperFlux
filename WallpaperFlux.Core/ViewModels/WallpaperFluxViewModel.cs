using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Xml;
using AdonisUI.Controls;
using HandyControl.Controls;
using LanceTools;
using Microsoft.WindowsAPICodePack.Dialogs;
using Microsoft.WindowsAPICodePack.Shell.PropertySystem;
using MvvmCross;
using MvvmCross.Binding.BindingContext;
using MvvmCross.Commands;
using MvvmCross.Core;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using WallpaperFlux.Core.Collections;
using WallpaperFlux.Core.IoC;
using WallpaperFlux.Core.JSON;
using WallpaperFlux.Core.JSON.Temp;
using WallpaperFlux.Core.Models;
using WallpaperFlux.Core.Models.Controls;
using WallpaperFlux.Core.Models.Tagging;
using WallpaperFlux.Core.Models.Theme;
using WallpaperFlux.Core.Tools;
using WallpaperFlux.Core.Util;
using MessageBox = AdonisUI.Controls.MessageBox;

namespace WallpaperFlux.Core.ViewModels
{
    public class WallpaperFluxViewModel : MvxViewModel
    {
        public static WallpaperFluxViewModel Instance;

        // TODO When you need more view models, initialize them in the constructor here

        //? this variable exists for use with the xaml despite the Static Reference
        // allows the data of settings to be accessed by the xaml code
        public ThemeModel Theme_USE_STATIC_REFERENCE_FIX_LATER { get; set; } = ThemeUtil.Theme; //! don't forget to update WallpaperFluxView.xaml when you changed this
        //? this variable exists for use with the xaml despite the Static Reference
        //? this variable exists for use with the xaml despite the Static Reference
        //? this variable exists for use with the xaml despite the Static Reference

        #region View Variables
        //-----View Variables-----
        public int MaxToolTipMilliseconds { get; set; } = int.MaxValue; // TODO See if you can make this a static resource in the main xaml instead

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
            get => _selectedImageFolder;
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

        #region Image Selector

        private readonly int IMAGES_PER_PAGE = 20;

        private MvxObservableCollection<ImageSelectorTabModel> _imageSelectorTabs = new MvxObservableCollection<ImageSelectorTabModel>();

        public MvxObservableCollection<ImageSelectorTabModel> ImageSelectorTabs
        {
            get => _imageSelectorTabs;
            set
            {
                SetProperty(ref _imageSelectorTabs, value);
                Debug.WriteLine("Updating ImageSelectorTabs");
            }
        }

        //! Without this initialization we will crash references on empty themes, may have to fix this for other properties
        private ImageSelectorTabModel _selectedImageSelectorTab = new ImageSelectorTabModel(-1);

        public ImageSelectorTabModel SelectedImageSelectorTab
        {
            get => _selectedImageSelectorTab;
            set => SetProperty(ref _selectedImageSelectorTab, value);
        }

        //? Updated by RaisePropertyChanged() calls in references
        public string SelectedImagePathText
        {
            get
            {
                if (SelectedImageSelectorTab == null || SelectedImageSelectorTab.SelectedImage == null) return "";

                if (SelectedImageCount <= 1)
                {
                    return SelectedImageSelectorTab.SelectedImage?.Path;
                }

                return "Selected Images: " + SelectedImageCount;
            }
        }

        private int _selectedImageCount;
        public int SelectedImageCount
        {
            get => _selectedImageCount;
            set
            {
                _selectedImageCount = value;
                RaisePropertyChanged(() => SelectedImagePathText);
                
                TaggingUtil.HighlightTags();
            }
        }

        //? Updated by RaisePropertyChanged() calls in references
        public string SelectedImageDimensionsText
        {
            get
            {
                if (SelectedImageSelectorTab == null || SelectedImageSelectorTab.SelectedImage == null) return "";

                Size size = SelectedImageSelectorTab.SelectedImage.GetSize();
                return size.Width + "x" + size.Height;
            }
        }

        public ImageModel SelectedImage => SelectedImageSelectorTab?.SelectedImage; //? for the xaml

        #region Inspector

        //? Updated by RaisePropertyChanged() calls in references
        public MvxObservableCollection<TagModel> InspectedImageTags
        {
            get
            {
                if (SelectedImageSelectorTab == null || SelectedImageSelectorTab.SelectedImage == null) return null;

                HashSet<TagModel> tags = SelectedImageSelectorTab.SelectedImage.Tags.GetTags();

                foreach (TagModel tag in tags)
                {
                    tag.RaisePropertyChanged(() => tag.ExceptionColor); //? we don't want to raise this property for *every single tag* on every inspector change so we'll do this here
                    tag.RaisePropertyChanged(() => tag.ExceptionText); //? we don't want to raise this property for *every single tag* on every inspector change so we'll do this here
                }

                return new MvxObservableCollection<TagModel>(tags);
            }
        }

        private double _inspectorHeight;
        public double InspectorHeight
        {
            get => _inspectorHeight;
            set => SetProperty(ref _inspectorHeight, value); //? needed to update the height when resizing the window
        }

        #endregion

        #endregion

        #endregion

        #region Enablers & Toggles
        public bool CanNextWallpaper => ImageFolders.Count > 0;

        public bool CanPreviousWallpaper
        {
            get
            {
                foreach (Stack<string> previousWallpaperSet in ThemeUtil.Theme.WallpaperRandomizer.PreviousWallpapers)
                {
                    //? depending on the setup of the timers different wallpapers can have different count of previous wallpapers so we only need one to be true
                    if (previousWallpaperSet.Count > 0) return true;
                }

                return false;
            }
        }

        public bool CanRemoveWallpaper => SelectedImageFolder != null;

        public bool CanSync => SelectedDisplaySetting != null;

        public bool CanSelectImages => ImageFolders.Count > 0;

        public bool IsImageSelected => SelectedImage != null;

        public bool InspectorToggle { get; set; }

        public bool GroupRenamed { get; set; }

        public bool IsThemeLoaded => !string.IsNullOrEmpty(JsonUtil.LoadedThemePath);

        #endregion

        #region Commands
        //-----Commands-----

        public IMvxCommand NextWallpaperCommand { get; set; }

        public IMvxCommand PreviousWallpaperCommand { get; set; }

        public IMvxCommand LoadThemeCommand { get; set; }

        public IMvxCommand SaveThemeCommand { get; set; }

        public IMvxCommand SaveThemeAsCommand { get; set; }

        public IMvxCommand AddFolderCommand { get; set; }

        public IMvxCommand RemoveFolderCommand { get; set; }

        public IMvxCommand SyncCommand { get; set; }

        #region Image Selector

        public IMvxCommand ClearImagesCommand { get; set; }

        public IMvxCommand SelectImagesCommand { get; set; }

        public IMvxCommand PasteTagBoardCommand { get; set; }

        public IMvxCommand SelectAllImagesCommand { get; set; }

        public IMvxCommand DeselectImagesCommand { get; set; }

        public IMvxCommand RenameImagesCommand { get; set; }

        public IMvxCommand MoveImagesCommand { get; set; }

        public IMvxCommand DeleteImagesCommand { get; set; }

        public IMvxCommand RankImagesCommand { get; set; }

        #endregion

        #region Inspector

        public IMvxCommand ToggleInspectorCommand { get; set; }

        public IMvxCommand CloseInspectorCommand { get; set; }

        #endregion

        #endregion

        public WallpaperFluxViewModel(/*xIMvxNavigationService navigationService*/)
        {
            Instance = this;

            AudioManager.StartAudioManagerTimer();

            //x_navigationService = navigationService;

            //xFolderUtil.LinkThemeImageFolders(ImageFolders);

            InitializeDisplaySettings();

            InitializeCommands();
        }

        /*x
        // Docs: https://www.mvvmcross.com/documentation/fundamentals/navigation
        public async Task TagViewModelNavigator()
        {
            await _navigationService.Navigate<TagViewModel>();
        }
        */

        async void InitializeDisplaySettings() // waits for the DisplayCount to be set before initializing the display settings
        {
            await Task.Run(() =>
            {
                while (WallpaperUtil.DisplayUtil.GetDisplayCount() == 0)
                {
                    Thread.Sleep(10);
                }

                //! do not directly add to the DisplaySettings itself, this threaded behavior will mistakenly add an extra object unless it's slept for a certain period
                MvxObservableCollection<DisplayModel> initDisplaySettings = new MvxObservableCollection<DisplayModel>();
                for (int i = 0; i < WallpaperUtil.DisplayUtil.GetDisplayCount(); i++)
                {
                    initDisplaySettings.Add(new DisplayModel(Mvx.IoCProvider.Resolve<IExternalTimer>(), NextWallpaper)
                    {
                        DisplayInterval = 0,
                        DisplayIndex = i,
                        DisplayIntervalType = IntervalType.Seconds,
                        DisplayStyle = WallpaperStyle.Stretch
                    });
                }

                //! do not directly add to the DisplaySettings itself, this threaded behavior will mistakenly add an extra object unless it's slept for a certain period
                //! do not directly add to the DisplaySettings itself, this threaded behavior will mistakenly add an extra object unless it's slept for a certain period
                DisplaySettings = initDisplaySettings;
                //! do not directly add to the DisplaySettings itself, this threaded behavior will mistakenly add an extra object unless it's slept for a certain period

            }).ConfigureAwait(false);
        }

        public void InitializeCommands()
        {
            // TODO Consider using models to hold command information (Including needed data)
            NextWallpaperCommand = new MvxCommand(NextWallpaper_ForceChange);
            PreviousWallpaperCommand = new MvxCommand(PreviousWallpaper);
            LoadThemeCommand = new MvxCommand(PromptLoadTheme);
            SaveThemeCommand = new MvxCommand(JsonUtil.QuickSave);
            SaveThemeAsCommand = new MvxCommand(PromptSaveTheme);
            AddFolderCommand = new MvxCommand(PromptAddFolder);
            RemoveFolderCommand = new MvxCommand(PromptRemoveFolder);
            SyncCommand = new MvxCommand(SyncDisplaySettings);

            // Image Selector
            ClearImagesCommand = new MvxCommand(ClearImages);
            SelectImagesCommand = new MvxCommand(SelectImages);
            PasteTagBoardCommand = new MvxCommand(PasteTagBoard);
            SelectAllImagesCommand = new MvxCommand(SelectAllImages);
            DeselectImagesCommand = new MvxCommand(DeselectAllImages);
            RenameImagesCommand = new MvxCommand(() => ImageRenamer.AutoRenameImageRange(GetAllHighlightedImages()));
            MoveImagesCommand = new MvxCommand(() => ImageRenamer.AutoMoveImageRange(GetAllHighlightedImages()));
            DeleteImagesCommand = new MvxCommand(() => ImageUtil.DeleteImageRange(GetAllHighlightedImages()));
            RankImagesCommand = new MvxCommand(() => ImageUtil.PromptRankImageRange(GetAllHighlightedImages()));

            // Image Inspector
            ToggleInspectorCommand = new MvxCommand(ToggleInspector);
            CloseInspectorCommand = new MvxCommand(CloseInspector);
        }

        #region Command Methods
        // TODO Create subclasses for the commands with excess methods (Like Theme Data)
        //  -----Command Methods-----
        #region Wallpaper Setters

        public void NextWallpaper()
        {
            for (int i = 0; i < WallpaperUtil.DisplayUtil.GetDisplayCount(); i++)
            {
                NextWallpaper(i, false, false);
            }
        }

        public void NextWallpaper_ForceChange()
        {
            for (int i = 0; i < WallpaperUtil.DisplayUtil.GetDisplayCount(); i++)
            {
                NextWallpaper(i, false, true);
            }
        }

        // TODO Check if the theme is finished loading before activating, check if any images are active
        public void NextWallpaper(int displayIndex, bool isCallerTimer, bool forceChange)
        {
            if (ThemeUtil.Theme.Images.GetAllImages().Length > 0 && ThemeUtil.Theme.RankController.GetAllRankedImages().Length > 0)
            {
                // ignoreRandomization = false here since we need to randomize the new set
                // Note that RandomizeWallpaper() will check if it even should randomize the wallpapers first (Varied timers and extended videos can undo this requirement)
                WallpaperUtil.SetWallpaper(displayIndex, false, forceChange);
            }
            else
            {
                MessageBoxModel messageBox = new MessageBoxModel
                {
                    Text = "Cannot change wallpaper, there are no active images in your theme (Some may need to be given a rank greater than 0)",
                    Caption = "Error",
                    Icon = MessageBoxImage.Error,
                    Buttons = new[] { MessageBoxButtons.Ok() }
                };

                MessageBox.Show(messageBox);
            }
        }
        
        public void PreviousWallpaper()
        {
            for (int i = 0; i < ThemeUtil.Theme.WallpaperRandomizer.PreviousWallpapers.Length; i++)
            {
                PreviousWallpaper(i);
            }
        }

        // sets all wallpapers to their previous wallpaper, if one existed
        public void PreviousWallpaper(int index)
        {
            if (ThemeUtil.Theme.WallpaperRandomizer.PreviousWallpapers[index].Count > 0)
            {
                ThemeUtil.Theme.WallpaperRandomizer.NextWallpapers[index] = ThemeUtil.Theme.WallpaperRandomizer.PreviousWallpapers[index].Pop();

                if (File.Exists(ThemeUtil.Theme.WallpaperRandomizer.NextWallpapers[index]))
                {
                    WallpaperUtil.SetWallpaper(index, true, true); // ignoreRandomization = true since there is no need to randomize wallpapers that have previously existed
                }
            }
            // Not needed here, we can just disable the button
            //xelse
            //x{
            //x    MessageBox.Show("There are no more previous wallpapers");
            //x}
        }
        #endregion

        #region Theme Data

        public void PromptLoadTheme()
        {
            bool loadingOld = MessageBoxUtil.PromptYesNo("Load old data? [Remove this prompt when you no longer rely on the old data]");

            using (CommonOpenFileDialog dialog = new CommonOpenFileDialog())
            {
                //x dialog.InitialDirectory = lastSelectedPath;
                dialog.Multiselect = false;
                dialog.Title = "Select a theme";
                dialog.Filters.Add(new CommonFileDialogFilter(JsonUtil.JSON_FILE_DISPLAY_NAME, JsonUtil.JSON_FILE_EXTENSION));

                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    JsonUtil.SetIsLoadingData(true);

                    if (loadingOld)
                    {
                        JsonUtil.ConvertOldTheme(JsonUtil.LoadOldData(dialog.FileName));
                    }
                    else
                    {
                        JsonUtil.ConvertTheme(JsonUtil.LoadData(dialog.FileName));
                    }

                    JsonUtil.SetIsLoadingData(false);
                }
            }
        }

        // updates some properties that may change during runtime but shouldn't be processed constantly
        public void UpdateTheme()
        {
            Debug.WriteLine("Updating theme...");
            // TODO This check will only matter once you use 'Update Theme' to check for the existence of new images
            //x DataUtil.Theme.Settings.ThemeSettings.FrequencyCalc.VerifyImageTypeExistence();
            JsonUtil.QuickSave();
        }

        public void PromptSaveTheme()
        {
            //! Do NOT save Child Tags! Those are handled on their own whenever the Parent Tag is linked!
            //! In general just do not save Tag Linking logic

            using (CommonSaveFileDialog dialog = new CommonSaveFileDialog())
            {
                dialog.Title = "Save data to json";
                dialog.Filters.Add(new CommonFileDialogFilter(JsonUtil.JSON_FILE_DISPLAY_NAME, JsonUtil.JSON_FILE_EXTENSION));
                dialog.DefaultExtension = ".json";

                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    JsonUtil.SaveData(dialog.FileName);
                }
            }
        }
        #endregion

        #region Image Folders
        // TODO Move these methods to FolderUtil.cs (Will need to modify ImageFolder, perhaps by moving it to DataUtil.Theme)
        public void PromptAddFolder()
        {
            using (CommonOpenFileDialog dialog = new CommonOpenFileDialog())
            {
                // dialog properties
                dialog.Multiselect = false;
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

        public void AddFolder(string path) => AddFolderRange(new string[] { path });

        public void AddFolderRange(string[] paths)
        {
            List<FolderModel> folders = new List<FolderModel>();

            foreach (string folderPath in paths)
            {
                folders.Add(new FolderModel(folderPath, true));
            }

            AddFolderRange(folders.ToArray());
        }

        public void AddFolderRange(SimplifiedFolder[] simpleFolders)
        {
            List<FolderModel> folders = new List<FolderModel>();

            foreach (SimplifiedFolder folder in simpleFolders)
            {
                folders.Add(new FolderModel(folder.Path, folder.Enabled));
            }
            Debug.WriteLine("Folders Created");

            AddFolderRange(folders.ToArray());
        }

        public void AddFolderRange(FolderModel[] folders)
        {
            string errorOutput = "Could not add the folder(s):";
            bool errorOccured = false;

            string alreadyExistsString = "The following folder(s) already exist:";
            bool alreadyExistsOccured = false;

            foreach (FolderModel folder in folders)
            {
                if (Directory.Exists(folder.Path))
                {
                    if (!ContainsFolder(folder.Path))
                    {
                        ImageFolders.Add(folder);
                    }
                    else
                    {
                        alreadyExistsString += "\n" + folder;
                        alreadyExistsOccured = true;
                    }
                }
                else
                {
                    errorOutput += "\n" + folder;
                    errorOccured = true;
                }
            }

            if (errorOccured) MessageBoxUtil.ShowError(errorOutput);
            if (alreadyExistsOccured) MessageBoxUtil.ShowError(alreadyExistsString);

            RaisePropertyChanged(() => CanNextWallpaper);
            RaisePropertyChanged(() => CanSelectImages);

            UpdateTheme();
        }

        public void PromptRemoveFolder()
        {
            if (MessageBoxUtil.PromptYesNo("Are you sure you want to remove the following folder?" + "\n" + SelectedImageFolder.Path))
            {
                RemoveFolder();
            }
        }

        // TODO Figure out how to remove multiple folders at once [Already done with tags & image selections, just apply what you did there here]
        public void RemoveFolder()
        {
            Debug.WriteLine("Removing: " + SelectedImageFolder.Path);
            SelectedImageFolder.RemoveAllImagesOfFolder();
            ImageFolders.Remove(SelectedImageFolder);
            //xImageFolders.ValidateImageFolders();

            UpdateTheme();
        }

        public bool ContainsFolder(string path) => ImageFolders.ContainsImageFolder(path);

        /*x
            foreach (FolderModel folder in ImageFolders)
            {
                if (folder.Path == path) return true;
            }

            return false;
            */

        #endregion

        #region Display Settings
        public void SyncDisplaySettings() => SyncDisplaySettings(SelectedDisplaySetting);

        public void SyncDisplaySettings(DisplayModel parentDisplaySetting)
        {
            if (parentDisplaySetting != null)
            {
                foreach (DisplayModel displaySetting in DisplaySettings)
                {
                    if (displaySetting != parentDisplaySetting)
                    {
                        displaySetting.SyncModel(parentDisplaySetting);
                    }
                }
            }
        }
        #endregion

        #region Image Selector
        private const string EXPLORER_BUTTON_ID = "explorer";
        private const string OTHER_BUTTON_ID = "other";
        private const string CANCEL_BUTTON_ID = "cancel";
        public void SelectImages()
        {
            if (_imageSelectorTabs == null) _imageSelectorTabs = new MvxObservableCollection<ImageSelectorTabModel>();

            // ----- Create Button -----
            MessageBoxModel messageBox = new MessageBoxModel
            {
                Text = "Choose a selection type",
                Caption = "Choose an option",
                Icon = MessageBoxImage.Question,
                Buttons = new[] { MessageBoxButtons.Custom("File Explorer", EXPLORER_BUTTON_ID), 
                    MessageBoxButtons.Custom("Other...", OTHER_BUTTON_ID),
                    MessageBoxButtons.Custom("Cancel", CANCEL_BUTTON_ID)
                }
            };

            MessageBox.Show(messageBox);

            // ----- Evaluate Button Result -----
            if ((string)messageBox.ButtonPressed.Id == EXPLORER_BUTTON_ID)
            {
                using (CommonOpenFileDialog dialog = new CommonOpenFileDialog())
                {
                    //dialog.InitialDirectory = lastSelectedPath;
                    dialog.Multiselect = true;
                    dialog.Title = "Select an Image";
                    dialog.AddImageFilesFilterToDialog();

                    if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                    {
                        RebuildImageSelector(dialog.FileNames.ToArray());
                    }
                }
            }
            else if ((string)messageBox.ButtonPressed.Id == OTHER_BUTTON_ID)
            {
                Mvx.IoCProvider.Resolve<IExternalViewPresenter>().PresentImageSelectionOptions();
            }
            else if ((string)messageBox.ButtonPressed.Id == CANCEL_BUTTON_ID)
            {
                // do nothing
            }
        }

        //? ----- Rebuild Image Selector (String) -----
        private readonly string INVALID_IMAGE_STRING_DEFAULT = "The following selected images do not exist in your theme:\n(Please add the folder that they reside in to include them)";
        private readonly string INVALID_IMAGE_STRING_ALL_INVALID = "None of the selected images exist in your theme. Please add the folder that they reside in to include them.";
        public void RebuildImageSelector(string[] selectedImages, bool randomize = false, bool reverseOrder = false)
        {
            List<ImageModel> selectedImageModels = new List<ImageModel>();

            int invalidCounter = 0;
            string invalidImageString = INVALID_IMAGE_STRING_DEFAULT;

            foreach (string imagePath in selectedImages)
            {
                if (ThemeUtil.Theme.Images.ContainsImage(imagePath))
                {
                    selectedImageModels.Add(ThemeUtil.Theme.Images.GetImage(imagePath));
                }
                else
                {
                    invalidCounter++;
                    invalidImageString += "\n" + imagePath;
                }
            }

            if (invalidCounter == selectedImages.Length)
            {
                MessageBoxUtil.ShowError(INVALID_IMAGE_STRING_ALL_INVALID);
                return;
            }

            if (invalidCounter > 0)
            {
                MessageBoxUtil.ShowError(invalidImageString);
            }

            RebuildImageSelector(selectedImageModels.ToArray(), randomize, reverseOrder);
        }

        //? ----- Rebuild Image Selector (ImageModel) -----
        public void RebuildImageSelector(ImageModel[] selectedImages, bool randomize = false, bool reverseOrder = false)
        {
            //-----Checking Validation Conditions-----
            if (selectedImages == null || selectedImages.Length == 0)
            {
                MessageBoxUtil.ShowError("No images were selected");
                return;
            }

            // Check for null images (The EnableDetectionOfInactiveImages function is handled further down)
            int nullCounter = 0;
            string nullImageString = "The following selected images do not exist:";
            foreach (ImageModel image in selectedImages)
            {
                if (image == null)
                {
                    nullCounter++;
                    nullImageString += "\n" + image;
                }
            }

            if (nullCounter == selectedImages.Length)
            {
                MessageBoxUtil.ShowError("All selected images do not exist");
                return;
            }

            if (nullCounter > 0)
            {
                MessageBoxUtil.ShowError(nullImageString);
                return;
            }

            //----- Apply Optional Modification Conditions -----
            //? should come after checking conditions, no need to process this if something is wrong
            if (randomize)
            {
                selectedImages = selectedImages.Randomize().ToArray();
            }
            else if (reverseOrder) //? it is redundant to randomize and reverse the order at the same time as the randomization will end up being the only factor
            {
                selectedImages = selectedImages.Reverse().ToArray();
            }

            //-----Rebuild-----
            ImageSelectorTabs.Clear();

            int tabCount = (selectedImages.Length / IMAGES_PER_PAGE) + 1;
            int imageIndex = 0;

            int invalidCounter = 0;
            string invalidImageString = INVALID_IMAGE_STRING_DEFAULT;

            for (int i = 0; i < tabCount; i++)
            {
                // a reference of the index for use with the XAML code
                ImageSelectorTabModel tabModel = new ImageSelectorTabModel(i + 1);

                // Add images to the current tabModel until we run out of images for this page
                for (int j = 0; j < IMAGES_PER_PAGE; j++)
                {
                    // a needed cutoff to prevent us from going above the max index of selectedImages[] since we are looping by images per page
                    if (j + imageIndex < selectedImages.Length)
                    {
                        ImageModel image = selectedImages[j + imageIndex];
                        if (ThemeUtil.Theme.Images.ContainsImage(image.Path, image.ImageType))
                        {
                            tabModel.Items.Add(image);
                        }
                        else
                        {
                            invalidCounter++;
                            invalidImageString += "\n" + image.Path;
                        }
                    }
                    else
                    {
                        break;
                    }
                }

                imageIndex += IMAGES_PER_PAGE; // ensures that we traverse through selectedImages[] properly

                //x tabModel.RaisePropertyChangedImages();
                ImageSelectorTabs.Add(tabModel);
            }

            if (invalidCounter == selectedImages.Length)
            {
                MessageBoxUtil.ShowError(INVALID_IMAGE_STRING_ALL_INVALID);
                //? we don't return here because the collection was modified
            }

            if (invalidCounter > 0)
            {
                MessageBoxUtil.ShowError(invalidImageString);
            }

            RaisePropertyChanged(() => ImageSelectorTabs);
            SelectedImageSelectorTab = ImageSelectorTabs[0];
            SelectedImageCount = 0; // without this the count will jump up to the number of images in the previous selection and be unable to reset
        }

        private void ClearImages()
        {
            ImageSelectorTabs.Clear();
        }

        // gathers all images across all image selector tabs
        //? While initializing this in RebuildImageSelector() is an option, keep in mind that doing so will bring additional load times that don't always apply to the use case
        //? each search, so it's best to just wait for when the user *actually* wants to select all selected images at once and perform a global action, which won't be common
        //? for large selection and does not have a performance impact on smaller selections, where this action will be more common
        public ImageModel[] GetImagesInAllTabs()
        {
            List<ImageModel> images = new List<ImageModel>();
            foreach (ImageSelectorTabModel selectorTab in ImageSelectorTabs)
            {
                images.AddRange(selectorTab.GetAllItems());
            }

            return images.ToArray();
        }

        // gathers all selected/highlighted images in all image selector tabs
        public ImageModel[] GetAllHighlightedImages()
        {
            List<ImageModel> images = new List<ImageModel>();
            foreach (ImageSelectorTabModel selectorTab in ImageSelectorTabs)
            {
                images.AddRange(selectorTab.GetSelectedItems());
            }

            return images.ToArray();
        }

        private void SelectAllImages()
        {
            foreach (ImageSelectorTabModel selectorTab in ImageSelectorTabs)
            {
                selectorTab.SelectAllItems();
            }
        }

        public void DeselectAllImages()
        {
            foreach (ImageSelectorTabModel selectorTab in ImageSelectorTabs)
            {
                selectorTab.DeselectAllItems();
            }
        }

        // pastes the current tagBoard selection to all highlighted images
        private void PasteTagBoard()
        {
            foreach (ImageModel image in GetAllHighlightedImages())
            {
                foreach (TagModel tag in TagViewModel.Instance.TagBoardTags)
                {
                    image.AddTag(tag, false);
                }
            }

            TaggingUtil.HighlightTags();
        }

        /// <summary>
        /// Checks for potentially deleted images and removes them accordingly
        /// </summary>
        /// <param name="tab">the Image Selector Tab to be scanned</param>
        public void VerifyImageSelectorTab(ImageSelectorTabModel tab) => tab.VerifyImages();

        #region Inspector

        private void ToggleInspector()
        {
            InspectorToggle = !InspectorToggle;
            RaisePropertyChanged(() => InspectorToggle);
        }

        private void OpenImageEditor()
        {
            InspectorToggle = true;
            RaisePropertyChanged(() => InspectorToggle);
        }

        private void CloseInspector()
        {
            InspectorToggle = false;
            RaisePropertyChanged(() => InspectorToggle);
        }

        public void SetInspectorHeight(double newHeight) => InspectorHeight = newHeight;

        #endregion

        #endregion
        #endregion
    }
}