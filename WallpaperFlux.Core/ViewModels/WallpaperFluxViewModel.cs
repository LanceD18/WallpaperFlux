using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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
using LanceTools.IO;
using LanceTools.WPF.Adonis.Util;
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
using WallpaperFlux.Core.Managers;
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

        /// Active Wallpapers
        private MvxObservableCollection<BaseImageModel> _activeWallpapers = new MvxObservableCollection<BaseImageModel>();

        public MvxObservableCollection<BaseImageModel> ActiveWallpapers
        {
            get => _activeWallpapers;
            set => SetProperty(ref _activeWallpapers, value);
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
                if (SelectedImage == null) return "";

                if (SelectedImageCount <= 1)
                {
                    if (SelectedImage is ImageModel selectedImage)
                    {
                        return selectedImage.Path;
                    }
                    else
                    {
                        return "";
                    }
                }
                else
                {
                    return "Selected Images: " + SelectedImageCount;
                }
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

                //xTaggingUtil.HighlightTags();
            }
        }

        //? Updated by RaisePropertyChanged() calls in references
        public string SelectedImageDimensionsText
        {
            get
            {
                if (SelectedImage == null) return ""; // nothing is currently selected

                if (SelectedImage is ImageModel selectedImage)
                {
                    Size size = selectedImage.GetSize();

                    if (selectedImage.IsInRelatedImageSet && !ImageSetInspectorToggle)
                    {
                        //! this shouldn't happen but it has, here's a hacky fix
                        SelectedImage = selectedImage.ParentRelatedImageModel;
                    }
                    else
                    {
                        return size.Width + "x" + size.Height;
                    }
                }

                if (SelectedImage is ImageSetModel selectedImageSet)
                {
                    return "Images in Set: " + selectedImageSet.RelatedImages.Length;
                }

                return "[ERROR]";
            }
        }

        private BaseImageModel _selectedImage;
        public BaseImageModel SelectedImage
        {
            get => _selectedImage;
            set
            {
                SetProperty(ref _selectedImage, value);

                RaisePropertyChanged(() => SelectedImagePathText);
                RaisePropertyChanged(() => SelectedImageDimensionsText);
                RaisePropertyChanged(() => IsImageSelected);
                RaisePropertyChanged(() => InspectedImageTags);

                if (value is ImageModel imageModel)
                {
                    SelectedImageModel = imageModel;
                    RaisePropertyChanged(() => SelectedImageModel);
                }

                MuteIfInspectorHasAudio(); // changing the selected image may change the inspector to a video with audio, in this case, mute wallpapers with audio
            }
        } //? for the xaml

        public ImageModel SelectedImageModel { get; set; }

        public bool TogglingAllSelections = false;

        #region Inspector

        //? Updated by RaisePropertyChanged() calls in references
        public MvxObservableCollection<TagModel> InspectedImageTags
        {
            get
            {
                if (SelectedImage is ImageModel selectedImage)
                {
                    HashSet<TagModel> tags = selectedImage.Tags.GetTags();

                    foreach (TagModel tag in tags)
                    {
                        tag.RaisePropertyChanged(() => tag.ExceptionColor); //? we don't want to raise this property for *every single tag* on every inspector change so we'll do this here
                        tag.RaisePropertyChanged(() => tag.ExceptionText); //? we don't want to raise this property for *every single tag* on every inspector change so we'll do this here
                    }

                    return new MvxObservableCollection<TagModel>(tags);
                }

                return null;
            }
        }

        private double _inspectorHeight;
        public double InspectorHeight
        {
            get => _inspectorHeight;
            set //? needed to update the height when resizing the window
            {
                SetProperty(ref _inspectorHeight, value);
                RaisePropertyChanged(() => ImageSetInspectorListBoxHeight);
            }
        }

        public double ImageSetInspectorListBoxHeight => InspectorHeight - 100;

        public MvxObservableCollection<ImageModel> InspectedImageSetImages
        {
            get
            {
                if (InspectedImageSet != null && ImageSetInspectorToggle) return new MvxObservableCollection<ImageModel>(InspectedImageSet.RelatedImages); // update active set view

                if (SelectedImage == null) return null;

                if (SelectedImage is ImageSetModel selectedSet)
                {
                    InspectedImageSet = selectedSet;
                    return new MvxObservableCollection<ImageModel>(selectedSet.RelatedImages);
                }

                return null;
            }
        }

        public ImageSetModel InspectedImageSet;

        public string InspectedImageRankText
        {
            get
            {
                if (InspectedImageSet != null)
                {
                    return "Rank: " + InspectedImageSet.Rank;
                }

                return "";
            }
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
                foreach (Stack<BaseImageModel> previousWallpaperSet in ThemeUtil.Theme.WallpaperRandomizer.PreviousWallpapers)
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

        private bool _inspectorToggle;
        public bool InspectorToggle
        {
            get => _inspectorToggle;
            set
            {
                SetProperty(ref _inspectorToggle, value);

                MuteIfInspectorHasAudio();
            }
        }

        private bool _imageSetInspectorToggle;
        public bool ImageSetInspectorToggle
        {
            get => _imageSetInspectorToggle;
            set
            {

                SetProperty(ref _imageSetInspectorToggle, value);

                if (!value)
                {
                    // deselect all potentially selected images in the inspected image set
                    foreach (ImageModel image in InspectedImageSet.RelatedImages) image.IsSelected = false;

                    InspectedImageSet = null;
                }

                if (value)
                {
                    RaisePropertyChanged(() => InspectedImageSetImages);
                    RaisePropertyChanged(() => InspectedImageRankText);
                    DeselectAllImages(); // don't want any regular images selected when viewing a set
                }
            }
        }

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

        public IMvxCommand DeRenderWinformCommand { get; set; }

        public IMvxCommand OpenWindowsCommand { get; set; }

        public IMvxCommand CloseAppCommand { get; set; }

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

        public IMvxCommand CreateRelatedImageSetCommand { get; set; }

        public IMvxCommand AddToImageSetCommand { get; set; }

        public IMvxCommand RemoveFromSetCommand { get; set; }

        #endregion

        #region Inspector

        public IMvxCommand ToggleInspectorCommand { get; set; }

        public IMvxCommand CloseInspectorCommand { get; set; }

        public IMvxCommand CloseImageSetInspectorCommand { get; set; }

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

            ImageFolders.CollectionChanged += ImageFoldersOnCollectionChanged;
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
            DeRenderWinformCommand = new MvxCommand(DeRenderWinform);

            OpenWindowsCommand = new MvxCommand(WallpaperUtil.AppUtil.OpenWindows);
            CloseAppCommand = new MvxCommand(WallpaperUtil.AppUtil.CloseApp);

            // Image Selector
            ClearImagesCommand = new MvxCommand(ClearImages);
            SelectImagesCommand = new MvxCommand(PromptImageSelectorRebuild);
            PasteTagBoardCommand = new MvxCommand(PasteTagBoard);
            SelectAllImagesCommand = new MvxCommand(SelectAllImages);
            DeselectImagesCommand = new MvxCommand(DeselectAllImages);
            RenameImagesCommand = new MvxCommand(() => ImageRenamer.AutoRenameImageRange(GetAllHighlightedImages()));
            MoveImagesCommand = new MvxCommand(() => ImageRenamer.AutoMoveImageRange(GetAllHighlightedImages()));
            DeleteImagesCommand = new MvxCommand(() => ImageUtil.DeleteImageRange(GetAllHighlightedImages()));
            RankImagesCommand = new MvxCommand(() => ImageUtil.PromptRankImageRange(GetAllHighlightedImages()));
            CreateRelatedImageSetCommand = new MvxCommand(() => ImageUtil.CreateRelatedImageSet(GetAllHighlightedImages(true, true), true));
            AddToImageSetCommand = new MvxCommand(AddToImageSet);
            RemoveFromSetCommand = new MvxCommand(RemoveFromImageSet);

            // Image Inspector
            ToggleInspectorCommand = new MvxCommand(ToggleInspector);
            CloseInspectorCommand = new MvxCommand(CloseInspector);
            CloseImageSetInspectorCommand = new MvxCommand(CloseImageSetInspector);
        }

        public void MuteIfInspectorHasAudio()
        {
            if (SelectedImage is ImageModel selectedImage && InspectorToggle && selectedImage.IsVideoOrGif) // inspector on, mute all wallpapers if the given wallpaper is a video
            {
                if (WallpaperUtil.VideoUtil.HasAudio(selectedImage.Path))
                {
                    WallpaperUtil.MuteWallpapers();
                }
            }
            else // vice versa, unmute if inspector is turned off or the inspected image is not a video
            {
                WallpaperUtil.UnmuteWallpapers();
            }
        }

        public void UpdateActiveWallpapers(BaseImageModel wallpaper, int changedIndex)
        {
            while (ActiveWallpapers.Count != WallpaperUtil.DisplayUtil.GetDisplayCount()) // ensure that we are at the display count
            {
                if (ActiveWallpapers.Count < WallpaperUtil.DisplayUtil.GetDisplayCount())
                {
                    ActiveWallpapers.Add(null);
                }

                if (ActiveWallpapers.Count > WallpaperUtil.DisplayUtil.GetDisplayCount())
                {
                    ActiveWallpapers.RemoveAt(ActiveWallpapers.Count - 1);
                }
            }

            ActiveWallpapers[changedIndex] = wallpaper;
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
                BaseImageModel prevWallpaper = ThemeUtil.Theme.WallpaperRandomizer.PreviousWallpapers[index].Pop();
                ThemeUtil.Theme.WallpaperRandomizer.NextWallpapers[index] = prevWallpaper;

                if (prevWallpaper is ImageModel imageModel && !FileUtil.Exists(imageModel.Path))
                {
                    return;
                }

                WallpaperUtil.SetWallpaper(index, true, true); // ignoreRandomization = true since there is no need to randomize wallpapers that have previously existed
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
            //xbool loadingOld = MessageBoxUtil.PromptYesNo("Load old data? [Remove this prompt when you no longer rely on the old data]");

            using (CommonOpenFileDialog dialog = new CommonOpenFileDialog())
            {
                //x dialog.InitialDirectory = lastSelectedPath;
                dialog.Multiselect = false;
                dialog.Title = "Select a theme";
                dialog.Filters.Add(new CommonFileDialogFilter(JsonUtil.JSON_FILE_DISPLAY_NAME, JsonUtil.JSON_FILE_EXTENSION));

                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    JsonUtil.SetIsLoadingData(true);

                    JsonUtil.ConvertTheme(JsonUtil.LoadData(dialog.FileName));

                    JsonUtil.SetIsLoadingData(false);
                }
            }
        }

        // updates some properties that may change during runtime but shouldn't be processed constantly
        public void UpdateTheme()
        {
            Debug.WriteLine("Updating theme...");
            // TODO This check will only matter once you use 'Update Theme' to check for the existence of new images
            //xThemeUtil.Theme.Settings.ThemeSettings.FrequencyCalc.VerifyImageTypeExistence();
            //xJsonUtil.QuickSave();
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
                    if (!ImageFolders.ContainsFolderPath(dialog.FileName))
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
                folders.Add(new FolderModel(folder.Path, folder.Enabled, folder.PriorityName));
            }

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
                        alreadyExistsString += "\n" + folder.Path;
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

            //xUpdateTheme();
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

            //xUpdateTheme();
        }

        public bool ContainsFolder(string path) => ImageFolders.ContainsFolderPath(path);

        private void ImageFoldersOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!JsonUtil.IsLoadingData) //! the order of operations done while loading prevents this from being needed
            {
                foreach (FolderModel newFolders in e.NewItems) //? re-validate newly added folders
                {
                    newFolders.ValidateImages(true); // the images won't be able to find the folder until it is added
                }
            }
        }

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
        public void PromptImageSelectorRebuild()
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

        //! This variant of RebuildImageSelector is typically used by Folder Selection
        public void RebuildImageSelector(string[] selectedImages, bool randomize = false, bool reverseOrder = false, bool dateTime = false)
        {
            List<BaseImageModel> selectedImageModels = new List<BaseImageModel>();

            int invalidCounter = 0;
            string invalidImageString = INVALID_IMAGE_STRING_DEFAULT;

            foreach (string imagePath in selectedImages)
            {
                ImageModel image = ThemeUtil.Theme.Images.GetImage(imagePath);

                if (image != null)
                {
                    BaseImageModel imageToAdd;
                    if (image.IsInRelatedImageSet)
                    {
                        imageToAdd = image.ParentRelatedImageModel;

                        if (selectedImageModels.Contains(imageToAdd))
                        {
                            continue; //? Given that this is a set of images, we may encounter the set multiple times. If this occurs, skip it
                        }
                    }
                    else
                    {
                        imageToAdd = image;
                    }

                    selectedImageModels.Add(imageToAdd);
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

            RebuildImageSelector(selectedImageModels.ToArray(), randomize, reverseOrder, dateTime);
        }

        //? ----- Rebuild Image Selector (ImageModel) -----
        public void RebuildImageSelector(BaseImageModel[] selectedImages, bool randomize = false, bool reverseOrder = false, bool dateTime = false)
        {
            //-----Checking Validation Conditions-----
            if (selectedImages == null || selectedImages.Length == 0)
            {
                MessageBoxUtil.ShowError("No images were selected");
                return;
            }

            //----- Apply Optional Modification Conditions -----
            //? should come after checking conditions, no need to process this if something is wrong
            if (randomize)
            {
                selectedImages = selectedImages.Randomize().ToArray();
            }
            else//? it is redundant to randomize and reverse the order at the same time as the randomization will end up being the only factor
            {
                // TODO Make the redundancy apparent in the interface by disabling everything else if random is picked
                if (dateTime)
                {
                    selectedImages = selectedImages.OrderBy(f => new FileInfo(ImageUtil.GetImageModel(f).Path).CreationTime).ToArray();
                }

                //! order by date time before reversing

                if (reverseOrder)
                {
                    selectedImages = selectedImages.Reverse().ToArray();
                }
            }

            //-----Rebuild-----
            ImageSelectorTabs.Clear();

            int tabCount = (selectedImages.Length / IMAGES_PER_PAGE) + 1;
            int imageIndex = 0;

            int invalidCounter = 0;
            string invalidImageString = INVALID_IMAGE_STRING_DEFAULT;

            HashSet<ImageSetModel> encounteredSets = new HashSet<ImageSetModel>();

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
                        BaseImageModel image = selectedImages[j + imageIndex];

                        bool success = false;
                        
                        if (image != null /*x&& ThemeUtil.Theme.Images.ContainsImage(image) ThemeUtil.Theme.Images.ContainsImage(image.Path, image.ImageType)*/)
                        {
                            Func<ImageSetModel, bool> checkForSet = set =>
                            {
                                if (!encounteredSets.Contains(set))
                                {
                                    encounteredSets.Add(set);
                                    image = set;
                                    return true;
                                }

                                return false;
                            };

                            if (image is ImageModel imageModel)
                            {
                                if (imageModel.IsInRelatedImageSet)
                                {
                                    // not much of a downside to using IsEnabled() on sets, sets have significantly less IsEnabled() checks anyways
                                    if (imageModel.ParentRelatedImageModel.IsEnabled()) //! this should NOT be combined with the above if statement! (would conflict with the else)
                                    {
                                        success = checkForSet.Invoke(imageModel.ParentRelatedImageModel);
                                    }
                                }
                                else if ((ThemeUtil.ThemeSettings.EnableDetectionOfInactiveImages && ThemeUtil.Theme.Images.ContainsImage(image)) 
                                         || image.Active) //? we don't need to send an error message for user-disabled images
                                {
                                    success = true;
                                }
                            }
                            else if (image is ImageSetModel imageSet)
                            {
                                success = checkForSet.Invoke(imageSet);
                            }

                            if (success)
                            {
                                tabModel.Items.Add(image);
                            }
                        }
                        else
                        {
                            invalidCounter++;

                            invalidImageString += "\n";

                            if (image is ImageModel imageModel)
                            {
                                invalidImageString += imageModel.Path;
                            }
                            else if (image is ImageSetModel)
                            {
                                invalidImageString += "<Image Set>";
                            }
                            else
                            {
                                invalidImageString += "[NULL]";
                            }
                        }

                        if (!success) // prevents creating null space
                        {
                            j--;
                            imageIndex++;
                        }
                    }
                    else
                    {
                        break; // we've reached the cutoff, stop producing pages
                    }
                }

                imageIndex += IMAGES_PER_PAGE; // ensures that we traverse through selectedImages[] properly

                //x tabModel.RaisePropertyChangedImages();

                if (tabModel.GetAllItems().Length > 0) // don't add the page if there's nothing there
                {
                    ImageSelectorTabs.Add(tabModel);
                }
            }
            
            RaisePropertyChanged(() => ImageSelectorTabs);
            if (ImageSelectorTabs.Count > 0) SelectedImageSelectorTab = ImageSelectorTabs[0];
            SelectedImageCount = 0; // without this the count will jump up to the number of images in the previous selection and be unable to reset

            // ----- Final Validations -----
            if (ImageSelectorTabs.Count == 0)
            {
                MessageBoxUtil.ShowError("No valid images were selected");
                return;
            }

            if (invalidCounter == selectedImages.Length) // TODO Pretty sure this is inaccessible now, decide what to do with it
            {
                MessageBoxUtil.ShowError(INVALID_IMAGE_STRING_ALL_INVALID);
                //x //? we don't return here because the collection was modified
                return;
            }

            if (invalidCounter > 0)
            {
                MessageBoxUtil.ShowError(invalidImageString);
                return;
            }
        }

        private void ClearImages()
        {
            DeselectAllImages();
            ImageSelectorTabs.Clear();
        }

        // gathers all images across all image selector tabs
        //? While initializing this in RebuildImageSelectorWithTagOptions() is an option, keep in mind that doing so will bring additional load times that don't always apply to the use case
        //? each search, so it's best to just wait for when the user *actually* wants to select all selected images at once and perform a global action, which won't be common
        //? for large selection and does not have a performance impact on smaller selections, where this action will be more common
        public ImageModel[] GetAllImagesInTabsOrSet()
        {
            if (!ImageSetInspectorToggle)
            {
                List<ImageModel> images = new List<ImageModel>();
                foreach (ImageSelectorTabModel selectorTab in ImageSelectorTabs)
                {
                    images.AddRange(selectorTab.GetAllImages());

                    foreach (ImageSetModel imageSet in selectorTab.GetAllSets())
                    {
                        images.AddRange(imageSet.RelatedImages);
                    }
                }

                return images.ToArray();
            }
            else
            {
                return InspectedImageSet.RelatedImages;
            }
        }

        // gathers all selected/highlighted images in all image selector tabs
        public ImageModel[] GetAllHighlightedImages(bool ignoreSets = false, bool cancelIfSetFound = false)
        {
            List<ImageModel> images = new List<ImageModel>();

            if (!ImageSetInspectorToggle)
            {
                foreach (ImageSelectorTabModel selectorTab in ImageSelectorTabs)
                {
                    if (cancelIfSetFound)
                    {
                        if (selectorTab.GetSelectedSets().Length > 0)
                        {
                            MessageBoxUtil.ShowError("Additional sets found, operation cancelled");
                            return null;
                        }
                    }

                    images.AddRange(selectorTab.GetSelectedImages());

                    if (!ignoreSets)
                    {
                        foreach (ImageSetModel imageSet in selectorTab.GetSelectedSets())
                        {
                            images.AddRange(imageSet.RelatedImages);
                        }
                    }
                }
            }
            else
            {
                foreach (ImageModel image in InspectedImageSet.RelatedImages)
                {
                    if (image.IsSelected)
                    {
                        images.Add(image);
                    }
                }
            }

            return images.ToArray();
        }

        public BaseImageModel[] GetAllHighlightedImageItems()
        {
            List<BaseImageModel> images = new List<BaseImageModel>();
            foreach (ImageSelectorTabModel selectorTab in ImageSelectorTabs)
            {
                images.AddRange(selectorTab.GetSelectedItems());
            }

            return images.ToArray();
        }

        public ImageSelectorTabModel GetSelectorTabOfImage(BaseImageModel image)
        {
            foreach (ImageSelectorTabModel imageSelectorTab in ImageSelectorTabs)
            {
                if (imageSelectorTab.Items.Contains(image))
                {
                    return imageSelectorTab;
                }
            }

            return null;
        }

        public bool RemoveImageFromTab(BaseImageModel image)
        {
            return RemoveImageFromTab(image, out _);
        }

        public bool RemoveImageFromTab(BaseImageModel image, out int index)
        {
            index = -1;
            ImageSelectorTabModel imageSelectorTab = GetSelectorTabOfImage(image);
            if (imageSelectorTab == null) return false;

            index = imageSelectorTab.Items.IndexOf(image);
            return imageSelectorTab.Items.Remove(image);
        }

        //! If this needs to be changed from ImageModel[] to BaseImageModel[], be sure to account for the errors associated with array casting from ImageModel[] to BaseImageModel[]
        public void RemoveImageRangeFromTabs(ImageModel[] images)
        {
            HashSet<ImageSelectorTabModel> tabsToEdit = new HashSet<ImageSelectorTabModel>();
            foreach (ImageModel image in images)
            {
                tabsToEdit.Add(GetSelectorTabOfImage(image));
            }

            foreach (ImageSelectorTabModel imageSelectorTab in tabsToEdit)
            {
                if (imageSelectorTab == null) continue;

                BaseImageModel[] tabImages = imageSelectorTab.Items.ToArray();

                foreach (BaseImageModel image in tabImages)
                {
                    if (images.Contains(image))
                    {
                        imageSelectorTab.RemoveImage(image);
                    }
                }
            }
        }

        public void ReplaceImageWithSet(ImageModel image, ImageSetModel imageSet)
        {
            // TODO Remove if not given a purpose
            RemoveImageFromTab(image, out int index);
        }

        public void AddSetToTab(ImageSetModel image)
        {
            // TODO Remove if not given a purpose
        }

        private void SelectAllImages()
        {
            TogglingAllSelections = true;
            foreach (ImageSelectorTabModel selectorTab in ImageSelectorTabs)
            {
                selectorTab.SelectAllItems();
            }
            TogglingAllSelections = false;

            TaggingUtil.HighlightTags();
        }

        public void DeselectAllImages()
        {
            TogglingAllSelections = true;
            foreach (ImageSelectorTabModel selectorTab in ImageSelectorTabs)
            {
                selectorTab.DeselectAllItems();
            }
            TogglingAllSelections = false;

            TaggingUtil.HighlightTags();
        }

        // pastes the current tagBoard selection to all highlighted images
        private void PasteTagBoard()
        {
            foreach (ImageModel image in GetAllHighlightedImages())
            {
                foreach (TagModel tag in TagViewModel.Instance.TagBoardTags)
                {
                    image.AddTag(tag, false); //! TAG HIGHLIGHT DONE BELOW
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

        public void ToggleInspector()
        {
            if (SelectedImage is ImageModel)
            {
                InspectorToggle = !InspectorToggle;
            }

            if (SelectedImage is ImageSetModel)
            {
                ImageSetInspectorToggle = !ImageSetInspectorToggle;
            }
        }

        public void OpenInspector() => InspectorToggle = true;

        public void CloseInspector() => InspectorToggle = false;

        public void CloseImageSetInspector() => ImageSetInspectorToggle = false;

        public void SetInspectorHeight(double newHeight) => InspectorHeight = newHeight;

        #endregion

        #region Image Sets

        private void AddToImageSet()
        {
            List<ImageModel> imagesFound = new List<ImageModel>();
            List<ImageSetModel> imageSetsFound = new List<ImageSetModel>();

            foreach (BaseImageModel image in GetAllHighlightedImageItems())
            {
                if (image is ImageSetModel imageSet)
                {
                    imageSetsFound.Add(imageSet);

                    if (imageSetsFound.Count > 1)
                    {
                        MessageBoxUtil.ShowError("Operation cancelled. You can only add images to one set");
                        return;
                    }
                }

                if (image is ImageModel imageModel)
                {
                    imagesFound.Add(imageModel);
                }
            }

            if (imageSetsFound.Count == 0)
            {
                // TODO the option to add to sets should be greyed out while no sets are selected
                MessageBoxUtil.ShowError("No set specified to add images to");
                return;
            }

            ImageUtil.AddToImageSet(imagesFound.ToArray(), imageSetsFound.First());
        }

        private void RemoveFromImageSet()
        {
            if (ImageSetInspectorToggle && InspectedImageSet != null)
            {
                List<ImageModel> imagesFound = new List<ImageModel>();

                foreach (ImageModel image in InspectedImageSet.RelatedImages)
                {
                    if (image.IsSelected)
                    {
                        imagesFound.Add(image);
                    }
                }

                ImageUtil.RemoveFromImageSet(imagesFound.ToArray(), InspectedImageSet);
            }
        }

        #endregion

        #endregion

        public void DeRenderWinform() => WallpaperUtil.WallpaperHandler.DisableMpv();

        #endregion
    }
}