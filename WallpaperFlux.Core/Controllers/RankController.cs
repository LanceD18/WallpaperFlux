using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AdonisUI.Controls;
using LanceTools;
using LanceTools.Collections.Reactive;
using WallpaperFlux.Core.Collections;
using WallpaperFlux.Core.Models;
using WallpaperFlux.Core.Models.Theme;
using WallpaperFlux.Core.Util;

//!using WallpaperFlux.Core.Util; Avoid using the DataUtil value from this

namespace WallpaperFlux.Core.Controllers
{
    public class RankController
    {
        // Structure: [ImageType (Key)][Rank (Index)][Image Path (Value of Index)]
        private Dictionary<ImageType, ReactiveList<ReactiveHashSet<ImageModel>>> RankData = new Dictionary<ImageType, ReactiveList<ReactiveHashSet<ImageModel>>>()
        {
            {ImageType.Static, new ReactiveList<ReactiveHashSet<ImageModel>>()},
            {ImageType.GIF, new ReactiveList<ReactiveHashSet<ImageModel>>()},
            {ImageType.Video, new ReactiveList<ReactiveHashSet<ImageModel>>()}
        };

        private Dictionary<ImageType, double> ImageTypeWeights = new Dictionary<ImageType, double>()
        {
            {ImageType.Static, 0},
            {ImageType.GIF, 0},
            {ImageType.Video, 0}
        };

        public PercentileController PercentileController;

        public RankController()
        {
            PercentileController = new PercentileController(CreateRankDataRef());

            foreach (ImageType imageType in RankData.Keys)
            {
                RankData[imageType].OnListAddItem += RankData_OnParentListAddItem;
                RankData[imageType].OnListRemoveItem += RankData_OnParentListRemoveItem;
            }
        }

        //! this method should only be called by the Setter of the ImageModel Rank property unless otherwise noted
        public void ModifyRank(ImageModel image, int oldRank, ref int newRank)
        {
            // clamps the given rank to the rank-range for just in case something out-of-bounds is given
            //xDebug.WriteLine("ModifyRank: " + ContainsRank(newRank, image.ImageType) + " | " + RankData[image.ImageType].Count);

            newRank = ClampValueToRankRange(newRank);

            RankData[image.ImageType][oldRank].Remove(image);
            RankData[image.ImageType][newRank].Add(image);
        }

        //! the only purpose for ImageCollection to be here is to serve as a reminder that this should only be accessed after calling an image removal from ImageCollection
        //! find a better solution to limit this procedure's access in the future
        //? in makes collection readonly, not really needed but considering its already vague purpose it felt appropriate to add
        public void RemoveRankedImage(ImageModel image, params object[] args) => RankData[image.ImageType][image.Rank].Remove(image);

        public VariableRef<Dictionary<ImageType, ReactiveList<ReactiveHashSet<ImageModel>>>> CreateRankDataRef()
        {
            return new VariableRef<Dictionary<ImageType, ReactiveList<ReactiveHashSet<ImageModel>>>>(
                () => RankData, 
                dictionary => throw new Exception("Cannot set RankData"));
        }

        private bool ContainsRank(int rank, ImageType imageType) => rank >= 0 && rank < RankData[imageType].Count; // implies that everything is set up correctly but forgoes looping

        public ImageModel[] GetImagesOfRank(int rank, ImageType imageType) => RankData[imageType][rank].ToArray();

        public ImageModel[] GetImagesOfRank(int rank)
        {
            List<ImageModel> images = new List<ImageModel>();
            foreach (ImageType imageType in RankData.Keys)
            {
                images.AddRange(GetImagesOfRank(rank, imageType));
            }

            return images.ToArray();
        }

        public ImageModel[] GetAllUnrankedImages() => GetImagesOfRank(0);

        public ImageModel[] GetAllUnrankedImagesOfType(ImageType imageType) => GetImagesOfRank(0, imageType);

        public ImageModel[] GetAllRankedImages()
        {
            List<ImageModel> images = new List<ImageModel>();
            foreach (ImageType imageType in RankData.Keys)
            {
                images.AddRange(GetAllRankedImagesOfType(imageType));
            }

            return images.ToArray();
        }

        public ImageModel[] GetAllRankedImagesOfType(ImageType imageType)
        {
            List<ImageModel> images = new List<ImageModel>();
            for (int i = 1; i < RankData[imageType].Count; i++)  //? i = 1 ensures that rank 0 images are not included since they are 'unranked'
            {
                images.AddRange(GetImagesOfRank(i));
            }

            return images.ToArray();
        }

        public ImageModel[] GetAllImagesOfType(ImageType imageType)
        {
            List<ImageModel> images = new List<ImageModel>();
            for (int i = 0; i < RankData[imageType].Count; i++)
            {
                images.AddRange(GetImagesOfRank(i, imageType));
            }

            return images.ToArray();
        }

        public ImageModel GetRandomImageOfRank(int rank, ref Random rand)
        {
            ImageModel[] imagesOfRank = GetImagesOfRank(rank);
            int randomImage = rand.Next(0, imagesOfRank.Length);
            return imagesOfRank[randomImage];
        }

        public ImageModel GetRandomImageOfRank(int rank, ref Random rand, ImageType imageType)
        {
            int randomImage = rand.Next(0, RankData[imageType][rank].Count);
            return RankData[imageType][rank].ElementAt(randomImage);
        }

        public int ClampValueToRankRange(int value)
        {
            return MathE.Clamp(value, 0, GetMaxRank());
        }

        #region Rank Size Modifier
        //? should be the same across image types! [Counts the Ranks from 0 - x] ; the - 1 accounts for Rank 0 adding an extra rank count
        //? should be the same across image types! [Counts the Ranks from 0 - x] ; the - 1 accounts for Rank 0 adding an extra rank count
        //? should be the same across image types! [Counts the Ranks from 0 - x] ; the - 1 accounts for Rank 0 adding an extra rank count
        public int GetMaxRank() => RankData[ImageType.Static].Count - 1; 

        public void SetMaxRank(int maxRank)
        {
            Debug.WriteLine("Setting Max Rank to: " + maxRank);
            if (maxRank > 0) // note that rank 0 is reserved for unranked images
            {
                SetRankDataSize(maxRank);
            }
            else
            {
                MessageBoxUtil.ShowError("The max rank cannot be less than or equal to 0");
            }
        }

        private void SetRankDataSize(int maxRank)
        {
            bool isRankDataEmpty = true; //? this includes unranked images so we cannot use GetAllRankedImages()
            foreach (ImageType imageType in RankData.Keys)
            {
                if (RankData[imageType].Count != 0)
                {
                    isRankDataEmpty = false;
                    break;
                }
            }

            bool rankWasUpdated = false;
            if (isRankDataEmpty) // there is nothing in the RankData, this is likely the initialization, we can easily setup the data
            {
                InitializeMaxRank(maxRank);
                rankWasUpdated = true;
            }
            else // Update RankData, giving it a new size and adjusting the existing images accordingly
            {
                if (MessageBoxUtil.PromptYesNo("Are you sure you want to change the max rank? \n(All images will have their ranks adjusted in proportion to this change)"))
                {
                    UpdateMaxRank(maxRank);
                    rankWasUpdated = true;
                }
            }

            if (rankWasUpdated) //! Must come after the Max Rank is Updated/Initialized in the RankController
            {
                PercentileController.SetRankPercentiles(maxRank);
                if (DataUtil.Theme != null)
                {
                    DataUtil.Theme.Settings.ThemeSettings.MaxRank = maxRank;
                }

            }
        }

        private void InitializeMaxRank(int maxRank)
        {
            foreach (ImageType imageType in RankData.Keys)
            {
                RankData[imageType].Add(new ReactiveHashSet<ImageModel>()); // this will be rank 0

                for (int i = 0; i < maxRank; i++)
                {
                    // adds rank 1 at a time to the max, there will be 1 additional slot to account for rank 0 (added above)
                    // due to this, you can directly reference an index by its rank
                    RankData[imageType].Add(new ReactiveHashSet<ImageModel>());
                }
            }
        }

        private void UpdateMaxRank(int newMaxRank)
        {
            int oldRankMax = GetMaxRank(); // this should be the same for all categories, no need to check this multiple times
            if (oldRankMax == newMaxRank) return; // no need to make any changes

            float rankChangeRatio = (float)newMaxRank / oldRankMax;

            //! This needs to be placed right here otherwise ImageData will crash on trying to add the image to an unknown rank
            // Increase RankData's possible ranks if needed
            foreach (ImageType imageType in RankData.Keys) //! This loop cannot cover the entire method as the rank modification of the entire image collection needs to act outside of this
            {
                if (rankChangeRatio > 1) // newest rank max is higher than the current rank max
                {
                    for (int i = oldRankMax; i < newMaxRank; i++)
                    {
                        if (JsonUtil.IsLoadingData) Debug.WriteLine("ERROR: This is unnecessary processing that should be avoided, especially for larger themes, under UpdateMaxRank()");

                        RankData[imageType].Add(new ReactiveHashSet<ImageModel>());
                        Debug.WriteLine(i + " | " + imageType);
                    }
                }
            }

            //xif (!IsLoadingData) // no need to update ranks if you aren't actually changing anything
            //x{
            //! Do not loop this segment by ImageType, you will modify the rank of all images 3 times
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
            //x}

            //! This needs to be placed right here otherwise ImageData will crash on trying to remove the image from an unknown rank
            // Decrease RankData's possible ranks if needed
            foreach (ImageType imageType in RankData.Keys) //! This loop cannot cover the entire method as the rank modification of the entire image collection needs to act outside of this
            {
                if (rankChangeRatio < 1)
                {
                    for (int i = oldRankMax; i > newMaxRank; i--)
                    {
                        RankData[imageType].RemoveAt(RankData.Count - 1);
                    }
                }
            }

            //xDebug.WriteLine(GetMaxRank());
        }
        #endregion

        #region Events
        private void RankData_OnParentListAddItem(object sender, ListChangedEventArgs<ReactiveHashSet<ImageModel>> e)
        {
            e.Item.OnHashSetAddItem += RankData_OnListAddItem;
            e.Item.OnHashSetRemoveItem += RankData_OnListRemoveItem;
        }

        private void RankData_OnParentListRemoveItem(object sender, ListChangedEventArgs<ReactiveHashSet<ImageModel>> e)
        {
            e.Item.OnHashSetAddItem -= RankData_OnListAddItem;
            e.Item.OnHashSetRemoveItem -= RankData_OnListRemoveItem;
        }

        private void RankData_OnListAddItem(object sender, HashSetChangedEventArgs<ImageModel> e)
        {
            //xif (!IsLoadingData) // UpdateRankPercentiles will be called once the loading ends
            //x{
            PercentileController.PotentialWeightedRankUpdate = true;
            if ((sender as ReactiveHashSet<ImageModel>).Count == 1) // allows the now unempty rank to be selected
            {
                Debug.WriteLine("A recently adjusted rank now has one image");
                PercentileController.PotentialRegularRankUpdate = true;

                // e.Item represents the added image
                DataUtil.Theme.Settings.ThemeSettings.FrequencyCalc.VerifyImageTypeExistence(e.Item);
            }
            //x}
        }

        private void RankData_OnListRemoveItem(object sender, HashSetChangedEventArgs<ImageModel> e)
        {
            //xif (!IsLoadingData) // UpdateRankPercentiles will be called once the loading ends
            //x{
            PercentileController.PotentialWeightedRankUpdate = true;
            if ((sender as ReactiveHashSet<ImageModel>).Count == 0) // prevents the empty rank from being selected
            {
                Debug.WriteLine("A recently adjusted rank is now empty");
                PercentileController.PotentialRegularRankUpdate = true;

                // e.Item represents the removed image
                DataUtil.Theme.Settings.ThemeSettings.FrequencyCalc.VerifyImageTypeExistence(e.Item);
            }
            //x}
        }
        #endregion

        #region ImageTypeWeights
        /// <summary>
        /// Gets the sum of the collective ranks of all images of an image type, a rank 100 image adds 100 to the sum while a rank 0 image adds 0 to the sum
        /// </summary>
        /// <param name="imageType"></param>
        /// <returns></returns>
        public int GetImagesOfTypeRankSum(ImageType imageType)
        {
            int count = 0;
            for (var i = 1; i < RankData[imageType].Count; i++) //? i starts at 1 since rank 0 images are not included
            {
                count += RankData[imageType][i].Count * i; // i = rank
            }

            return count;
        }

        public bool IsAnyImagesOfTypeRanked(ImageType imageType)
        {
            for (var i = 1; i < RankData[imageType].Count; i++) //? i starts at 1 since rank 0 images are not included
            {
                if (RankData[imageType][i].Count > 0)
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsAllImagesOfTypeUnranked(ImageType imageType) => !IsAnyImagesOfTypeRanked(imageType);

        public void UpdateImageTypeWeights()
        {
            int totalSum = 0;
            Dictionary<ImageType, int> imageTypeRankSum = new Dictionary<ImageType, int>();
            foreach (ImageType imageType in ImageTypeWeights.Keys)
            {
                int sum = GetImagesOfTypeRankSum(imageType);
                imageTypeRankSum.Add(imageType, sum);
                totalSum += sum;
            }

            foreach (ImageType imageType in imageTypeRankSum.Keys)
            {
                ImageTypeWeights[imageType] = (double)imageTypeRankSum[imageType] / totalSum;
                Debug.WriteLine("New Weight [" + imageType + "]: " + ImageTypeWeights[imageType]);
            }

            //? the image type doesn't matter here, only calling this so that the exact frequencies can be updated
            //? what DOES matter is that we need to choose to ""update"" the relative frequency so that the exact frequencies can be recalculated
            //? this is because there's no need to change the relative frequencies here, they are always relative, only the exact frequencies should be touched
            DataUtil.Theme.Settings.ThemeSettings.FrequencyCalc.UpdateFrequency(ImageType.Static, FrequencyType.Relative,
                DataUtil.Theme.Settings.ThemeSettings.FrequencyModel.RelativeFrequencyStatic);
        }

        public double GetImageOfTypeWeight(ImageType imageType) => ImageTypeWeights[imageType];
        #endregion
    }
}
