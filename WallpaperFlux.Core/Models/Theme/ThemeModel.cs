using System;
using LanceTools;
using WallpaperFlux.Core.Collections;
using WallpaperFlux.Core.Controllers;

namespace WallpaperFlux.Core.Models.Theme
{
    public class ThemeModel
    {
        public SettingsModel Settings { get; set; } //? getter and setter needed for the XAML file

        public ImageCollection Images;

        public RankController RankController;

        public FolderCollection Folders;

        public WallpaperRandomizationController WallpaperRandomizer;

        public ThemeModel(int maxRank)
        {
            Settings = new SettingsModel(maxRank);
            Images = new ImageCollection();
            RankController = new RankController();
            WallpaperRandomizer = new WallpaperRandomizationController();

            RankController.SetMaxRank(maxRank); //! don't call this in the constructor of RankController to prevent potential initialization mishaps
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
