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

    public class JsonWallpaperData
    {
        [JsonProperty("Settings")] public SettingsModel Settings;

        [JsonProperty("MiscData")] public MiscData MiscData;

        [JsonProperty("ImageFolders")] public SimplifiedFolder[] ImageFolders;

        [JsonProperty("TagData")] public SimplifiedCategory[] Categories;

        [JsonProperty("ImageData")] public SimplifiedImage[] Images;

        public JsonWallpaperData(SettingsModel settings, MiscData miscData, SimplifiedFolder[] imageFolders, SimplifiedCategory[] categories, SimplifiedImage[] images)
        {
            Settings = settings;
            MiscData = miscData;
            ImageFolders = imageFolders;
            Categories = categories;
            Images = images;
        }
    }

    /*x
    public struct Settings
    {
        public int MaxRank;

        public Settings(int maxRank)
        {
            MaxRank = maxRank;
        }
    }
    */
    
    public struct MiscData
    {
        public bool RandomizeSelection;

        public bool ReverseSelection;

        public bool RandomizeSelectionViaTags;

        public bool ReverseSelectionViaTags;

        public TagSortType TagSortType;

        public bool SortTagsByNameDirection;

        public bool SortTagsByCountDirection;

        // TODO Display Settings

        public MiscData(bool randomizeSelection, bool reverseSelection, bool randomizeSelectionViaTags, bool reverseSelectionViaTags,
            TagSortType tagSortType, bool sortTagsByNameDirection, bool sortTagsByCountDirection)
        {
            RandomizeSelection = randomizeSelection;
            ReverseSelection = reverseSelection;
            RandomizeSelectionViaTags = randomizeSelectionViaTags;
            ReverseSelectionViaTags = reverseSelectionViaTags;
            TagSortType = tagSortType;
            SortTagsByNameDirection = sortTagsByNameDirection;
            SortTagsByCountDirection = sortTagsByCountDirection;
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

        public static void SetIsLoadingData(bool isLoadingData) => IsLoadingData = isLoadingData;

        #region Data Saving

        public static void QuickSave()
        {
            if (!File.Exists(LoadedThemePath)) return;

            SaveData(LoadedThemePath);
        }

        public static void SaveData(string path)
        {
            //! Implementing a saving thread may cause issues if changes are made while saving or if you accidentally allow to saves to occur at once, so it'll be best to just keep that option off

            if (string.IsNullOrEmpty(path)) return;

            //? ----- Backup -----
            // Create a temporary backup in the save file's directory for just in case something goes wrong during the saving process
            string backupPath = BackupData(path);

            //? ----- Write to JSON file -----
            //x using a regular Task.Run process here will cause the program to crash (and save to be incomplete)
            //x if this method is accessed too rapidly this allows this method to only be accessed if the thread is done
            //xSavingThread = new Thread(() =>
            //x{
            //? The thread was removed so that any potential changes made to the program would be impossible while saving
            //? If you can make this safer in the future, go ahead and put it back in but for now it should stay like this
            //! Threading may have similar issues to WinForm in WPF where the application could crash after attempting to access a control, although it'd be far less likely here
            //! Threading this could potentially cause errors with saving to the Default Theme if you're not careful

            Debug.WriteLine("Saving to: " + path);
            
            //! the instance itself will be NULL if you DON'T OPEN it before saving, so don't use ViewModel.Instance here!
            MiscData miscData = new MiscData(false, false, false, false, 
                TaggingUtil.GetActiveSortType(), TaggingUtil.GetSortByNameDirection(), TaggingUtil.GetSortByCountDirection());

            JsonWallpaperData jsonWallpaperData = new JsonWallpaperData(
                ThemeUtil.Theme.Settings,
                miscData,
                ConvertToSimplifiedFolders(WallpaperFluxViewModel.Instance.ImageFolders.ToArray()),
                ConvertToSimplifiedCategories(ThemeUtil.Theme.Categories.ToArray()),
                ConvertToSimplifiedImages(ThemeUtil.Theme.Images.GetAllImages().ToArray()));

            Debug.WriteLine("Writing to JSON file");
            using (StreamWriter file = File.CreateText(path))
            {
                new JsonSerializer { Formatting = Formatting.Indented }.Serialize(file, jsonWallpaperData);
            }
            Debug.WriteLine("Writing finished");
            //x});
            //xSavingThread.Start();

            //? ----- Remove Backup [If the user has this option enabled] -----
            // TODO Create an option for this later
            /*x
            if (backupPath != null)
            {
                //! How does this differ from FileSystem.DeleteFile??? (I'm assuming this doesn't go to the recycling bin)
                File.Delete(backupPath);
            }
            */

        }

        #region Simplified Data Conversions
        private static SimplifiedFolder[] ConvertToSimplifiedFolders(FolderModel[] folders)
        {
            List<SimplifiedFolder> simplifiedFolders = new List<SimplifiedFolder>();

            foreach (FolderModel folder in folders)
            {
                simplifiedFolders.Add(new SimplifiedFolder(folder.Path, folder.Active));
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
                    category.Enabled,
                    category.UseForNaming,
                    ConvertToSimplifiedTags(category.GetTags())));
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
                    tag.Enabled,
                    tag.UseForNaming,
                    tag.ParentCategory.Name,
                    ConvertToParentTagArray(tag.GetParentTags())
                ));
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
                    image.Volume,
                    image.MinimumLoops,
                    image.MaximumTime,
                    image.OverrideMinimumLoops,
                    image.OverrideMaximumTime));
            }

            return simplifiedImages.ToArray();
        }
        #endregion

        private static string BackupData(string path, string backupNameExtension = "_BACKUP")
        {
            // only need to create a backup is the file actually exists
            if (!File.Exists(path)) return null;

            FileInfo pathFile = new FileInfo(path);
            string tempPathName = pathFile.DirectoryName + "\\" + Path.GetFileNameWithoutExtension(pathFile.Name) + backupNameExtension;
            string tempPath = tempPathName + pathFile.Extension;

            // for just in case the user ends up with multiple accidents, we don't want to overwrite any backups with the damaged file
            if (File.Exists(tempPath))
            {
                int tempPathCount = 1;
                string newTempPath = tempPath;
                while (File.Exists(newTempPath))
                {
                    newTempPath = tempPathName + "_" + tempPathCount + pathFile.Extension; // updates the temp path name with a number
                    tempPathCount++;
                }

                tempPath = newTempPath;
            }

            Debug.WriteLine("Temp File Location: " + tempPath);
            File.Copy(path, tempPath);

            return tempPath;
        }
        #endregion

        // Load Data
        public static JsonWallpaperData LoadData(string path)
        {
            Debug.WriteLine("Loading Data");

            if (File.Exists(path))
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
                MessageBoxUtil.ShowError("Attempted to load a non-existent file");
            }
            
            return null;
        }

        public static TemporaryJsonWallpaperData LoadOldData(string path)
        {
            Debug.WriteLine("Loading Old Data");

            if (File.Exists(path))
            {
                /* TODO
                jpxToJpgWarning = "";

                Debug.WriteLine("Resetting Wallpaper Manager");

                ResetWallpaperManager();

                Debug.WriteLine("Resetting Core Data");
                //! This must be called before loading JsonWallpaperData to avoid issues
                ResetCoreData();
                */

                Debug.WriteLine("Loading JSON Data");
                //? RankData and ActiveImages will both be automatically set when jsonWallpaperData is loaded as the constructors for ImageData is what sets them
                TemporaryJsonWallpaperData jsonWallpaperData;
                using (StreamReader file = File.OpenText(path))
                {
                    jsonWallpaperData = new JsonSerializer().Deserialize(file, typeof(TemporaryJsonWallpaperData)) as TemporaryJsonWallpaperData;
                }

                if (jsonWallpaperData == null)
                {
                    MessageBoxUtil.ShowError("Load failed");
                    return null;
                }

                Debug.WriteLine("Inserting JSON Data");
                /* TODO
                LoadCoreData(jsonWallpaperData);
                LoadOptionsData(jsonWallpaperData);
                LoadMiscData(jsonWallpaperData);

                if (jpxToJpgWarning != "")
                {
                    MessageBox.Show(jpxStringPrompt + jpxToJpgWarning);
                }
                */
                
                /* TODO
                WallpaperPathSetter.ActiveWallpaperTheme = path;
                UpdateRankPercentiles(ImageType.None); //! Now that image types exist this preemptive change may not be worth it
                */

                Debug.WriteLine("Finished Loading");
                return jsonWallpaperData;
            }

            //! MessageBox warnings for non-existent files should not be used in this method but rather the ones that call it***************************************************************************************-------------
            Debug.WriteLine("Attempted to load a non-existent file");
            return null;
        }

        #region JSON Conversion

        public static void ConvertTheme(JsonWallpaperData wallpaperData)
        {
            Debug.WriteLine("Converting theme...");

            ConvertThemeOptions(wallpaperData); //! must be done first due to SetMaxRank()
            ConvertMiscData(wallpaperData);
            ConvertTags(wallpaperData);
            ConvertImagesAndFolders(wallpaperData);

            TaggingUtil.HighlightTags();


            Debug.WriteLine("Conversion Finished");
        }

        private static void ConvertThemeOptions(JsonWallpaperData wallpaperData)
        {
            ThemeUtil.ReconstructTheme(wallpaperData.Settings);  //! This needs to be done before any images are added otherwise their ranks will be changed!
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
                    TagModel instanceTag = instanceCategory.VerifyTagWithData(simpleTag.Name, simpleTag.UseForNaming, simpleTag.Enabled, true);
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
        }

        private static void ConvertImagesAndFolders(JsonWallpaperData wallpaperData)
        {
            Debug.WriteLine("Converting Images...");
            //! Placing this after AddFolderRange() will *significantly* increase load times as the images will attempt to be added multiple times
            // TODO Even with the above statement, this still takes a considerable amount of time to load
            // TODO Some of the lag may have to do with the conversions, it'll likely be a bit better once TempImageData is no longer needed

            int invalidImageCount = 0;
            string invalidImageString = "The following image(s) no longer exist and have been removed from the theme: ";

            int imagesLoaded = 0;
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

                ImageModel image = new ImageModel(simpleImage.Path, simpleImage.Rank, volume: simpleImage.Volume, 
                    minimumLoops: simpleImage.MinLoops, overrideMinimumLoops: simpleImage.OverrideMinLoops, maximumTime: simpleImage.MaxTime, overrideMaximumTime: simpleImage.OverrideMaxTime);
                ImageTagCollection tags = new ImageTagCollection(image);

                //? We need two iterations of this, one for regular tags & one for naming exceptions
                ConvertSimpleImageTagsToTagCollection(simpleImage, tags, false);
                ConvertSimpleImageTagsToTagCollection(simpleImage, tags, true);

                ThemeUtil.Theme.Images.AddImage(image);
                
                /*x
                //! Debug ; used to test image load times
                imagesLoaded++;

                if (imagesLoaded % 1000 == 0)
                {
                    Debug.WriteLine("Loaded Images: " + imagesLoaded);
                }
                //! Debug ; used to test image load times
                */
            }

            //? ----- Converting Folders -----
            //! FOLDERS NEED TO BE ADDED AFTER!!!! images are added so that they don't have to be verified twice
            //! (The folders will add all images as their default if added first, if added second they'll just find that the image already exists)
            Debug.WriteLine("Adding folders...");
            WallpaperFluxViewModel.Instance.AddFolderRange(wallpaperData.ImageFolders);

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

        public static void ConvertOldTheme(TemporaryJsonWallpaperData wallpaperData)
        {
            //! Do NOT turn the regions here into methods, they are only called once and should only be called right after each other!!!
            //! Do NOT turn the regions here into methods, they are only called once and should only be called right after each other!!!
            //! Do NOT turn the regions here into methods, they are only called once and should only be called right after each other!!!
            //! Do NOT turn the regions here into methods, they are only called once and should only be called right after each other!!!
            //? A local method might be okay but still doesn't enforce singular 'calls' (Unless you want to introduce additional boolean variables)

            #region Convert Theme Options
            Debug.WriteLine("Converting Theme Options...");
            ThemeUtil.ReconstructTheme(wallpaperData.miscData.maxRank); //! This needs to be done before any images are added
            // TODO wallpaperData.themeOptions;
            #endregion

            #region Convert Misc
            Debug.WriteLine("Converting Misc...");
            // TODO wallpaperData.miscData;
            #endregion

            #region Convert Tags
            Debug.WriteLine("Converting Tags...");
            // ----- Add Categories -----
            List<CategoryModel> orderedCategories = new List<CategoryModel>(); // retains the saved order
            foreach (TempCategoryData tempCat in wallpaperData.tagData)
            {
                // verify category before adding tags
                // TODO Consider converting the data held up by the TagViewModel Instance into another subset of ThemeModel
                CategoryModel instanceCategory = TaggingUtil.VerifyCategoryWithData(tempCat.Name, tempCat.UseForNaming, tempCat.Enabled, true);

                // ----- Add Tags (of this Category) -----
                foreach (TempTagData tempTag in tempCat.Tags)
                {
                    // verify tag before adding
                    TagModel instanceTag = instanceCategory.VerifyTagWithData(tempTag.Name, tempTag.UseForNaming, tempTag.Enabled, true);
                    instanceTag.ParentCategory = instanceCategory; // not saved into the tag's JSON as it would be unnecessary bloat since the category has this

                    // ----- Add Parent Tags (of this Tag) -----
                    foreach (Tuple<string, string> parentInfo in tempTag.ParentTags)
                    {
                        //? This Tuple should be replace with SimplifiedTag in the official conversion
                        string parentCategoryName = parentInfo.Item1;
                        string parentName = parentInfo.Item2;

                        //! Keep in mind that this tackles both Parent and Child tags, no need to loop through the Child Tag list as well
                        //! (There's no way to directly add child tags anyways)
                        instanceTag.LinkTag(TaggingUtil.VerifyCategory(parentCategoryName).VerifyTag(parentName), false);
                    }

                    //x Debug.WriteLine("Adding Tag: " + instanceTag.Name);
                    instanceCategory.AddTag(instanceTag); // TODO May want to convert this to AddRange in the actual conversion
                }

                //x Debug.WriteLine("Adding Category: " + instanceCategory.Name + "\n\n\n");
                //? handled via verification
                //x TaggingUtil.AddCategory(instanceCategory); // TODO May want to convert this to AddRange in the actual conversion

                orderedCategories.Add(instanceCategory);
            }

            //? we need the official category list ahead of time for data handling but this gives them the wrong order, so we used this to fix it
            ThemeUtil.Theme.Categories = new List<CategoryModel>(orderedCategories);
            TaggingUtil.UpdateCategoryView(); // don't forget to update the view
            #endregion

            #region Convert Images & Folders
            Debug.WriteLine("Converting Images...");
            //! Placing this after AddFolderRange() will *significantly* increase load times as the images will attempt to be added multiple times
            // TODO Even with the above statement, this still takes a considerable amount of time to load
            // TODO Some of the lag may have to do with the conversions, it'll likely be a bit better once TempImageData is no longer needed

            int invalidImageCount = 0;
            string invalidImageString = "The following image(s) no longer exist and have been removed from the theme: ";

            foreach (TempImageData imageData in wallpaperData.imageData)
            {
                if (!File.Exists(imageData.Path))
                {
                    invalidImageCount++;
                    invalidImageString += "\n" + imageData.Path;
                    continue;
                }

                ImageModel image = new ImageModel(imageData.Path, imageData.Rank, volume: imageData.VideoSettings.Volume);
                ImageTagCollection tags = new ImageTagCollection(image);

                //? ----- Converting Image's Tags -----
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

                        //! Specific to this, purely for conversion from the old theme, we need to nuke all references to parent tags in images to move over to the new paradigm
                        //! Will check if a parent tag of this tag was added to this collection, if so, remove it
                        foreach (TagModel parentTag in tag.GetParentTags())
                        {
                            if (tags.Contains(parentTag))
                            {
                                tags.Remove(parentTag, false);
                            }
                        }

                        tags.Add(tag, false);

                        //! Specific to this, purely for conversion from the old theme, we need to nuke all references to parent tags in images to move over to the new paradigm
                        //! Will check if this tag is the parent of another tag in this collection, if so, remove it
                        foreach (TagModel childTag in tag.GetChildTags())
                        {
                            if (tags.Contains(childTag))
                            {
                                tags.Remove(tag, false);
                                break;
                            }
                        }
                    }
                }

                //? ----- Converting Naming Exceptions -----
                foreach (Tuple<string, string> tagInfo in imageData.TagNamingExceptions)
                {
                    string categoryName = tagInfo.Item1;
                    string tagName = tagInfo.Item2;

                    //? We will not be verifying anything here, that was done in the Convert Tag section. If it doesn't exist, it will not be added (Instead of crashing)
                    CategoryModel category = TaggingUtil.GetCategory(categoryName);

                    if (category == null)
                    {
                        Debug.WriteLine("(Naming Exception) Category [" + categoryName + "] was referenced while converting images but was not found while converting tags, dropping");
                        continue;
                    }

                    TagModel tag = category.GetTag(tagName);

                    if (tag == null)
                    {
                        Debug.WriteLine("(Naming Exception) Tag [" + tagName + "] was referenced while converting images but was not found while converting tags, dropping");
                        continue;
                    }

                    if (!tag.UseForNaming)
                    {
                        tags.AddNamingException(tag);
                    }
                    else
                    {
                        //? parent tags with naming exceptions can now just be added directly since they are no longer automatically added to the image when adding a child, this forces them in on conversion
                        tags.Add(tag, false);
                    }
                }

                //x Debug.WriteLine("Adding Image: " + image.PathName);
                ThemeUtil.Theme.Images.AddImage(image);
            }

            if (invalidImageCount > 0) MessageBoxUtil.ShowError(invalidImageString);

            Debug.WriteLine("Adding folders...");
            //! Folders need to be added after images are added so that they don't have to be verified twice
            //! (The folders will add all images as their default if added first, if added second they'll just find that the image already exists)
            WallpaperFluxViewModel.Instance.AddFolderRange(wallpaperData.imageFolders.Keys.ToArray());
            #endregion

            Debug.WriteLine("Conversion Finished");
        }
        #endregion
    }
}
