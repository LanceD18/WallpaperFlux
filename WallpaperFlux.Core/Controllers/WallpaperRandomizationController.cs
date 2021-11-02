using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using LanceTools;
using MvvmCross;
using WallpaperFlux.Core.External;
using WallpaperFlux.Core.Util;

namespace WallpaperFlux.Core.Controllers
{
    public class WallpaperRandomizationController
    {
        public string[] ActiveWallpapers = new string[WallpaperUtil.DisplayUtil.GetDisplayCount()]; // holds paths of the currently active wallpapers
        public string[] NextWallpapers = new string[WallpaperUtil.DisplayUtil.GetDisplayCount()]; // derived from UpcomingWallpapers, holds the next set of wallpapers
        public Stack<string>[] PreviousWallpapers = new Stack<string>[WallpaperUtil.DisplayUtil.GetDisplayCount()]; // allows you to return back to every wallpaper encountered during the current session
        public Queue<string[]> UpcomingWallpapers = new Queue<string[]>(); // allows display-dependent wallpaper orders to be set without synced displays

        public WallpaperRandomizationController()
        {
            InitializePreviousWallpapers();
        }

        private void InitializePreviousWallpapers()
        {
            for (int i = 0; i < PreviousWallpapers.Length; i++) PreviousWallpapers[i] = new Stack<string>();
        }
        
        // The wallpaper must both exist within the theme and exist as a file to be valid
        public static bool IsWallpapersValid(string[] wallpapers)
        {
            foreach (string wallpaperPath in wallpapers)
            {
                //xif (!File.Exists(wallpaperPath))
                if (!DataUtil.Theme.Images.ContainsImage(wallpaperPath) || !File.Exists(wallpaperPath))
                {
                    Debug.WriteLine("Invalid Wallpaper Found: " + wallpaperPath);
                    return false;
                }
            }

            return true;
        }

        public bool SetNextWallpaperOrder(int index)
        {
            // this indicates that it's time to search for a new set of upcoming wallpapers
            //? ActiveWallpapers[index] == NextWallpapers[index] checks if the next set of wallpapers have been updated. If not, randomization will occur
            //? ignoreRandomization allows the next set of wallpapers to be directly applied. This helps the previous wallpapers setting function as intended
            if (ActiveWallpapers[index] == NextWallpapers[index])
            {
                // queues next set of upcoming wallpapers
                if (!RandomizeWallpapers())
                {
                    Debug.WriteLine("Randomization Failed");
                    return false;  //? allowing this to continue may cause UpcomingWallpapers to dequeue a null value and crash the program
                }

                Debug.WriteLine("Setting Next Set Wallpaper for Display " + index);
                NextWallpapers = UpcomingWallpapers.Dequeue();
            }

            ActiveWallpapers[index] = NextWallpapers[index];

            return true;
        }

        private bool RandomizeWallpapers()
        {
            Random rand = new Random();

            // Gather potential wallpapers

            string[] potentialWallpapers = new string[WallpaperUtil.DisplayUtil.GetDisplayCount()];
            for (int i = 0; i < WallpaperUtil.DisplayUtil.GetDisplayCount(); i++)
            {
                // Determine random image type
                ImageType imageTypeToSearchFor = ImageType.None;

                double staticChance = DataUtil.Theme.Settings.ThemeSettings.FrequencyCalc.GetExactFrequency(ImageType.Static);
                double gifChance = DataUtil.Theme.Settings.ThemeSettings.FrequencyCalc.GetExactFrequency(ImageType.GIF);
                double videoChance = DataUtil.Theme.Settings.ThemeSettings.FrequencyCalc.GetExactFrequency(ImageType.Video);

                if ((staticChance + gifChance + videoChance) == 0)
                {
                    MessageBoxUtil.ShowError("Unable to generate any wallpapers while all Exact Frequencies are set to 0");
                    return false;
                }

                ImageType[] imageTypeIndexes = { ImageType.Static, ImageType.GIF, ImageType.Video };
                double[] imageTypePercentages = { staticChance, gifChance, videoChance };

                //xMessageBoxUtil.ShowError("Huh: " + rand.NextPercentageIndex(imageTypePercentages, imageTypePercentages.Sum()));

                imageTypeToSearchFor = rand.NextInWeightedArray(imageTypeIndexes, imageTypePercentages);

                if (DataUtil.Theme.RankController.IsAllImagesOfTypeUnranked(imageTypeToSearchFor))
                {
                    MessageBoxUtil.ShowError("Attempted to set a wallpaper to an image type with no valid/ranked images." +
                                             "\nWallpaper Change Cancelled [IMAGE TYPE: " + imageTypeToSearchFor + "]" +
                                             "\n\nEither change relative frequency chance of the above image type to 0% (Under Frequency in the options menu)\n" +
                                             "or activate some wallpapers of the above image type (Unranked images with a rank of 0 are inactive");
                    return false;
                }

                int randomRank = GetRandomRank(ref rand, imageTypeToSearchFor);

                // Find random image path
                if (randomRank != -1)
                {
                    Debug.WriteLine("Setting Wallpaper: " + i);
                    potentialWallpapers[i] =  DataUtil.Theme.RankController.GetRandomImageOfRank(randomRank, ref rand, imageTypeToSearchFor);

                    if (!DataUtil.Theme.Images.GetImage(potentialWallpapers[i]).Active)
                    {
                        //! This shouldn't happen, if this does you have a bug to fix
                        MessageBoxUtil.ShowError("Attempted to set display " + i + " to an inactive wallpaper | A new wallpaper has been chosen");
                        i--; // find another wallpaper, the selected wallpaper is inactive
                    }
                }
                else
                {
                    Debug.WriteLine("-1 rank selected | Fix Code | This will occur if all ranks are 0");
                }
            }

            ModifyWallpaperOrder(ref potentialWallpapers);
            UpcomingWallpapers.Enqueue(potentialWallpapers);

            return true;
        }

        // Picks ranks based on their default percentiles (Where the highest rank is the most likely to appear and it goes down from there)
        private int GetRandomRank(ref Random rand, ImageType imageType)
        {
            Debug.WriteLine("Searching for: " + imageType);
            // the percentiles for weighted ranks change everytime an image's rank is altered or if the image type is not none
            if ((DataUtil.Theme.RankController.PercentileController.PotentialWeightedRankUpdate && DataUtil.Theme.Settings.ThemeSettings.WeightedRanks)
                || DataUtil.Theme.RankController.PercentileController.PotentialRegularRankUpdate
                || imageType != ImageType.None)
            {
                DataUtil.Theme.RankController.PercentileController.UpdateRankPercentiles(imageType); //? this method sets the above booleans to false
            }

            Dictionary<int, double> modifiedRankPercentiles = DataUtil.Theme.RankController.PercentileController.GetRankPercentiles(imageType);

            return rand.NextInWeightedArray(modifiedRankPercentiles.Keys.ToArray(), modifiedRankPercentiles.Values.ToArray());
        }

        #region Wallpaper Order Modifiers
        private void ModifyWallpaperOrder(ref string[] wallpapersToModify)
        {
            // TODO
            // request next 3 wallpapers, determine their preferred setting
            // set first display with said preferred setting
            // request next 3 wallpapers
            // set second display with preferred setting
            // no request
            // set first wallpaper to next wallpaper (Using second set of requested wallpapers)
            // request next 3 wallpapers, this changes the third wallpaper setting
            // set third display, this will use the *second* preferred setting (Using third set of requested wallpapers)
            // essentially, there will always be an upcoming set of preferred wallpapers, once that set surpassed, a new set will be made that all displays will have to follow

            if (IsWallpapersValid(wallpapersToModify))
            {
                string[] reorderedWallpapers = new string[0];
                // if looking for HigherRankedImage
                if (DataUtil.Theme.Settings.ThemeSettings.HigherRankedImagesOnLargerDisplays || DataUtil.Theme.Settings.ThemeSettings.LargerImagesOnLargerDisplays)
                {
                    int[] largestMonitorIndexOrder = Mvx.IoCProvider.Resolve<IExternalDisplayUtil>().GetLargestDisplayIndexOrder().ToArray();

                    if (DataUtil.Theme.Settings.ThemeSettings.HigherRankedImagesOnLargerDisplays)
                    {
                        reorderedWallpapers = (from f in wallpapersToModify orderby DataUtil.Theme.Images.GetImage(f).Rank descending select f).ToArray();

                        // both ranking and size are now a factor so first an image's rank will determine their index and then afterwards
                        // any ranking conflicts have their indexes determined by size rather than being random
                        if (DataUtil.Theme.Settings.ThemeSettings.LargerImagesOnLargerDisplays)
                        {
                            ConflictResolveIdenticalRanks(ref reorderedWallpapers);
                        }
                    }
                    else if (DataUtil.Theme.Settings.ThemeSettings.LargerImagesOnLargerDisplays)
                    {
                        reorderedWallpapers = LargestImagesWithCustomFilePath(wallpapersToModify);
                    }

                    //? Applies the final modification
                    wallpapersToModify = ApplyModifiedPathOrder(reorderedWallpapers, largestMonitorIndexOrder);
                }
            }
        }

        private void ConflictResolveIdenticalRanks(ref string[] reorderedWallpapers)
        {
            bool conflictFound = false;
            Dictionary<int, List<string>> rankConflicts = new Dictionary<int, List<string>>();
            foreach (string wallpaper in reorderedWallpapers)
            {
                int wallpaperRank = DataUtil.Theme.Images.GetImage(wallpaper).Rank;
                if (!rankConflicts.ContainsKey(wallpaperRank))
                {
                    rankConflicts.Add(wallpaperRank, new List<string> { wallpaper });
                }
                else // more than one wallpaper contains the same rank, they'll have to have their conflicts resolved below
                {
                    rankConflicts[wallpaperRank].Add(wallpaper);
                    conflictFound = true;
                }
            }

            if (conflictFound) // if this is false then nothing will happen and the original reorderedWallpapers value will persist
            {
                List<string> conflictResolvedOrder = new List<string>();
                foreach (int rank in rankConflicts.Keys)
                {
                    if (rankConflicts[rank].Count > 1) // conflict present, fix it by comparing image sizes and placing the largest image first
                    {
                        string[] conflictResolvedRank = LargestImagesWithCustomFilePath(rankConflicts[rank].ToArray());
                        foreach (string wallpaper in conflictResolvedRank)
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
        // however doing so is not needed for such a small collection
        private string[] LargestImagesWithCustomFilePath(string[] customFilePath)
        {
            IExternalImage[] images = new IExternalImage[customFilePath.Length];

            for (var i = 0; i < images.Length; i++)
            {
                images[i].SetImage(customFilePath[i]);

                //? Note that the tag is empty beforehand | This is used to organize the images below based on their width and height
                images[i].SetTag(customFilePath[i]);
            }

            customFilePath = (
                from f
                    in images 
                orderby f.GetSize().Width + f.GetSize().Height 
                    descending 
                select f.GetTag().ToString()).ToArray();

            // Dispose the loaded images once finished
            foreach (IExternalImage image in images) image.Dispose();

            return customFilePath;
        }

        private static string[] ApplyModifiedPathOrder(string[] reorderedWallpapers, int[] reorderedIndexes)
        {
            string[] newOrder = new string[reorderedWallpapers.Length];
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
