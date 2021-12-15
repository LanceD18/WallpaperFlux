using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using WallpaperFlux.Core.JSON.Temp;

namespace WallpaperFlux.Core.Util
{
    #region Temporary
    public class TemporaryJsonWallpaperData
    {
        [JsonProperty("ThemeOptions")] public TempThemeOptions themeOptions;

        [JsonProperty("MiscData")] public TempMiscData miscData;

        [JsonProperty("ImageFolders")] public Dictionary<string, bool> imageFolders;

        [JsonProperty("TagData")] public TempCategoryData[] tagData;

        //! ImageData MUST remain at the bottom at all times to ensure that the needed info above is loaded first
        //! (This allows you to initialize more data in the constructor like the EvaluateActiveState() method)
        [JsonProperty("ImageData")] public TempImageData[] imageData;

        public TemporaryJsonWallpaperData(TempMiscData miscData, TempThemeOptions themeOptions, TempCategoryData[] tagData, TempImageData[] imageData, Dictionary<string, bool> imageFolders)
        {
            //x //! This handles SAVING!!! | Don't go to this code segment for modifying how data is loaded!
            //? While yes this does handle saving initialization is still done when loading, without this, imageData and imageFolders would not work on the initial load

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

    //! Temp information here for use until you import the data over to your new system
    public static class JsonUtil
    {
        public static readonly string JSON_FILE_DISPLAY_NAME = "JSON Files (*.json)";
        public static readonly string JSON_FILE_EXTENSION = "*.json";

        public static bool IsLoadingData { get; private set; }

        /*TODO
        // Save Data
        public static void SaveData(string path)
        {
            //xif (SavingThread != null && SavingThread.IsAlive) return;

            if (path != null)
            {
                //-----Backup-----
                // Create a temporary backup in the save file's directory for just in case something goes wrong during the saving process
                FileInfo pathFile = new FileInfo(path);
                string tempPathName = pathFile.DirectoryName + "\\" + Path.GetFileNameWithoutExtension(pathFile.Name) + "_TEMP_BACKUP";
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

                //-----Write to JSON File-----
                //x using a regular Task.Run process here will cause the program to crash (and save to be incomplete)
                //x if this method is accessed too rapidly this allows this method to only be accessed if the thread is done
                //xSavingThread = new Thread(() =>
                //x{
                //? The thread was removed so that any potential changes made to the program would be impossible while saving
                //? If you can make this safer in the future, go ahead and put it back in but for now it should stay like this
                //! Also NOTE: Some of the data accessed require using controls, which will cause an error involving accessing a control from the wrong stream
                //! Another NOTE: I believe making this a thread may have caused the Default Theme save errors

                Debug.WriteLine("Saving to: " + path);

                JsonWallpaperData jsonWallpaperData = new JsonWallpaperData(FileData.Values.ToArray(), ImageFolders);

                Debug.WriteLine("Writing to JSON file");
                using (StreamWriter file = File.CreateText(path))
                {
                    new JsonSerializer { Formatting = Formatting.Indented }.Serialize(file, jsonWallpaperData);
                }
                //x});
                //xSavingThread.Start();

                //-----Remove the backup-----
                File.Delete(tempPath);
            }
            else
            {
                Debug.WriteLine("Attempted to save to a null path");
            }
        }
        */

        // Load Data
        // TODO Should probably focus on just getting the data ported over to the new format so that you don't have to deal with the long conversion load times
        // TODO Should probably focus on just getting the data ported over to the new format so that you don't have to deal with the long conversion load times
        // TODO Should probably focus on just getting the data ported over to the new format so that you don't have to deal with the long conversion load times
        // TODO Should probably focus on just getting the data ported over to the new format so that you don't have to deal with the long conversion load times
        // TODO Should probably focus on just getting the data ported over to the new format so that you don't have to deal with the long conversion load times
        // TODO Should probably focus on just getting the data ported over to the new format so that you don't have to deal with the long conversion load times
        // TODO Should probably focus on just getting the data ported over to the new format so that you don't have to deal with the long conversion load times
        // TODO Should probably focus on just getting the data ported over to the new format so that you don't have to deal with the long conversion load times
        public static TemporaryJsonWallpaperData LoadData(string path)
        {
            Debug.WriteLine("Load Data");

            if (File.Exists(path))
            {
                IsLoadingData = true; // used to speed up the loading process by preventing unnecessary calls
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

                IsLoadingData = false;
                /* TODO
                WallpaperPathSetter.ActiveWallpaperTheme = path;
                UpdateRankPercentiles(ImageType.None); //! Now that image types exist this preemptive change may not be worth it
                */

                Debug.WriteLine("Finished Loading");
                return jsonWallpaperData;
            }

            //! MessageBox warnings for non-existent files should not be used in this method but rather the ones that call it
            Debug.WriteLine("Attempted to load a non-existent file");
            return null;
        }

        /*TODO
        private static void ResetCoreData()
        {
            int oldRankMax = RankData.Count - 1;

            FileData.Clear(); // AddImage handles most of FileData
            RankData.Clear(); //? Loaded in when jsonWallpaperData is created
            ActiveImages.Clear(); //? Loaded in when jsonWallpaperData is created
            ImageFolders.Clear();
            TaggingInfo = new TaggingInfo();

            ImagesOfType.Clear();
            ImagesOfTypeRankData.Clear();
            ActiveImagesOfType.Clear();

            WallpaperPathSetter.Reset();

            InitializeImagesOfType();

            // This is needed if loading otherwise images with invalid ranks will crash the program
            SetRankData(LargestMaxRank);
        }

        private static void ResetWallpaperManager()
        {
            WallpaperManagerForm.ResetWallpaperManager();
        }

        private static void LoadCoreData(JsonWallpaperData jsonWallpaperData)
        {
            SetMaxRank(jsonWallpaperData.miscData.maxRank);

            //? Must be set before the foreach loop where AddImage is called so that the available tags and categories can exist
            TaggingInfo = new TaggingInfo(jsonWallpaperData.tagData.ToList());

            foreach (CategoryData category in TaggingInfo.GetAllCategories())
            {
                category.Initialize(false);
            }

            // All tags will be linked through the AddImage method
            string invalidImagesString = "A few image files for your theme appear to be missing.\nThe following image's will not be saved to your theme: \n";
            foreach (ImageData image in jsonWallpaperData.imageData)
            {
                if (AddImage(image) == null)
                {
                    invalidImagesString += "\n" + image.Path;
                }
            }

            if (invalidImagesString.Contains("\n\n"))
            {
                MessageBox.Show(invalidImagesString);
            }

            // since activating an image folder also adds undeteced images, this needs to be loaded last
            IsLoadingImageFolders = true; // this is used to override the IsLoading bool for new images added when loading image folders
            WallpaperManagerForm.LoadImageFolders(jsonWallpaperData.imageFolders);
            IsLoadingImageFolders = false;
        }

        private static void LoadOptionsData(JsonWallpaperData jsonWallpaperData)
        {
            OptionsData.ThemeOptions = jsonWallpaperData.themeOptions;

            // this is only really needed when adding in new options, a minor convenience feature to prevent errors when loading the default theme
            OptionsData.InitializePotentialNulls();
        }

        private static void LoadMiscData(JsonWallpaperData jsonWallpaperData)
        {
            WallpaperManagerForm.UpdateWallpaperStyle(jsonWallpaperData.miscData.displaySettings.WallpaperStyles);
            WallpaperManagerForm.SetTimerIndex(jsonWallpaperData.miscData.displaySettings.WallpaperIntervals, true);
            RandomizeSelection = jsonWallpaperData.miscData.randomizeSelection;
            TagSortOption = jsonWallpaperData.miscData.tagSortOption;
        }

        public static void LoadDefaultTheme()
        {
            if (LoadData(Properties.Settings.Default["DefaultTheme"] as string))
            {
                WallpaperManagerForm.NextWallpaper();
            }
        }
        */
    }

}
