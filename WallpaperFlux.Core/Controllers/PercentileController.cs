using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HandyControl.Tools.Extension;
using LanceTools;
using LanceTools.Collections.Reactive;
using WallpaperFlux.Core.Models;
using WallpaperFlux.Core.Models.Theme;
using WallpaperFlux.Core.Util;

namespace WallpaperFlux.Core.Controllers
{
    public class PercentileController
    {
        private double[] RankPercentiles;

        // int = rank, double = percentile
        private Dictionary<int, double> ModifiedRankPercentiles = new Dictionary<int, double>();

        private VariableRef<Dictionary<ImageType, ReactiveList<ReactiveHashSet<BaseImageModel>>>> RankData;

        public bool PotentialWeightedRankUpdate;
        public bool PotentialRegularRankUpdate;

        public PercentileController(VariableRef<Dictionary<ImageType, ReactiveList<ReactiveHashSet<BaseImageModel>>>> rankData) // for the core RankData we typically use
        {
            RankData = rankData;
        }

        //! This was initially intended to be used as a VariableRef only to RankController, this context creates some oddities such as the need to use dummyRankData
        public PercentileController(BaseImageModel[] images, ImageType imageType, bool checkForSet) // for subset percentile groups
        {
            Dictionary<ImageType, ReactiveList<ReactiveHashSet<BaseImageModel>>> dummyRankData = new Dictionary<ImageType, ReactiveList<ReactiveHashSet<BaseImageModel>>>
            {
                {ImageType.Static, new ReactiveList<ReactiveHashSet<BaseImageModel>>()},
                {ImageType.GIF, new ReactiveList<ReactiveHashSet<BaseImageModel>>()},
                {ImageType.Video, new ReactiveList<ReactiveHashSet<BaseImageModel>>()}
            };

            RankData = new VariableRef<Dictionary<ImageType, ReactiveList<ReactiveHashSet<BaseImageModel>>>>(
                () => dummyRankData,
                dictionary => throw new Exception("Cannot set RankData"));

            ThemeUtil.RankController.InitializeRankDataImageType(RankData.Get(), ThemeUtil.RankController.GetMaxRank(), imageType);

            foreach (BaseImageModel image in images)
            {
                if (image.IsEnabled(checkForSet))
                {
                    RankData.Get()[imageType][image.Rank].Add(image);
                }
            }

            SetRankPercentiles(ThemeUtil.RankController.GetMaxRank());
        }

        public void SetRankPercentiles(int newMaxRank)
        {
            // Set Rank Percentiles
            RankPercentiles = new double[newMaxRank];
            double rankMultiplier = 10.0 / newMaxRank;

            for (int i = 0; i < newMaxRank; i++)
            {
                // This is the default formula for rank percentiles, where each 10% of ranks has twice the probability of the previous 10%
                // Due to the rank multiplier, the max rank will always have a probability of 1024
                // ex: if the max rank is 100, rank 100 will have a probability of 1024 while rank 90 will have a probability of 512. These same numbers apply to 45 and 50 if the max is 50
                //? Note that the below formula does not include rank 0 as 0 * rankMultiplier is Rank 1
                //? When the percentages are calculated, Rank 1 will still be possible despite a score of 0 as the percentage uses
                //? the range from 0 to 1 instead of the 0 itself
                RankPercentiles[i] = Math.Pow(2, i * rankMultiplier);
            }
        }

        /// <summary>
        /// Modifies rank percentiles to represent the actual percentage chance of the rank appearing
        /// (The percentages of each rank will be modified to exclude images with a rank of 0)
        /// </summary>
        /// <returns></returns>
        //? You should call UpdateRankPercentiles instead if that's what's you need
        // in prevents reassignment https://stackoverflow.com/questions/2339074/can-parameters-be-constant/48068110#48068110
        private Dictionary<int, double> GetModifiedRankPercentiles(ImageType imageType)
        {
            double rankPercentagesTotal = 0;
            List<int> validRanks = new List<int>();
            for (int i = 0; i < RankData.Get()[imageType].Count; i++) // i == rank | Remember that the count should always be 1 more than the max rank
            {
                if (RankData.Get()[imageType][i].Count != 0 && i != 0) // The use of i != 0 excludes unranked images
                {
                    if (imageType != ImageType.None) // if an image type is being searched for, check if contains any values
                    {
                        if (RankData.Get()[imageType][i].Count == 0)
                        {
                            continue; // a rank of 0 is not valid
                        }
                    }

                    rankPercentagesTotal += RankPercentiles[i - 1];
                    validRanks.Add(i);
                }
            }

            Dictionary<int, double> modifiedRankPercentiles = new Dictionary<int, double>();

            // scales the percentages to account for ranks that weren't included
            foreach (int rank in validRanks)
            {
                modifiedRankPercentiles.Add(rank, RankPercentiles[rank - 1] / rankPercentagesTotal);
                //xDebug.WriteLine("Rank: " + rank + " | Percentile: " + modifiedRankPercentiles[rank]);
            }

            return modifiedRankPercentiles;
        }

        /// <summary>
        /// Weights rank percentiles on both how high the rank is and how many images are in a rank
        /// </summary>
        //? You should call UpdateRankPercentiles instead if that's what's you need
        // TODO Modify this in a way that doesn't need GetModifiedRankPercentiles(), removing the need to loop twice. I doubt this will have much of a performance
        // TODO impact however considering how fast it already is. It may be best to just leave it as is for convenience
        // in prevents reassignment https://stackoverflow.com/questions/2339074/can-parameters-be-constant/48068110#48068110
        private Dictionary<int, double> GetWeightedRankPercentiles(ImageType imageType)
        {
            Debug.WriteLine("Getting Weighted Rank Percentiles");
            if (imageType == ImageType.None) return null;

            Dictionary<int, double> modifiedRankPercentiles = GetModifiedRankPercentiles(imageType);
            int[] validRanks = modifiedRankPercentiles.Keys.ToArray();

            int rankedImageCount = ThemeUtil.Theme.RankController.GetAllRankedImages().Length;
            double newRankPercentageTotal = 0;

            // sets the individual weighted percentage of each rank
            foreach (int rank in validRanks)
            {
                // If an image type is being searched for then only include the number of images from said image type
                double percentileModifier = ((double)RankData.Get()[imageType][rank].Count / rankedImageCount);

                modifiedRankPercentiles[rank] *= percentileModifier;
                newRankPercentageTotal += modifiedRankPercentiles[rank];
            }

            // rescales the percentages to account for weighting
            foreach (int rank in validRanks)
            {
                modifiedRankPercentiles[rank] /= newRankPercentageTotal;
                //xDebug.WriteLine("Rank: " + rank + " | Weighted Percentile: " + modifiedRankPercentiles[rank]);
            }

            return modifiedRankPercentiles;
        }

        public Dictionary<int, double> GetRankPercentiles(ImageType imageType)
        {
            // sets up the ModifiedRankPercentiles variable if it's empty
            if (ModifiedRankPercentiles.Count == 0)
            {
                UpdateRankPercentiles(imageType);
            }

            return ModifiedRankPercentiles;
        }

        // TODO Async this at some point, but remember that this has to be done before the wallpaper is set (So the await key must be in the SetWallpaper method)
        public void UpdateRankPercentiles(ImageType imageType)
        {
            Debug.WriteLine("Updating Rank Percentiles");
            PotentialWeightedRankUpdate = false; //? Prevents this from being called often due to the potential performance costs
            PotentialRegularRankUpdate = false; //? Prevents this from being called often due to the potential performance costs

            ModifiedRankPercentiles = ThemeUtil.Theme.Settings.ThemeSettings.WeightedRanks ? GetWeightedRankPercentiles(imageType) : GetModifiedRankPercentiles(imageType);

            // Update Image Type Weights if the Weighted Frequency option is checked
            if (ThemeUtil.Theme.Settings.ThemeSettings.FrequencyModel.WeightedFrequency)
            {
                ThemeUtil.Theme.RankController.UpdateImageTypeWeights();
            }
        }

        public BaseImageModel GetRandomImageOfRank(int rank, ref Random rand, ImageType imageType)
        {
            return ThemeUtil.RankController.GetRandomImageOfRank(rank, ref rand, imageType, RankData);
        }
    }
}
