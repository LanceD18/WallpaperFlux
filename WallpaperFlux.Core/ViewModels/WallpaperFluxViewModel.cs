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
using Microsoft.WindowsAPICodePack.Dialogs;
using Microsoft.WindowsAPICodePack.Shell.PropertySystem;
using MvvmCross;
using MvvmCross.Binding.BindingContext;
using MvvmCross.Commands;
using MvvmCross.Core;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using WallpaperFlux.Core.Collections;
using WallpaperFlux.Core.External;
using WallpaperFlux.Core.JSON.Temp;
using WallpaperFlux.Core.Models;
using WallpaperFlux.Core.Models.Controls;
using WallpaperFlux.Core.Models.Tagging;
using WallpaperFlux.Core.Models.Theme;
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
        public ThemeModel Theme { get; set; } = DataUtil.Theme;

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

        public ImageModel SelectedImage => SelectedImageSelectorTab?.SelectedImage;

        #region Inspector

        //? Updated by RaisePropertyChanged() calls in references
        public MvxObservableCollection<TagModel> InspectedImageTags
        {
            get
            {
                if (SelectedImageSelectorTab == null || SelectedImageSelectorTab.SelectedImage == null) return null;
                
                return new MvxObservableCollection<TagModel>(SelectedImageSelectorTab.SelectedImage.Tags.GetTags());
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

        #region Enablers
        //-----Enablers-----
        public bool CanNextWallpaper => ImageFolders.Count > 0;

        public bool CanPreviousWallpaper => false;

        public bool CanRemoveWallpaper => SelectedImageFolder != null;

        public bool CanSync => SelectedDisplaySetting != null;

        public bool CanSelectImages => ImageFolders.Count > 0;

        public bool InspectorToggle { get; set; } = false;

        #endregion

        //xprivate readonly IMvxNavigationService _navigationService;

        public WallpaperFluxViewModel(/*xIMvxNavigationService navigationService*/)
        {
            Instance = this;

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
                MvxObservableCollection<DisplayModel> initSettings = new MvxObservableCollection<DisplayModel>();
                for (int i = 0; i < WallpaperUtil.DisplayUtil.GetDisplayCount(); i++)
                {
                    initSettings.Add(new DisplayModel(Mvx.IoCProvider.Resolve<IExternalTimer>(), NextWallpaper)
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

        #region Commands
        //-----Commands-----
        
        public IMvxCommand NextWallpaperCommand { get; set; }

        public IMvxCommand PreviousWallpaperCommand { get; set; }

        public IMvxCommand LoadThemeCommand { get; set; }

        public IMvxCommand AddFolderCommand { get; set; }

        public IMvxCommand RemoveFolderCommand { get; set; }

        public IMvxCommand SyncCommand { get; set; }

        #region Image Selector

        public IMvxCommand ClearImagesCommand { get; set; }

        public IMvxCommand SelectImagesCommand { get; set; }

        public IMvxCommand PasteTagBoardCommand { get; set; }

        public IMvxCommand SelectAllImagesCommand { get; set; }

        public IMvxCommand DeselectImagesCommand { get; set; }

        #endregion

        #region Inspector

        public IMvxCommand ToggleInspectorCommand { get; set; }

        public IMvxCommand CloseInspectorCommand { get; set; }
        
        #endregion

        #endregion

        public void InitializeCommands()
        {
            // TODO Consider using models to hold command information (Including needed data)
            NextWallpaperCommand = new MvxCommand(NextWallpaper);
            PreviousWallpaperCommand = new MvxCommand(() => { MessageBox.Show("Not implemented"); });
            LoadThemeCommand = new MvxCommand(LoadTheme);
            AddFolderCommand = new MvxCommand(PromptAddFolder);
            RemoveFolderCommand = new MvxCommand(RemoveFolder);
            SyncCommand = new MvxCommand(Sync);

            ClearImagesCommand = new MvxCommand(ClearImages);
            SelectImagesCommand = new MvxCommand(SelectImages);
            PasteTagBoardCommand = new MvxCommand(PasteTagBoard);
            SelectAllImagesCommand = new MvxCommand(SelectAllImages);
            DeselectImagesCommand = new MvxCommand(DeselectAllImages);

            ToggleInspectorCommand = new MvxCommand(ToggleInspector);
            CloseInspectorCommand = new MvxCommand(CloseInspector);
        }

        #region Command Methods
        // TODO Create subclasses for the commands with excess methods (Like Theme Data)
        //  -----Command Methods-----
        #region Wallpaper Setters
        public void NextWallpaper()
        {
            // TODO Turn this into a general method, refer to WallpaperManager.Pathing
            if (Theme.Images.GetAllImages().Length > 0)
            {
                for (int i = 0; i < WallpaperUtil.DisplayUtil.GetDisplayCount(); i++)
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

        // TODO Check if the theme is finished loading before activating, check if any images are active
        public void NextWallpaper(int displayIndex, bool isCallerTimer)
        {
            if (Theme.Images.GetAllImages().Length > 0 && Theme.RankController.GetAllRankedImages().Length > 0)
            {
                // ignoreRandomization = false here since we need to randomize the new set
                // Note that RandomizeWallpaper() will check if it even should randomize the wallpapers first (Varied timers and extended videos can undo this requirement)
                WallpaperUtil.SetWallpaper(displayIndex, false);
            }
        }

        /* TODO
        public void PreviousWallpaper()
        {
            for (int i = 0; i < WallpaperPathSetter.PreviousWallpapers.Length; i++)
            {
                PreviousWallpaper(i);
            }
        }

        // sets all wallpapers to their previous wallpaper, if one existed
        public void PreviousWallpaper(int index)
        {
            if (WallpaperPathSetter.PreviousWallpapers[index].Count > 0)
            {
                WallpaperPathSetter.NextWallpapers[index] = WallpaperPathSetter.PreviousWallpapers[index].Pop();

                if (File.Exists(WallpaperPathSetter.NextWallpapers[index]))
                {
                    ResetTimer(index);
                    WallpaperUtil.SetWallpaper(index, true); // ignoreRandomization = true since there is no need to randomize wallpapers that have previously existed
                }
            }
            // Not needed here, we can just disable the button
            //xelse
            //x{
            //x    MessageBox.Show("There are no more previous wallpapers");
            //x}
        }
        */
        #endregion

        #region Theme Data

        // updates some properties that may change during runtime but shouldn't be processed constantly
        // TODO This should be attached to the eventual Update Theme/Quick Save Button
        public void UpdateTheme()
        {
            Debug.WriteLine("Updating theme...");
            Theme.Settings.ThemeSettings.FrequencyCalc.VerifyImageTypeExistence();
        }

        public void LoadTheme()
        {
            using (CommonOpenFileDialog dialog = new CommonOpenFileDialog())
            {
                //x dialog.InitialDirectory = lastSelectedPath;
                dialog.Multiselect = false;
                dialog.Title = "Select a theme";
                dialog.Filters.Add(new CommonFileDialogFilter(JsonUtil.JSON_FILE_DISPLAY_NAME, JsonUtil.JSON_FILE_EXTENSION));

                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    JsonUtil.IsLoadingData = true;
                    ConvertOldTheme(JsonUtil.LoadData(dialog.FileName));
                    JsonUtil.IsLoadingData = false;
                }
            }
        }

        public void SaveTheme()
        {
            //! Do NOT save Child Tags! Those are handled on their own whenever the Parent Tag is linked!

        }

        // TODO Consider moving this to JsonUtil or its own class (i.e., DataConverter.cs) [JsonUtil will do just fine though and well, serve its purpose]
        // TODO Consider moving this to JsonUtil or its own class (i.e., DataConverter.cs) [JsonUtil will do just fine though and well, serve its purpose]
        // TODO Consider moving this to JsonUtil or its own class (i.e., DataConverter.cs) [JsonUtil will do just fine though and well, serve its purpose]
        // TODO Consider moving this to JsonUtil or its own class (i.e., DataConverter.cs) [JsonUtil will do just fine though and well, serve its purpose]
        // TODO Consider moving this to JsonUtil or its own class (i.e., DataConverter.cs) [JsonUtil will do just fine though and well, serve its purpose]
        #region JSON Conversion

        private void ConvertTheme( /*JsonWallpaperData wallpaperData*/)
        {
            #region Convert Tags
            //! You will use SimplifiedTag here instead of a Tuple<string, string>
            #endregion

            #region Convert Images
            //! The Dictionary<string, HashSet<string>> from ConvertOldImagesAndFolders should be converted to SimplifiedTagCollection
            #endregion

            #region Reference Me Later
            /*

            //! Do NOT turn the regions here into methods, they are only called once and should only be called right after each other!!!
            //! Do NOT turn the regions here into methods, they are only called once and should only be called right after each other!!!
            //! Do NOT turn the regions here into methods, they are only called once and should only be called right after each other!!!
            //! Do NOT turn the regions here into methods, they are only called once and should only be called right after each other!!!
            //? A local method might be okay but still doesn't enforce singular 'calls' (Unless you want to introduce additional boolean variables)

            Theme.RankController.SetMaxRank(wallpaperData.miscData.maxRank); //! This needs to be done before any images are added

            #region Convert Theme Options
            Debug.WriteLine("Converting Theme Options...");
            // TODO wallpaperData.themeOptions;
            #endregion

            #region Convert Misc
            Debug.WriteLine("Converting Misc...");
            // TODO wallpaperData.miscData;
            #endregion

            #region Convert Tags
            Debug.WriteLine("Converting Tags...");
            // ----- Add Categories -----
            foreach (TempCategoryData tempCat in wallpaperData.tagData)
            {
                Debug.WriteLine("Temp Cat Name: "  + tempCat.Name);
                // verify category before adding tags
                                                // TODO Consider converting the data held up by the TagViewModel Instance into another subset of ThemeModel
                CategoryModel instanceCategory = TaggingUtil.VerifyCategory(tempCat.Name, tempCat.UseForNaming, tempCat.Enabled);

                // ----- Add Tags (of this Category) -----
                foreach (TempTagData tempTag in tempCat.Tags)
                {
                    // verify tag before adding
                    TagModel instanceTag = instanceCategory.VerifyTag(tempTag.Name, tempTag.UseForNaming, tempTag.Enabled);

                    // ----- Add Parent Tags (of this Tag) -----
                    foreach (Tuple<string, string> parentInfo in tempTag.ParentTags)
                    {
                        //? This Tuple should be replace with SimplifiedTag in the official conversion
                        string parentCategoryName = parentInfo.Item1;
                        string parentName = parentInfo.Item2;

                        //! Keep in mind that this tackles both Parent and Child tags, no need to loop through the Child Tag list as well
                        //! (There's no way to directly add child tags anyways)
                        instanceTag.LinkTag(TaggingUtil.VerifyCategory(parentCategoryName).VerifyTag(parentName));
                    }

                    Debug.WriteLine("Adding Tag: " + instanceTag.Name);
                    instanceCategory.AddTag(instanceTag);
                }

                Debug.WriteLine("Adding Category: " + instanceCategory.Name + "\n\n\n");
                TaggingUtil.AddCategory(instanceCategory);
            }
            #endregion

            #region Convert Images & Folders
            Debug.WriteLine("Converting Images...");
            //! Placing this after AddFolderRange() will *significantly* increase load times as the images will attempt to be added multiple times
            // TODO Even with the above statement, this still takes a considerable amount of time to load
            // TODO Some of the lag may have to do with the conversions, it'll likely be a bit better once TempImageData is no longer needed
            foreach (TempImageData imageData in wallpaperData.imageData)
            {
                ImageModel image = new ImageModel(imageData.Path, imageData.Rank);

                ImageTagCollection tags = new ImageTagCollection(image);

                foreach (string categoryName in imageData.Tags.Keys)
                {
                    //? We will not be verifying anything here, that was done in the Convert Tag section. If it doesn't exist, it will not be added (Instead of crashing)
                    CategoryModel category = TaggingUtil.GetCategory(categoryName);

                    if (category == null)
                    {
                        Debug.WriteLine("Category [" + categoryName + "] was referenced while converting images but was not found while converting tags, dropping");
                        continue;
                    }

                    foreach (string tagName in imageData.Tags[categoryName])
                    {
                        TagModel tag = category.GetTag(tagName);

                        if (tag == null)
                        {
                            Debug.WriteLine("Tag [" + tagName + "] was referenced while converting images but was not found while converting tags, dropping");
                            continue;
                        }

                        tags.Add(tag);
                    }
                }

                DataUtil.Theme.Images.AddImage(image);
            }

            //! Folders need to be added after images are added so that they don't have to be verified twice
            AddFolderRange(wallpaperData.imageFolders.Keys.ToArray());
            #endregion

            */
            #endregion
        }

        private void ConvertOldTheme(TemporaryJsonWallpaperData wallpaperData)
        {
            //! Do NOT turn the regions here into methods, they are only called once and should only be called right after each other!!!
            //! Do NOT turn the regions here into methods, they are only called once and should only be called right after each other!!!
            //! Do NOT turn the regions here into methods, they are only called once and should only be called right after each other!!!
            //! Do NOT turn the regions here into methods, they are only called once and should only be called right after each other!!!
            //? A local method might be okay but still doesn't enforce singular 'calls' (Unless you want to introduce additional boolean variables)

            Theme.RankController.SetMaxRank(wallpaperData.miscData.maxRank); //! This needs to be done before any images are added

            #region Convert Theme Options
            Debug.WriteLine("Converting Theme Options...");
            // TODO wallpaperData.themeOptions;
            #endregion

            #region Convert Misc
            Debug.WriteLine("Converting Misc...");
            // TODO wallpaperData.miscData;
            #endregion

            #region Convert Tags
            Debug.WriteLine("Converting Tags...");
            // ----- Add Categories -----
            foreach (TempCategoryData tempCat in wallpaperData.tagData)
            {
                // verify category before adding tags
                                                // TODO Consider converting the data held up by the TagViewModel Instance into another subset of ThemeModel
                CategoryModel instanceCategory = TaggingUtil.VerifyCategory(tempCat.Name, tempCat.UseForNaming, tempCat.Enabled);

                // ----- Add Tags (of this Category) -----
                foreach (TempTagData tempTag in tempCat.Tags)
                {
                    // verify tag before adding
                    TagModel instanceTag = instanceCategory.VerifyTag(tempTag.Name, tempTag.UseForNaming, tempTag.Enabled); // TODO Redundant at the moment but may not be so in the actual conversion
                    instanceTag.ParentCategory = instanceCategory; // not saved into the tag's JSON as it would be unnecessary bloat since the category has this

                    // ----- Add Parent Tags (of this Tag) -----
                    foreach (Tuple<string, string> parentInfo in tempTag.ParentTags)
                    {
                        //? This Tuple should be replace with SimplifiedTag in the official conversion
                        string parentCategoryName = parentInfo.Item1;
                        string parentName = parentInfo.Item2;

                        //! Keep in mind that this tackles both Parent and Child tags, no need to loop through the Child Tag list as well
                        //! (There's no way to directly add child tags anyways)
                        instanceTag.LinkTag(TaggingUtil.VerifyCategory(parentCategoryName).VerifyTag(parentName));
                    }

                    //x Debug.WriteLine("Adding Tag: " + instanceTag.Name);
                    instanceCategory.AddTag(instanceTag); // TODO May want to convert this to AddRange in the actual conversion
                }

                //x Debug.WriteLine("Adding Category: " + instanceCategory.Name + "\n\n\n");
                TaggingUtil.AddCategory(instanceCategory); // TODO May want to convert this to AddRange in the actual conversion
            }
            #endregion

            #region Convert Images & Folders
            Debug.WriteLine("Converting Images...");
            //! Placing this after AddFolderRange() will *significantly* increase load times as the images will attempt to be added multiple times
            // TODO Even with the above statement, this still takes a considerable amount of time to load
            // TODO Some of the lag may have to do with the conversions, it'll likely be a bit better once TempImageData is no longer needed
            foreach (TempImageData imageData in wallpaperData.imageData)
            {
                ImageModel image = new ImageModel(imageData.Path, imageData.Rank);

                ImageTagCollection tags = new ImageTagCollection(image);

                foreach (string categoryName in imageData.Tags.Keys)
                {
                    //? We will not be verifying anything here, that was done in the Convert Tag section. If it doesn't exist, it will not be added (Instead of crashing)
                    CategoryModel category = TaggingUtil.GetCategory(categoryName);

                    if (category == null)
                    {
                        Debug.WriteLine("Category [" + categoryName + "] was referenced while converting images but was not found while converting tags, dropping");
                        continue;
                    }

                    foreach (string tagName in imageData.Tags[categoryName])
                    {
                        TagModel tag = category.GetTag(tagName);

                        if (tag == null)
                        {
                            Debug.WriteLine("Tag [" + tagName + "] was referenced while converting images but was not found while converting tags, dropping");
                            continue;
                        }

                        tags.Add(tag);
                    }
                }

                //x Debug.WriteLine("Adding Image: " + image.PathName);
                DataUtil.Theme.Images.AddImage(image);
            }

            //! Folders need to be added after images are added so that they don't have to be verified twice
            AddFolderRange(wallpaperData.imageFolders.Keys.ToArray());
            #endregion
        }
        #endregion

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

        public void AddFolder(string path) => AddFolderRange(new string[] { path });

        public void AddFolderRange(string[] paths)
        {
            string errorOutput = "Could not add the folder(s):\n";
            bool errorOccured = false;

            foreach (string path in paths)
            {
                if (Directory.Exists(path))
                {
                    ImageFolders.Add(new FolderModel(path, true));
                }
                else
                {
                    errorOutput += path + "\n";
                    errorOccured = true;
                }
            }

            if (errorOccured) MessageBoxUtil.ShowError(errorOutput);

            RaisePropertyChanged(() => CanNextWallpaper);
            RaisePropertyChanged(() => CanSelectImages);

            UpdateTheme();
        }

        // TODO Figure out how to remove multiple folders at once
        public void RemoveFolder()
        {
            Debug.WriteLine("Removing: " + SelectedImageFolder.Path);
            SelectedImageFolder.DeactivateFolder();
            ImageFolders.Remove(SelectedImageFolder);
            //xImageFolders.ValidateImageFolders();

            UpdateTheme();
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
                    }
                }
            }
            else if (messageBox.ButtonPressed.Id == OTHER_BUTTON_ID)
            {
                // do thing
            }
        }

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

            if (invalidCounter > 0)
            {
                MessageBoxUtil.ShowError("Some of the selected images do not exist: \n" + invalidImageString);
                return;
            }

            if (invalidCounter == selectedImages.Length)
            {
                MessageBoxUtil.ShowError("All selected images do not exist");
                return;
            }

            //-----Rebuild-----
            ImageSelectorTabs.Clear();

            int tabCount = (selectedImages.Length / IMAGES_PER_PAGE) + 1;

            int imageIndex = 0;
            
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
                        tabModel.Items.Add(Theme.Images.GetImage(selectedImages[j + imageIndex]));
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
                    image.AddTag(tag);
                }
            }
        }

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