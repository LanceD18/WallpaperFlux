using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LanceTools;
using LanceTools.Collections.Reactive;
using LanceTools.IO;
using LanceTools.WPF.Adonis.Util;
using MvvmCross;
using WallpaperFlux.Core.IoC;
using WallpaperFlux.Core.Models;
using WallpaperFlux.Core.Util;
using WallpaperFlux.Core.ViewModels;

namespace WallpaperFlux.Core.Controllers
{
    public class WallpaperRandomizationController
    {
        public ReactiveArray<BaseImageModel> ActiveWallpapers = new ReactiveArray<BaseImageModel>(WallpaperUtil.DisplayUtil.GetDisplayCount());  // holds paths of the currently active wallpapers
        public BaseImageModel[] NextWallpapers = new BaseImageModel[WallpaperUtil.DisplayUtil.GetDisplayCount()]; // derived from UpcomingWallpapers, holds the next set of wallpapers
        public Stack<BaseImageModel>[] PreviousWallpapers = new Stack<BaseImageModel>[WallpaperUtil.DisplayUtil.GetDisplayCount()]; // allows you to return back to every wallpaper encountered during the current session

        public WallpaperRandomizationController()
        {
            InitializePreviousWallpapers();

            ActiveWallpapers.OnArrayChanged += (sender, args) => WallpaperFluxViewModel.Instance.UpdateActiveWallpapers(args.Item, args.Index);
        }

        private void InitializePreviousWallpapers()
        {
            for (int i = 0; i < PreviousWallpapers.Length; i++) PreviousWallpapers[i] = new Stack<BaseImageModel>();
        }
        
        // The wallpaper must both exist within the theme and exist as a file to be valid
        public static bool IsWallpapersValid(BaseImageModel[] wallpapers)
        {
            foreach (BaseImageModel wallpaper in wallpapers)
            {
                if (!ThemeUtil.Theme.Images.ContainsImage(wallpaper) || (wallpaper is ImageModel imageModel && !FileUtil.Exists(imageModel.Path)))
                {
                    Debug.WriteLine("Invalid Wallpaper Found: " + wallpaper);
                    return false;
                }
            }

            return true;
        }

        public bool SetNextWallpaperOrder(int index, bool forceRandomization)
        {
            // this indicates that it's time to search for a new set of upcoming wallpapers
            //? ActiveWallpapers[index] == NextWallpapers[index] checks if the next set of wallpapers have been updated. If not, randomization will occur
            //? ignoreRandomization allows the next set of wallpapers to be directly applied. This helps the previous wallpapers setting function as intended
            if (forceRandomization || Equals(ActiveWallpapers[index], NextWallpapers[index]) || NextWallpapers.IsEveryElementNull())
            {
                BaseImageModel[] nextWallpapers = RandomizeWallpapers();

                if (nextWallpapers == null) // queues next set of upcoming wallpapers
                {
                    Debug.WriteLine("Randomization Failed");
                    return false;
                }
                else
                {
                    NextWallpapers = nextWallpapers;
                }
            }

            PreviousWallpapers[index].Push(ActiveWallpapers[index]);
            ActiveWallpapers[index] = NextWallpapers[index];

            return true;
        }

        private BaseImageModel[] RandomizeWallpapers()
        {
            Random rand = new Random();

            // Gather potential wallpapers

            BaseImageModel[] potentialWallpapers = new BaseImageModel[WallpaperUtil.DisplayUtil.GetDisplayCount()];
            for (int i = 0; i < WallpaperUtil.DisplayUtil.GetDisplayCount(); i++)
            {
                // Determine random image type
                ImageType imageTypeToSearchFor = ImageType.None;

                double staticChance = ThemeUtil.Theme.Settings.ThemeSettings.FrequencyCalc.GetExactFrequency(ImageType.Static);
                double gifChance = ThemeUtil.Theme.Settings.ThemeSettings.FrequencyCalc.GetExactFrequency(ImageType.GIF);
                double videoChance = ThemeUtil.Theme.Settings.ThemeSettings.FrequencyCalc.GetExactFrequency(ImageType.Video);

                if (staticChance + gifChance + videoChance == 0)
                {
                    MessageBoxUtil.ShowError("Unable to generate any wallpapers while all Exact Frequencies are set to 0");
                    return null;
                }

                ImageType[] imageTypeIndexes = { ImageType.Static, ImageType.GIF, ImageType.Video };
                double[] imageTypePercentages = { staticChance, gifChance, videoChance };

                //xMessageBoxUtil.ShowError("Huh: " + rand.NextPercentageIndex(imageTypePercentages, imageTypePercentages.Sum()));

                imageTypeToSearchFor = rand.NextInWeightedArray(imageTypeIndexes, imageTypePercentages);

                if (ThemeUtil.Theme.RankController.IsAllImagesOfTypeUnranked(imageTypeToSearchFor))
                {
                    MessageBoxUtil.ShowError("Attempted to set a wallpaper to an image type with no valid/ranked images." +
                                             "\nWallpaper Change Cancelled [IMAGE TYPE: " + imageTypeToSearchFor + "]" +
                                             "\n\nEither change relative frequency chance of the above image type to 0% (Under Frequency in the options menu)\n" +
                                             "or activate some wallpapers of the above image type (Unranked images with a rank of 0 are inactive");
                    return null;
                }

                int randomRank = GetRandomRank(ref rand, imageTypeToSearchFor);

                // find random image path
                if (randomRank != -1)
                {
                    Debug.WriteLine("Setting Wallpaper: " + i);
                    potentialWallpapers[i] =  ThemeUtil.Theme.RankController.GetRandomImageOfRank(randomRank, ref rand, imageTypeToSearchFor);

                    if (!potentialWallpapers[i].Enabled)
                    {
                        //! This shouldn't happen, if this does you have a bug to fix
                        MessageBoxUtil.ShowError("Attempted to set display " + i + " to an inactive wallpaper | A new wallpaper has been chosen");
                        i--; // find another wallpaper, the selected wallpaper is inactive
                    }
                }
                else
                {
                    Debug.WriteLine("-1 rank selected | Either all ranks are 0 or all images are disabled");
                }
            }

            ModifyWallpaperOrder(ref potentialWallpapers);
            
            return potentialWallpapers;
        }

        // Picks ranks based on their default percentiles (Where the highest rank is the most likely to appear and it goes down from there)
        private int GetRandomRank(ref Random rand, ImageType imageType)
        {
            Debug.WriteLine("Searching for: " + imageType);
            // the percentiles for weighted ranks change everytime an image's rank is altered or if the image type is not none
            if ((ThemeUtil.Theme.RankController.PercentileController.PotentialWeightedRankUpdate && ThemeUtil.Theme.Settings.ThemeSettings.WeightedRanks)
                || ThemeUtil.Theme.RankController.PercentileController.PotentialRegularRankUpdate
                || imageType != ImageType.None)
            {
                ThemeUtil.Theme.RankController.PercentileController.UpdateRankPercentiles(imageType); //? this method sets the above booleans to false
            }

            return GetRandomRank(ref rand, imageType, ThemeUtil.Theme.RankController.PercentileController);
        }

        public static int GetRandomRank(ref Random rand, ImageType imageType, PercentileController percentileController)
        {
            Dictionary<int, double> modifiedRankPercentiles = percentileController.GetRankPercentiles(imageType);

            if (modifiedRankPercentiles.Count <= 0) return -1; // no ranks were valid (all had 0 images)

            return rand.NextInWeightedArray(modifiedRankPercentiles.Keys.ToArray(), modifiedRankPercentiles.Values.ToArray());
        }

        public static BaseImageModel GetRandomImageFromPreset(BaseImageModel[] images, ImageType imageType, bool checkForSet)
        {
            PercentileController percentileController = new PercentileController(images, imageType, checkForSet);

            Random rand = new Random();
            int randomRank = GetRandomRank(ref rand, imageType, percentileController);

            if (randomRank != -1)
            {
                return percentileController.GetRandomImageOfRank(randomRank, ref rand, imageType);
            }
            else
            {
                Debug.WriteLine("-1 rank selected | Either all ranks are 0 or all images are disabled");
                return null;
            }
        }

        #region Wallpaper Order Modifiers
        private void ModifyWallpaperOrder(ref BaseImageModel[] wallpapersToModify)
        {
            if (IsWallpapersValid(wallpapersToModify))
            {
                BaseImageModel[] reorderedWallpapers = Array.Empty<BaseImageModel>();
                // if looking for HigherRankedImage
                if (ThemeUtil.Theme.Settings.ThemeSettings.HigherRankedImagesOnLargerDisplays || ThemeUtil.Theme.Settings.ThemeSettings.LargerImagesOnLargerDisplays)
                {
                    int[] largestMonitorIndexOrder = Mvx.IoCProvider.Resolve<IExternalDisplayUtil>().GetLargestDisplayIndexOrder().ToArray();

                    if (ThemeUtil.Theme.Settings.ThemeSettings.HigherRankedImagesOnLargerDisplays)
                    {
                        reorderedWallpapers = (from f in wallpapersToModify orderby f.Rank descending select f).ToArray();

                        // both ranking and size are now a factor so first an image's rank will determine their index and then afterwards
                        // any ranking conflicts have their indexes determined by size rather than being random
                        if (ThemeUtil.Theme.Settings.ThemeSettings.LargerImagesOnLargerDisplays)
                        {
                            ConflictResolveIdenticalRanks(ref reorderedWallpapers);
                        }
                    }
                    else if (ThemeUtil.Theme.Settings.ThemeSettings.LargerImagesOnLargerDisplays)
                    {
                        reorderedWallpapers = LargestImages(wallpapersToModify);
                    }

                    //? Applies the final modification
                    wallpapersToModify = ApplyModifiedPathOrder(reorderedWallpapers, largestMonitorIndexOrder);
                }
            }
        }

        private void ConflictResolveIdenticalRanks(ref BaseImageModel[] reorderedWallpapers)
        {
            bool conflictFound = false;
            Dictionary<int, List<BaseImageModel>> rankConflicts = new Dictionary<int, List<BaseImageModel>>();
            foreach (BaseImageModel wallpaper in reorderedWallpapers)
            {
                int wallpaperRank = wallpaper.Rank;
                if (!rankConflicts.ContainsKey(wallpaperRank))
                {
                    rankConflicts.Add(wallpaperRank, new List<BaseImageModel> { wallpaper });
                }
                else // more than one wallpaper contains the same rank, they'll have to have their conflicts resolved below
                {
                    rankConflicts[wallpaperRank].Add(wallpaper);
                    conflictFound = true;
                }
            }

            if (conflictFound) // if this is false then nothing will happen and the original reorderedWallpapers value will persist
            {
                List<BaseImageModel> conflictResolvedOrder = new List<BaseImageModel>();
                foreach (int rank in rankConflicts.Keys)
                {
                    if (rankConflicts[rank].Count > 1) // conflict present, fix it by comparing image sizes and placing the largest image first
                    {
                        BaseImageModel[] conflictResolvedRank = LargestImages(rankConflicts[rank].ToArray());
                        foreach (BaseImageModel wallpaper in conflictResolvedRank)
                        {
                            conflictResolvedOrder.Add(wallpaper);
                        }
                    }
                    else
                    {
                        conflictResolvedOrder.Add(rankConflicts[rank][0]);
                    }
                }

                reorderedWallpapers = conflictResolvedOrder.ToArray();
            }
        }

        // the speed of this can be improved by not loading the image at all and instead using alternative options:
        // https://www.codeproject.com/Articles/35978/Reading-Image-Headers-to-Get-Width-and-Height
        // however doing so is not needed for such a small array (doubt anyone will have hundreds of monitors)
        private BaseImageModel[] LargestImages(BaseImageModel[] baseImages)
        {
            /*x
            IExternalImage[] images = new IExternalImage[baseImages.Length];

            for (var i = 0; i < images.Length; i++)
            {
                await Task.Run(() => images[i].SetImage(baseImages[i]));

                //? Note that the tag is empty beforehand | This is used to organize the images below based on their width and height
                images[i].SetTag(baseImages[i]);
            }

            baseImages = (
                from f
                    in images 
                orderby f.GetSize().Width + f.GetSize().Height 
                    descending 
                select f.GetTag().ToString()).ToArray();

            // Dispose the loaded images once finished
            foreach (IExternalImage image in images) image.Dispose();
            */

            ImageModel[] images = new ImageModel[baseImages.Length];

            for (var i = 0; i < baseImages.Length; i++)
            {
                images[i] = ImageUtil.GetImageModel(baseImages[i]);
            }

            baseImages = (
                from f 
                    in images 
                orderby f.GetSize().Width + f.GetSize().Height 
                    descending 
                select f).ToArray();

            return baseImages;
        }

        private static BaseImageModel[] ApplyModifiedPathOrder(BaseImageModel[] reorderedWallpapers, int[] reorderedIndexes)
        {
            BaseImageModel[] newOrder = new BaseImageModel[reorderedWallpapers.Length];
            for (int i = 0; i < newOrder.Length; i++)
            {
                newOrder[reorderedIndexes[i]] = reorderedWallpapers[i];
            }
            Debug.WriteLine("Modified Path Order Set");
            return newOrder;
        }
        #endregion
    }
}
