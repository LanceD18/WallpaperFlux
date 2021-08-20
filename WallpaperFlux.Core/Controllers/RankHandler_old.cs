using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using AdonisUI.Controls;
using LanceTools;
using WallpaperFlux.Core.Models;
using WallpaperFlux.Core.Util;

namespace WallpaperFlux.Core.Collections
{
    public class RankHandler_old
    {
        //xprivate Dictionary<int, List<string>> RankData = new Dictionary<int, List<string>>(); 
        private ReactiveList<ReactiveList<string>> RankData = new ReactiveList<ReactiveList<string>>(); //? not stored in the JSON
        private double[] RankPercentiles;

        private Dictionary<int, double> ModifiedRankPercentiles;

        private bool potentialRegularRankUpdate;
        private bool potentialWeightedRankUpdate;

        //! temp
        private bool OptionsDataWeightedRanks = true;
        private bool OptionsDataWeightedFrequency = true;
        //! temp

        public RankHandler_old(int maxRank)
        {
            InitializeImagesOfType();

            RankData.OnListAddItem += RankData_OnParentListAddItem;
            RankData.OnListRemoveItem += RankData_OnParentListRemoveItem;

            ModifyMaxRank(maxRank);
        }

        public ImageModel[] GetImagesOfRank(int rank)
        {
            throw new NotImplementedException();
        }

        public ImageModel[] GetImagesOfRanks(int[] ranks)
        {
            throw new NotImplementedException();
        }

        public ImageModel[] GetAllRankedImages()
        {
            throw new NotImplementedException();
        }

        public void GenerateRankData(ImageModel[] images)
        {
            foreach (ImageModel image in images)
            {
                RankData[image.Rank].Add(image.Path);
            }
        }

        public void ModifyRank(ImageModel image, int oldRank, int newRank)
        {
            RankData[oldRank].Remove(image.Path);
            RankData[newRank].Add(image.Path);
        }

        private int GetMaxRank() => RankData.Count - 1;

        private bool ContainsRank(int rank) => rank >= 0 && rank < RankData.Count;

        private void ModifyMaxRank(int newMaxRank)
        {
            if (newMaxRank > 0) // note that rank 0 is reserved for unranked images
            {
                SetRankDataSize(newMaxRank);
                SetRankPercentiles(newMaxRank);
            }
            else
            {
                MessageBoxModel messageBox = new MessageBoxModel
                {
                    Text = "The max rank cannot be less than or equal to 0",
                    Icon = MessageBoxImage.Error,
                };

                MessageBox.Show(messageBox);
            }
        }

        private void SetRankDataSize(int newMaxRank)
        {
            if (RankData.Count == 0) // there is nothing in the RankData, this is likely the initialization, we can easily setup the data
            {
                RankData.Add(new ReactiveList<string>()); // this will be rank 0

                for (int i = 0; i < newMaxRank; i++)
                {
                    // adds rank 1 at a time to the max, there will be 1 additional slot to account for rank 0 (added above)
                    // due to this, you can directly reference an index by its rank
                    RankData.Add(new ReactiveList<string>());
                }
            }
            else // Update RankData, giving it a new size and adjusting the existing images accordingly
            {
                MessageBoxModel messageBox = new MessageBoxModel
                {
                    Text = "Are you sure you want to change the max rank? \n(All images will have their ranks adjusted according to this change)",
                    Caption = "Choose an option",
                    Icon = MessageBoxImage.Question,
                    Buttons = MessageBoxButtons.YesNo()
                };

                if (MessageBox.Show(messageBox) == MessageBoxResult.Yes)
                {
                    UpdateMaxRank(newMaxRank);
                }
            }
        }

        private void UpdateMaxRank(int newMaxRank)
        {
            int oldRankMax = GetMaxRank();
            float rankChangeRatio = (float)newMaxRank / oldRankMax;

            //! This needs to be placed right here otherwise ImageData will crash on trying to add the image to an unknown rank
            // Increase RankData's possible ranks if needed
            if (rankChangeRatio > 1) // newest rank max is higher than the current rank max
            {
                for (int i = oldRankMax; i < newMaxRank; i++)
                {
                    RankData.Add(new ReactiveList<string>());
                }
            }

            //xif (!IsLoadingData) // no need to update ranks if you aren't actually changing anything
            //x{
                // Re-rank existing images
                string[] images = DataUtil.Theme.Images.GetAllImagePaths();//xFileData.Keys.ToArray();
                foreach (string image in images)
                {
                    ImageModel imageModel = DataUtil.Theme.Images.GetImage(image);
                    if (imageModel.Rank != 0) // no need to modify the rank of any rank 0 images
                    {
                        int newRank = Math.Max((int)Math.Round((double)imageModel.Rank * rankChangeRatio), 1); // the Math.Max is used to ensure that no images are set to 0 (unranked)
                        imageModel.Rank = newRank;
                    }
                }

                //xWallpaperManagerForm.UpdateImageRanks(); Updates the  UI
            //x}

            //! This needs to be placed right here otherwise ImageData will crash on trying to remove the image from an unknown rank
            // Decrease RankData's possible ranks if needed
            if (rankChangeRatio < 1)
            {
                for (int i = oldRankMax; i > newMaxRank; i--)
                {
                    RankData.RemoveAt(RankData.Count - 1);
                }
            }

            //Debug.WriteLine(GetMaxRank());
        }

        // TODO Move this to PercentileHandler
        #region Percentiles
        private void SetRankPercentiles(int newMaxRank)
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
        private Dictionary<int, double> GetModifiedRankPercentiles(ImageType imageType)
        {
            double rankPercentagesTotal = 0;
            List<int> validRanks = new List<int>();
            for (int i = 0; i < RankData.Count; i++) // i == rank
            {
                if (RankData[i].Count != 0 && i != 0) // The use of i != 0 excludes unranked images
                {
                    if (imageType != ImageType.None) // if an image type is being searched for, check if contains any values
                    {
                        if (ImagesOfTypeRankData[imageType][i].Count == 0)
                        {
                            continue; // this rank is not valid since the selected image type is not present
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
        private Dictionary<int, double> GetWeightedRankPercentiles(ImageType imageType)
        {
            Dictionary<int, double> modifiedRankPercentiles = GetModifiedRankPercentiles(imageType);
            int[] validRanks = modifiedRankPercentiles.Keys.ToArray();

            int rankedImageCount = GetAllRankedImages().Length;
            double newRankPercentageTotal = 0;

            // sets the individual weighted percentage of each rank
            foreach (int rank in validRanks)
            {
                // If an image type is being searched for then only include the number of images from said image type
                double percentileModifier = imageType == ImageType.None ?
                    ((double)RankData[rank].Count / rankedImageCount) :
                    ((double)ImagesOfTypeRankData[imageType][rank].Count / rankedImageCount);

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
            Debug.WriteLine("Updating Weight Percentiles");
            potentialWeightedRankUpdate = false; //? Prevents this from being called often due to the potential performance costs
            potentialRegularRankUpdate = false; //? Prevents this from being called often due to the potential performance costs
            ModifiedRankPercentiles = OptionsDataWeightedRanks ? GetWeightedRankPercentiles(imageType) : GetModifiedRankPercentiles(imageType);

            if (OptionsDataWeightedFrequency)
            {
                UpdateImageTypeWeights();
            }
        }
        #endregion

        // TODO Move this ImageTypeHandler
        #region Image Type Weighting
        // TODO Consider merging ImagesOfType & ImagesOfTypeRankData with FileData & RankData [NOTE, this will add more loops to your general functions so I'd honestly advise against the merge]

        //! These ImageOfType variables are initialized under WallpaperData.InitializeImagesOfType() due to the fact that every time a theme is loaded these will be cleared so its
        //! best to have them initialized there instead of here

        // used to give the user more selection options
        private Dictionary<ImageType, Dictionary<string, ImageModel>> ImagesOfType;

        //? this doesn't need to be reactive lists since the regular RankData does enough to handle the issue presented (Checking if rank percentiles should be updated)
        private Dictionary<ImageType, List<List<string>>> ImagesOfTypeRankData;

        private Dictionary<ImageType, double> ImageTypeWeights = new Dictionary<ImageType, double>()
        {
            {ImageType.Static, 0},
            {ImageType.GIF, 0},
            {ImageType.Video, 0}
        };

        //! No longer needed currently, consider removing this in the future
        private Dictionary<ImageType, List<string>> ActiveImagesOfType;

        public void InitializeImagesOfType() //? this needs to be reloaded whenever a theme is loaded
        {
            ImagesOfType = new Dictionary<ImageType, Dictionary<string, ImageModel>>()
            {
                {ImageType.Static, new Dictionary<string, ImageModel>()},
                {ImageType.GIF, new Dictionary<string, ImageModel>()},
                {ImageType.Video, new Dictionary<string, ImageModel>()}
            };

            ImagesOfTypeRankData = new Dictionary<ImageType, List<List<string>>>()
            {
                {ImageType.Static, new List<List<string>>()},
                {ImageType.GIF, new List<List<string>>()},
                {ImageType.Video, new List<List<string>>()}
            };

            ActiveImagesOfType = new Dictionary<ImageType, List<string>>()
            {
                {ImageType.Static, new List<string>()},
                {ImageType.GIF, new List<string>()},
                {ImageType.Video, new List<string>()}
            };
        }

        public bool IsAllImagesOfTypeUnranked(ImageType imageType) => ImagesOfTypeRankData[imageType][0].Count == ImagesOfType[imageType].Count;

        public string[] GetAllImagesOfType(ImageType imageType) => ImagesOfType[imageType].Keys.ToArray();

        public int GetImagesOfTypeRankSum(ImageType imageType)
        {
            int count = 0;
            for (var i = 1; i < ImagesOfTypeRankData[imageType].Count; i++) //? i starts at 1 since rank 0 images are not included [Although they are likely inactive anyways]
            {
                List<string> rank = ImagesOfTypeRankData[imageType][i];
                count += rank.Count * i; // i = rank
            }

            return count;
        }

        public void UpdateImageTypeWeights()
        {
            int totalSum = 0;
            Dictionary<ImageType, int> ImageTypeRankSum = new Dictionary<ImageType, int>();
            foreach (ImageType imageType in ImageTypeWeights.Keys)
            {
                int sum = GetImagesOfTypeRankSum(imageType);
                ImageTypeRankSum.Add(imageType, sum);
                totalSum += sum;
            }

            foreach (ImageType imageType in ImageTypeRankSum.Keys)
            {
                ImageTypeWeights[imageType] = (double)ImageTypeRankSum[imageType] / totalSum;
            }
        }

        public double GetImageOfTypeWeight(ImageType imageType) => ImageTypeWeights[imageType];
        #endregion

        #region Events
        private void RankData_OnParentListAddItem(object sender, ListChangedEventArgs<ReactiveList<string>> e)
        {
            e.Item.OnListAddItem += RankData_OnListAddItem;
            e.Item.OnListRemoveItem += RankData_OnListRemoveItem;

            ImagesOfTypeRankData[ImageType.Static].Insert(e.Index, new List<string>());
            ImagesOfTypeRankData[ImageType.GIF].Insert(e.Index, new List<string>());
            ImagesOfTypeRankData[ImageType.Video].Insert(e.Index, new List<string>());
        }

        private void RankData_OnParentListRemoveItem(object sender, ListChangedEventArgs<ReactiveList<string>> e)
        {
            e.Item.OnListAddItem -= RankData_OnListAddItem;
            e.Item.OnListRemoveItem -= RankData_OnListRemoveItem;

            ImagesOfTypeRankData[ImageType.Static].RemoveAt(e.Index);
            ImagesOfTypeRankData[ImageType.GIF].RemoveAt(e.Index);
            ImagesOfTypeRankData[ImageType.Video].RemoveAt(e.Index);
        }

        private void RankData_OnListAddItem(object sender, ListChangedEventArgs<string> e)
        {
            //xif (!IsLoadingData) // UpdateRankPercentiles will be called once the loading ends
            //x{
                potentialWeightedRankUpdate = true;
                if ((sender as ReactiveList<string>).Count == 1) // allows the now unempty rank to be selected
                {
                    potentialRegularRankUpdate = true;
                }
            //x}
        }

        private void RankData_OnListRemoveItem(object sender, ListChangedEventArgs<string> e)
        {
            //xif (!IsLoadingData) // UpdateRankPercentiles will be called once the loading ends
            //x{
                potentialWeightedRankUpdate = true;
                if ((sender as ReactiveList<string>).Count == 0) // prevents the empty rank from being selected
                {
                    potentialRegularRankUpdate = true;
                }
            //x}
        }
        #endregion
    }
}
