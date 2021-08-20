using System;
using WallpaperFlux.Core.Collections;
using WallpaperFlux.Core.Controllers;

namespace WallpaperFlux.Core.Models.Theme
{
    //? Making this a regular class instead of a static class will make resetting its data easier on you
    public class ThemeModel
    {
        public SettingsModel Settings; // for options

        public ImageCollection Images;

        public RankController RankController;

        public FolderCollection Folders;

        public WallpaperRandomizationController Randomizer;

        public PercentileController PercentileController;

        public ThemeModel(int maxRank)
        {
            Settings = new SettingsModel(maxRank);
            Images = new ImageCollection();
            RankController = new RankController(maxRank);
            PercentileController = new PercentileController();
        }

        public string GetRandomImagePath()
        {
            string[] imagePaths = this.Images.GetAllImagePaths();

            if (imagePaths.Length <= 0) return string.Empty;

            Random rand = new Random();
            int imageIndex = rand.Next(imagePaths.Length);

            return imagePaths[imageIndex];
        }
    }
}
