using System;
using System.Collections.Generic;
using System.Diagnostics;
using LanceTools;
using WallpaperFlux.Core.Collections;
using WallpaperFlux.Core.Controllers;
using WallpaperFlux.Core.JSON;
using WallpaperFlux.Core.Models.Tagging;

namespace WallpaperFlux.Core.Models.Theme
{
    public class ThemeModel
    {
        public SettingsModel Settings { get; set; } //? getter and setter needed for the XAML file

        public ImageCollection Images;

        public List<CategoryModel> Categories = new List<CategoryModel>();

        public RankController RankController;

        // TODO Will need to be implemented for saving
        public FolderCollection Folders_TODO;

        public WallpaperRandomizationController WallpaperRandomizer;

        public SimplifiedFolderPriority[] PreLoadedFolderPriorities; // for use when the TagViewModel is activated;

        public void Init(int maxRank)
        {
            Settings = new SettingsModel(maxRank);
            Images = new ImageCollection();
            RankController = new RankController();
            WallpaperRandomizer = new WallpaperRandomizationController();

            RankController.SetMaxRank(maxRank); //! don't call this in the constructor of RankController to prevent potential initialization mishaps

            //? the following code cannot be referenced until various components are initialized

            // Without this the UI won't represent the default FrequencyModel settings on launch, everything would be 0
            Settings.ThemeSettings.FrequencyModel.Init();
        }

        public string GetRandomImagePath(int displayIndex)
        {
            return WallpaperRandomizer.NextWallpapers[displayIndex];
        }

        /*x
        public string GetRandomImagePath()
        {
            string[] imagePaths = Images.GetAllImagePaths();

            if (imagePaths.Length <= 0) return string.Empty;

            Random rand = new Random();
            int imageIndex = rand.Next(imagePaths.Length);

            return imagePaths[imageIndex];
        }
        */
    }
}
