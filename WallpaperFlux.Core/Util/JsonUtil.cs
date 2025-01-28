using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using LanceTools.IO;
using LanceTools.WPF.Adonis.Util;
using MvvmCross.ViewModels;
using Newtonsoft.Json;
using WallpaperFlux.Core.Collections;
using WallpaperFlux.Core.JSON;
using WallpaperFlux.Core.JSON.Temp;
using WallpaperFlux.Core.Models;
using WallpaperFlux.Core.Models.Tagging;
using WallpaperFlux.Core.Models.Theme;
using WallpaperFlux.Core.ViewModels;

namespace WallpaperFlux.Core.Util
{
    //! Temp
    #region Temporary
    public class TemporaryJsonWallpaperData
    {
        [JsonProperty("ThemeOptions")] public TempThemeOptions themeOptions;

        [JsonProperty("MiscData")] public TempMiscData miscData;

        [JsonProperty("ImageFolders")] public Dictionary<string, bool> imageFolders;

        [JsonProperty("TagData")] public TempCategoryData[] tagData;
        
        [JsonProperty("ImageData")] public TempImageData[] imageData;

        public TemporaryJsonWallpaperData(TempMiscData miscData, TempThemeOptions themeOptions, TempCategoryData[] tagData, TempImageData[] imageData, Dictionary<string, bool> imageFolders)
        {
            //? This was not needed to load information properly in the previous version for an arbitrary reason, it is now though

            this.miscData = miscData; //x new MiscData(); // TODO values are updated in the constructor [THIS CONSTRUCTOR IS CURRENTLY UNFINISHED]
            this.themeOptions = themeOptions; //xOptionsData.ThemeOptions;
            this.imageFolders = imageFolders;
            this.tagData = tagData; //x TaggingInfo.GetAllCategories();
            this.imageData = imageData;
        }
    }

    public class TempMiscData
    {
        // Display Settings
        public TempDisplaySettings displaySettings;

        // Options
        public bool randomizeSelection;
        public int maxRank;

        // Tagging Settings
        public string tagSortOption;

        public TempMiscData()
        {
            // TODO More constructor stuff that can be re-purposed later, don't worry about this, just get the data loaded off to someplace else
            /*x
            // Display Settings
            displaySettings.WallpaperStyles = WallpaperManagerForm.GetWallpaperStyles();
            displaySettings.WallpaperIntervals = WallpaperManagerForm.GetTimerIndexes();
            displaySettings.Synced = WallpaperManagerForm.DisplaySettingsSynced;

            // Options
            randomizeSelection = RandomizeSelection;
            maxRank = GetMaxRank();

            // Tagging Settings
            tagSortOption = TagSortOption;
            */
        }
    }
    #endregion
    //! Temp

    public class JsonWallpaperData
    {
        [JsonProperty("Settings")] public SettingsModel Settings;

        [JsonProperty("DisplaySettings")] public SimplifiedDisplaySettings DisplaySettings;

        [JsonProperty("FrequencyData")] public SimplifiedFrequencyModel FrequencyModel;

        [JsonProperty("FolderPriorities")] public SimplifiedFolderPriority[] FolderPriorities;

        [JsonProperty("MiscData")] public MiscData MiscData;

        [JsonProperty("ImageFolders")] public SimplifiedFolder[] ImageFolders;

        [JsonProperty("TagData")] public SimplifiedCategory[] Categories;

        [JsonProperty("ImageData")] public SimplifiedImage[] Images;

        [JsonProperty("ImageSetData")] public SimplifiedImageSet[] ImageSets;

        public JsonWallpaperData(SettingsModel settings, SimplifiedDisplaySettings displaySettings, SimplifiedFrequencyModel frequencyModel, SimplifiedFolderPriority[] folderPriorities,
            MiscData miscData, SimplifiedFolder[] imageFolders, SimplifiedCategory[] categories, SimplifiedImage[] images, SimplifiedImageSet[] imageSets)
        {
            Settings = settings;
            DisplaySettings = displaySettings;
            FrequencyModel = frequencyModel;
            FolderPriorities = folderPriorities;
            MiscData = miscData;
            ImageFolders = imageFolders;
            Categories = categories;
            Images = images;
            ImageSets = imageSets;
        }
    }

    public struct MiscData
    {
        public bool RandomizeSelection;

        public bool ReverseSelection;

        public bool RandomizeSelectionViaTags;

        public bool ReverseSelectionViaTags;

        public TagSortType TagSortType;

        public bool SortTagsByNameDirection;

        public bool SortTagsByCountDirection;

        public string DefaultConflictResolutionPath;

        // TODO Display Settings

        public MiscData(bool randomizeSelection, bool reverseSelection, bool randomizeSelectionViaTags, bool reverseSelectionViaTags,
            TagSortType tagSortType, bool sortTagsByNameDirection, bool sortTagsByCountDirection, string defaultConflictResolutionPath)
        {
            RandomizeSelection = randomizeSelection;
            ReverseSelection = reverseSelection;
            RandomizeSelectionViaTags = randomizeSelectionViaTags;
            ReverseSelectionViaTags = reverseSelectionViaTags;
            TagSortType = tagSortType;
            SortTagsByNameDirection = sortTagsByNameDirection;
            SortTagsByCountDirection = sortTagsByCountDirection;
            DefaultConflictResolutionPath = defaultConflictResolutionPath;
        }
    }

    public static class JsonUtil
    {
        public static readonly string JSON_FILE_DISPLAY_NAME = "JSON Files (*.json)";
        public static readonly string JSON_FILE_EXTENSION = "*.json";

        private static string _loadedThemePath;
        public static string LoadedThemePath
        {
            get => _loadedThemePath;
            private set
            {
                _loadedThemePath = value;
                WallpaperFluxViewModel.Instance.RaisePropertyChanged(() => WallpaperFluxViewModel.Instance.IsThemeLoaded);
            }
        }

        public static bool IsLoadingData { get; private set; } // used to speed up the loading process by preventing unnecessary calls

        public static List<Action> ActionsPendingLoad = new List<Action>();

        public static void SetIsLoadingData(bool isLoadingData)
        {
            IsLoadingData = isLoadingData;

            if (isLoadingData)
            {
                ActionsPendingLoad.Add(() =>
                {
                    // --- Update Tag Information ---
                    //! This does not go into ConvertTags as images have to add the tags first, doing this here instead of while loading the images improves performance
                    foreach (CategoryModel category in ThemeUtil.Theme.Categories)
                    {
                        foreach (TagModel tag in category.GetTags())
                        {
                            tag.RaisePropertyChangedImageCount();
                        }
                    }

                    WallpaperFluxViewModel.Instance.ImageFolders.ValidateImageFolders(true); // validation was cancelled beforehand

                    SettingsViewModel.Instance.Settings = ThemeUtil.Theme.Settings;
                });
            }

            if (!isLoadingData) // we are no longer loading data, call all pending actions
            {
                // ? Call methods / actions that were disabled doing the loading process but need to be called once loading is finished
                // ? (Many were disabled for being called too frequently or when information they need is unavailable)
                // TODO Would be better to try and design a way to avoid needing this (avoid running into loading complications, that is) if you can

                foreach (Action action in ActionsPendingLoad) action?.Invoke();
                ActionsPendingLoad.Clear();
            }
        }

        #region Data Saving

        public static void QuickSave()
        {
            if (!FileUtil.Exists(LoadedThemePath)) return;

            SaveData(LoadedThemePath);
        }

        public static void SaveData(string path)
        {
            if (IsLoadingData) return;

            //! Implementing a saving thread may cause issues if changes are made while saving or if you accidentally allow to saves to occur at once, so it'll be best to just keep that option off

            if (string.IsNullOrEmpty(path)) return;

            //? ----- Backup -----
            // Create a temporary backup in the save file's directory for just in case something goes wrong during the saving process
            string backupPath = BackupData(path);

            //? ----- Write to JSON file -----
            //x using a regular await Task.Run process here will cause the program to crash (and save to be incomplete)
            //x if this method is accessed too rapidly this allows this method to only be accessed if the thread is done
            //xSavingThread = new Thread(() =>
            //x{
            //? The thread was removed so that any potential changes made to the program would be impossible while saving
            //? If you can make this safer in the future, go ahead and put it back in but for now it should stay like this
            //! Threading may have similar issues to WinForm in WPF where the application could crash after attempting to access a control, although it'd be far less likely here
            //! Threading this could potentially cause errors with saving to the Default Theme if you're not careful

            Debug.WriteLine("Saving to: " + path);

            //? ----- Conversions -----

            // Frequency
            SimplifiedFrequencyModel frequencyModel = new SimplifiedFrequencyModel(
                ThemeUtil.ThemeSettings.FrequencyModel.RelativeFrequencyStatic,
                ThemeUtil.ThemeSettings.FrequencyModel.RelativeFrequencyGIF,
                ThemeUtil.ThemeSettings.FrequencyModel.RelativeFrequencyVideo,
                ThemeUtil.ThemeSettings.FrequencyModel.WeightedFrequency);

            // --- Misc Data ---
            //! the instance itself will be NULL if you DON'T OPEN it before saving, so don't use ViewModel.Instance here!
            MiscData miscData = new MiscData(false, false, false, false, 
                TaggingUtil.GetActiveSortType(), TaggingUtil.GetSortByNameDirection(), TaggingUtil.GetSortByCountDirection(),
                TaggingUtil.DefaultConflictResolutionPath);

            //? --- Serialization ---
            JsonWallpaperData jsonWallpaperData = new JsonWallpaperData(
                ThemeUtil.Theme.Settings,
                ConvertToSimplifiedDisplaySettings(),
                frequencyModel,
                ConvertToSimplifiedFolderPriorities(),
                miscData,
                ConvertToSimplifiedFolders(WallpaperFluxViewModel.Instance.ImageFolders.ToArray()),
                ConvertToSimplifiedCategories(ThemeUtil.Theme.Categories.ToArray()),
                ConvertToSimplifiedImages(ThemeUtil.Theme.Images.GetAllImages().ToArray()),
                ConvertToSimplifiedImageSets(ThemeUtil.Theme.Images.GetAllImageSets()));

            Debug.WriteLine("Writing to JSON file");
            using (StreamWriter file = File.CreateText(path))
            {
                new JsonSerializer { Formatting = Formatting.Indented }.Serialize(file, jsonWallpaperData);
            }
            Debug.WriteLine("Writing finished");
            //x});
            //xSavingThread.Start();
        }

        private static SimplifiedFolderPriority[] ConvertToSimplifiedFolderPriorities()
        {
            SimplifiedFolderPriority[] folderPriorities = new SimplifiedFolderPriority[TagViewModel.Instance.FolderPriorities.Count];

            for (int i = 0; i < folderPriorities.Length; i++)
            {
                FolderPriorityModel priority = TagViewModel.Instance.FolderPriorities[i];
                folderPriorities[i] = new SimplifiedFolderPriority(priority.Name, priority.ConflictResolutionFolder, priority.PriorityOverride);
            }

            return folderPriorities;
        }

        #region Simplified Data Conversions
        private static SimplifiedDisplaySettings ConvertToSimplifiedDisplaySettings()
        {
            SimplifiedDisplaySetting[] displaySettingArr = new SimplifiedDisplaySetting[WallpaperUtil.DisplayUtil.GetDisplayCount()];
            bool isSynced = false; //? we are synced if any models have a parent synced to them

            for (int i = 0; i < displaySettingArr.Length; i++)
            {
                DisplayModel display = WallpaperFluxViewModel.Instance.DisplaySettings[i];

                displaySettingArr[i].DisplayInterval = display.DisplayInterval;
                displaySettingArr[i].DisplayIntervalType = display.DisplayIntervalType;
                displaySettingArr[i].DisplayStyle = display.DisplayStyle;

                if (!display.NotSyncedToParent) isSynced = true;
            }

            SimplifiedDisplaySettings displaySettings = new SimplifiedDisplaySettings(displaySettingArr, isSynced);
            return displaySettings;
        }

        private static SimplifiedFolder[] ConvertToSimplifiedFolders(FolderModel[] folders)
        {
            List<SimplifiedFolder> simplifiedFolders = new List<SimplifiedFolder>();

            foreach (FolderModel folder in folders)
            {
                simplifiedFolders.Add(new SimplifiedFolder(folder.Path, folder.PriorityName, folder.Enabled));
            }

            return simplifiedFolders.ToArray();
        }

        private static SimplifiedCategory[] ConvertToSimplifiedCategories(CategoryModel[] categories)
        {
            List<SimplifiedCategory> simplifiedCategories = new List<SimplifiedCategory>();

            foreach (CategoryModel category in categories)
            {
                simplifiedCategories.Add(new SimplifiedCategory(
                    category.Name,
                    ConvertToSimplifiedTags(category.GetTags()), category.Enabled, category.UseForNaming));
            }

            return simplifiedCategories.ToArray();
        }

        private static SimplifiedTag[] ConvertToSimplifiedTags(HashSet<TagModel> tags)
        {
            List<SimplifiedTag> simplifiedTags = new List<SimplifiedTag>();

            foreach (TagModel tag in tags)
            {

                simplifiedTags.Add(new SimplifiedTag(
                    tag.Name,
                    tag.ParentCategory.Name,
                    ConvertToParentTagArray(tag.GetParentTags()),
                    tag.RenameFolderPath, tag.Enabled, tag.UseForNaming));
            }

            return simplifiedTags.ToArray();
        }

        private static SimplifiedParentTag[] ConvertToParentTagArray(HashSet<TagModel> parentTags)
        {
            List<SimplifiedParentTag> parentTagList = new List<SimplifiedParentTag>();

            foreach (TagModel parentTag in parentTags)
            {
                parentTagList.Add(new SimplifiedParentTag(parentTag.Name, parentTag.ParentCategory.Name));
            }

            return parentTagList.ToArray();
        }

        private static SimplifiedImage[] ConvertToSimplifiedImages(ImageModel[] images)
        {
            List<SimplifiedImage> simplifiedImages = new List<SimplifiedImage>();

            foreach (ImageModel image in images)
            {
                simplifiedImages.Add(new SimplifiedImage(
                    image.Path,
                    image.Rank,
                    image.Tags.GetConvertTagsToDictionary(),
                    image.Tags.GetConvertTagNamingExceptionsToDictionary(),
                    image.MinimumLoops,
                    image.MaximumTime,
                    image.OverrideMinimumLoops,
                    image.OverrideMaximumTime,
                    image.Enabled, image.Volume));
            }

            return simplifiedImages.ToArray();
        }

        private static SimplifiedImageSet[] ConvertToSimplifiedImageSets(ImageSetModel[] imageSets)
        {
            List<SimplifiedImageSet> simplifiedImageSets = new List<SimplifiedImageSet>();

            foreach (ImageSetModel imageSet in imageSets)
            {
                simplifiedImageSets.Add(new SimplifiedImageSet(
                    imageSet.GetImagePaths(),
                    imageSet.OverrideRank,
                    imageSet.UsingAverageRank,
                    imageSet.UsingWeightedAverage,
                    imageSet.UsingOverrideRank,
                    imageSet.UsingWeightedOverride,
                    imageSet.OverrideRankWeight,
                    imageSet.Enabled,
                    imageSet.Speed,
                    imageSet.SetType,
                    imageSet.MinimumLoops,
                    imageSet.MaximumTime,
                    imageSet.OverrideMinimumLoops,
                    imageSet.OverrideMinimumLoops,
                    imageSet.FractionIntervals,
                    imageSet.StaticIntervals,
                    imageSet.WeightedIntervals,
                    imageSet.RetainImageIndependence));
            }

            return simplifiedImageSets.ToArray();
        }
        #endregion

        private static string BackupData(string path, string backupNameExtension = "_BACKUP")
        {
            // only need to create a backup is the file actually exists
            if (!FileUtil.Exists(path)) return null;

            FileInfo pathFile = new FileInfo(path);
            string tempPathName = pathFile.DirectoryName + "\\WF_Backup\\" + Path.GetFileNameWithoutExtension(pathFile.Name) + backupNameExtension;
            string tempPath = tempPathName + pathFile.Extension;

            // for just in case the user ends up with multiple accidents, we don't want to overwrite a singular backup with a damaged file
            int tempPathCount = 1;
            
            SortedList<DateTime, string> dateSortedFilePaths = new SortedList<DateTime, string>();

            string newTempPath = tempPath;
            while (FileUtil.Exists(newTempPath))
            {
                DateTime dt = new FileInfo(newTempPath).LastWriteTime;
                if (!dateSortedFilePaths.ContainsKey(dt)) // sometimes files can be saved too closely together, the initial file will be the oldest out of the two
                {
                    dateSortedFilePaths.Add(dt, newTempPath);
                }

                newTempPath = tempPathName + "_" + tempPathCount + pathFile.Extension; // updates the temp path name with a number

                if (tempPathCount > 10) //? overwrite the oldest backup upon reaching 10 backups
                {
                    newTempPath = dateSortedFilePaths.Values[0];
                    break;
                }

                tempPathCount++;
            }
            tempPath = newTempPath;

            Debug.WriteLine("Temp File Location: " + tempPath);

            FileInfo tempFileInfo = new FileInfo(tempPath);
            if (!Directory.Exists(tempFileInfo.DirectoryName))
            {
                if (tempFileInfo.DirectoryName != null)
                {
                    Directory.CreateDirectory(tempFileInfo.DirectoryName);
                }
            }

            File.Copy(path, tempPath, true);

            return tempPath;
        }

        #endregion

        // Load Data
        public static JsonWallpaperData LoadData(string path)
        {
            Debug.WriteLine("Loading Data");

            if (FileUtil.Exists(path))
            {
                Debug.WriteLine("Loading JSON Data");
                //? RankData and ActiveImages will both be automatically set when jsonWallpaperData is loaded as the constructors for ImageData is what sets them
                JsonWallpaperData jsonWallpaperData;
                using (StreamReader file = File.OpenText(path))
                {
                    jsonWallpaperData = new JsonSerializer().Deserialize(file, typeof(JsonWallpaperData)) as JsonWallpaperData;
                }

                if (jsonWallpaperData == null)
                {
                    MessageBoxUtil.ShowError("Load failed");
                    return null;
                }

                Debug.WriteLine("Finished Loading");

                LoadedThemePath = path;
                return jsonWallpaperData;
            }
            else
            {
                MessageBoxUtil.ShowError("Attempted to load a file that does not exist: \n" + path);
            }
            
            return null;
        }
        
        #region JSON Conversion

        public static void ConvertTheme(JsonWallpaperData wallpaperData)
        {
            Debug.WriteLine("Converting theme...");

            //! The order of operations done here is vital to reducing load times & ensuring properties end up where they need to be
            //! The order of operations done here is vital to reducing load times & ensuring properties end up where they need to be
            //! The order of operations done here is vital to reducing load times & ensuring properties end up where they need to be
            ConvertThemeOptions(wallpaperData); //! must be done first due to SetMaxRank()
            ConvertMiscData(wallpaperData);
            ConvertTags(wallpaperData);
            ConvertImagesAndFolders(wallpaperData);
            ConvertImageSets(wallpaperData.ImageSets);

            Debug.WriteLine("Conversion Finished");
        }

        private static void ConvertThemeOptions(JsonWallpaperData wallpaperData)
        {
            // --- Load General Settings ---
            ThemeUtil.ReconstructTheme(wallpaperData.Settings);  //! This needs to be done before any images are added otherwise their ranks will be changed!

            // --- Load Frequency Settings ---
            //! cannot be loaded before calling ThemeUtil.ReconstructTheme, otherwise this will just be overwritten
            ThemeUtil.ThemeSettings.FrequencyModel.RelativeFrequencyStatic = wallpaperData.FrequencyModel.RelativeFrequencyStatic;
            ThemeUtil.ThemeSettings.FrequencyModel.RelativeFrequencyGIF = wallpaperData.FrequencyModel.RelativeFrequencyGif;
            ThemeUtil.ThemeSettings.FrequencyModel.RelativeFrequencyVideo = wallpaperData.FrequencyModel.RelativeFrequencyVideo;
            ThemeUtil.ThemeSettings.FrequencyModel.WeightedFrequency = wallpaperData.FrequencyModel.WeightedFrequency;

            // --- Load Display Settings ---
            for (int i = 0; i < WallpaperUtil.DisplayUtil.GetDisplayCount(); i++) //? keep in mind that we need to account for dynamic changes to monitor count
            {
                SimplifiedDisplaySetting simplifiedDisplaySetting = wallpaperData.DisplaySettings.DisplaySettings[i];
                DisplayModel displaySetting = WallpaperFluxViewModel.Instance.DisplaySettings[i];

                displaySetting.DisplayInterval = simplifiedDisplaySetting.DisplayInterval;
                displaySetting.DisplayIntervalType = simplifiedDisplaySetting.DisplayIntervalType;
                displaySetting.DisplayStyle = simplifiedDisplaySetting.DisplayStyle;
            }

            if (wallpaperData.DisplaySettings.IsSynced)
            {
                WallpaperFluxViewModel.Instance.SyncDisplaySettings(WallpaperFluxViewModel.Instance.DisplaySettings[0]); // if synced, all will be the same, so just pick 0
            }
        }

        private static void ConvertMiscData(JsonWallpaperData wallpaperData)
        {
            TaggingUtil.SetActiveSortType(wallpaperData.MiscData.TagSortType);

            switch (wallpaperData.MiscData.TagSortType)
            {
                case TagSortType.Name:
                    TaggingUtil.SetSortByNameDirection(wallpaperData.MiscData.SortTagsByNameDirection);
                    TaggingUtil.SetSortByCountDirection(false);
                    break;

                case TagSortType.Count:
                    TaggingUtil.SetSortByNameDirection(false);
                    TaggingUtil.SetSortByCountDirection(wallpaperData.MiscData.SortTagsByCountDirection);
                    break;
            }

            TaggingUtil.DefaultConflictResolutionPath = wallpaperData.MiscData.DefaultConflictResolutionPath;
        }

        private static void ConvertTags(JsonWallpaperData wallpaperData)
        {
            Debug.WriteLine("Converting Tags...");
            //? ----- Add Categories -----
            List<CategoryModel> orderedCategories = new List<CategoryModel>();
            foreach (SimplifiedCategory simpleCategory in wallpaperData.Categories)
            {
                // verify category before adding tags
                // TODO Consider converting the data held up by the TagViewModel Instance into another subset of ThemeModel
                CategoryModel instanceCategory = TaggingUtil.VerifyCategoryWithData(simpleCategory.Name, simpleCategory.UseForNaming, simpleCategory.Enabled, true);

                //? ----- Add Tags (of this Category) -----
                foreach (SimplifiedTag simpleTag in simpleCategory.Tags)
                {
                    // verify tag before adding
                    TagModel instanceTag = instanceCategory.VerifyTagWithData(simpleTag.Name, simpleTag.UseForNaming, simpleTag.Enabled, simpleTag.RenameFolderPath, true);
                    instanceTag.ParentCategory = instanceCategory; // not saved into the tag's JSON as it would be unnecessary bloat since the category has this

                    // ----- Add Parent Tags (of this Tag) -----
                    foreach (SimplifiedParentTag parentTag in simpleTag.ParentTags)
                    {
                        //! Keep in mind that this tackles both Parent and Child tags, no need to loop through the Child Tag list as well
                        //! (There's no way to directly add child tags anyways, it will always occur when a parent tag is linked)
                        instanceTag.LinkTag(TaggingUtil.VerifyCategory(parentTag.ParentCategoryName).VerifyTag(parentTag.Name), false);
                    }

                    instanceCategory.AddTag(instanceTag);
                }

                //? handled via verification
                //x TaggingUtil.AddCategory(instanceCategory); // TODO May want to convert this to AddRange in the actual conversion

                orderedCategories.Add(instanceCategory);
            }

            //? we need the official category list ahead of time for data handling but this gives them the wrong order, so we used this to fix it
            ThemeUtil.Theme.Categories = new List<CategoryModel>(orderedCategories);
            TaggingUtil.UpdateCategoryView(); // don't forget to update the view
            
            TagViewModel.Instance.InitFolderPriorities(wallpaperData.FolderPriorities);
        }

        private static void ConvertImagesAndFolders(JsonWallpaperData wallpaperData)
        {
            Debug.WriteLine("Converting Images...");
            //! Placing this after AddFolderRange() will *significantly* increase load times as the images will attempt to be added multiple times
            // TODO Even with the above statement, this still takes a considerable amount of time to load
            // TODO Some of the lag may have to do with the conversions, it'll likely be a bit better once TempImageData is no longer needed

            int invalidImageCount = 0;
            string invalidImageString = "The following image(s) no longer exist and have been removed from the theme: ";

            //? ----- Converting Images -----
            for (int i = 0; i < wallpaperData.Images.Length; i++)
            {
                SimplifiedImage simpleImage = wallpaperData.Images[i];

                if (!FileUtil.Exists(simpleImage.Path))
                {
                    invalidImageCount++;
                    invalidImageString += "\n" + simpleImage.Path;
                    continue;
                }

                ImageModel image = new ImageModel(simpleImage.Path, simpleImage.Rank, simpleImage.Enabled, volume: simpleImage.Volume, 
                    minimumLoops: simpleImage.MinLoops, overrideMinimumLoops: simpleImage.OverrideMinLoops, maximumTime: simpleImage.MaxTime, overrideMaximumTime: simpleImage.OverrideMaxTime);
                ImageTagCollection tags = new ImageTagCollection(image);
                if (image.ImageType == ImageType.None) continue; // ? previously valid image is now invalid, move on

                //? We need two iterations of this, one for regular tags & one for naming exceptions
                ConvertSimpleImageTagsToTagCollection(simpleImage, tags, false);
                ConvertSimpleImageTagsToTagCollection(simpleImage, tags, true);

                ThemeUtil.Theme.Images.AddImage(image, null);
            }

            //? ----- Converting Folders -----
            //! FOLDERS NEED TO BE ADDED AFTER!!!! images are added so that they don't have to be verified twice
            //! (The folders will add all images as if they were new if added first, however, we still need this functionality to find *actually* new images)
            Debug.WriteLine("Adding folders...");
            WallpaperFluxViewModel.Instance.AddFolderRange(wallpaperData.ImageFolders);
            Debug.WriteLine("Folders Created");

            //? placing this at the end of the loading process just because it's a simple warning message which would be annoying to have interrupt the loading process
            if (invalidImageCount > 0) MessageBoxUtil.ShowError(invalidImageString);
        }

        private static void ConvertSimpleImageTagsToTagCollection(SimplifiedImage simpleImage, ImageTagCollection tagCollection, bool namingExceptions)
        {
            Dictionary<string, List<string>> tags = namingExceptions ? simpleImage.TagNamingExceptions : simpleImage.Tags;

            //? ----- Converting Image's Tags -----
            foreach (string categoryName in tags.Keys)
            {
                //? We will not be verifying anything here, that was done in the Convert Tag section. If it doesn't exist, it will not be added (Instead of crashing)
                CategoryModel category = TaggingUtil.GetCategory(categoryName);

                if (category == null)
                {
                    Debug.WriteLine("Category [" + categoryName + "] was referenced while converting images but was not found while converting tags, dropping");
                    continue;
                }

                foreach (string tagName in tags[categoryName])
                {
                    TagModel tag = category.GetTag(tagName);

                    if (tag == null)
                    {
                        Debug.WriteLine("Tag [" + tagName + "] was referenced while converting images but was not found while converting tags, dropping");
                        continue;
                    }

                    if (namingExceptions)
                    {
                        tagCollection.AddNamingException(tag);
                    }
                    else
                    {
                        tagCollection.Add(tag, false);
                    }
                }
            }
        }

        private static void ConvertImageSets(SimplifiedImageSet[] imageSets)
        {
            if (imageSets != null)
            {
                foreach (SimplifiedImageSet imageSet in imageSets)
                {
                    ImageModel[] images = ThemeUtil.Theme.Images.GetImageRange(imageSet.ImagePaths);
                    ImageSetModel imageSetModel = new ImageSetModel.Builder(images).Build();

                    if (imageSetModel != null)
                    {
                        imageSetModel.OverrideRank = imageSet.OverrideRank;
                        imageSetModel.UsingAverageRank = imageSet.UsingAverageRank;
                        imageSetModel.UsingWeightedAverage = imageSet.UsingWeightedAverage;
                        imageSetModel.UsingOverrideRank = imageSet.UsingOverrideRank;
                        imageSetModel.UsingWeightedOverride = imageSet.UsingWeightedRank;
                        imageSetModel.OverrideRankWeight = imageSet.OverrideRankWeight;
                        imageSetModel.Enabled = imageSet.Enabled;
                        imageSetModel.Speed = imageSet.Speed;
                        imageSetModel.SetType = imageSet.SetType;
                        imageSetModel.MinimumLoops = imageSet.MinLoops;
                        imageSetModel.MaximumTime = imageSet.MaxTime;
                        imageSetModel.OverrideMinimumLoops = imageSet.OverrideMinLoops;
                        imageSetModel.OverrideMaximumTime = imageSet.OverrideMaxTime;
                        imageSetModel.FractionIntervals = imageSet.FractionIntervals;
                        imageSetModel.StaticIntervals = imageSet.StaticIntervals;
                        imageSetModel.WeightedIntervals = imageSet.WeightedIntervals;
                        imageSetModel.RetainImageIndependence = imageSet.RetainImageIndependence;
                    }
                }
            }
        }
        #endregion
    }
}
