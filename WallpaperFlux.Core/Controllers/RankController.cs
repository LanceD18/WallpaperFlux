using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AdonisUI.Controls;
using LanceTools;
using LanceTools.Collections.Reactive;
using LanceTools.WPF.Adonis.Util;
using WallpaperFlux.Core.Collections;
using WallpaperFlux.Core.Models;
using WallpaperFlux.Core.Models.Tagging;
using WallpaperFlux.Core.Models.Theme;
using WallpaperFlux.Core.Util;

//!using WallpaperFlux.Core.Util; Avoid using the ThemeUtil value from this

namespace WallpaperFlux.Core.Controllers
{
    public class RankController
    {
        // Structure: [ImageType (Key)][Rank (Index)][Image Path (Value of Index)]
        private Dictionary<ImageType, ReactiveList<ReactiveHashSet<BaseImageModel>>> RankData = new Dictionary<ImageType, ReactiveList<ReactiveHashSet<BaseImageModel>>>
        {
            {ImageType.Static, new ReactiveList<ReactiveHashSet<BaseImageModel>>()},
            {ImageType.GIF, new ReactiveList<ReactiveHashSet<BaseImageModel>>()},
            {ImageType.Video, new ReactiveList<ReactiveHashSet<BaseImageModel>>()}
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
                RankData[imageType].OnListAddItem += RankData_OnAddRank;
                RankData[imageType].OnListRemoveItem += RankData_OnRemoveRank;
            }
        }

        //! this method should only be called by the Setter of the ImageModel Rank property unless otherwise noted
        public void ModifyRank(BaseImageModel image, int oldRank, ref int newRank, bool modifyingImageSet = false)
        {
            if (JsonUtil.IsLoadingData) return; //? this will be revisited for all images once loading is finished and all folders are re-validated

            // clamps the given rank to the rank-range for just in case something out-of-bounds is given
            if (!modifyingImageSet)
            {
                newRank = ClampValueToRankRange(newRank);
            }
            else
            {
                //? if we were to modify the rank reference the override rank would be updated every single time
                int testRank = ClampValueToRankRange(newRank);
                if (testRank != newRank)
                {
                    newRank = testRank;
                }
            }

            RankData[image.ImageType][oldRank].Remove(image);
            RankData[image.ImageType][newRank].Add(image);
        }

        //! should only be used in limited circumstances, the bool only exists to remind us of that (we don't want lost images)
        //! find a better solution to limit this procedure's access in the future
        public void RemoveRankedImage(BaseImageModel image, bool validUseCase_dummyParam)
        {
            RankData[image.ImageType][image.Rank].Remove(image);
        }

        //! should only be used in limited circumstances, the bool only exists to remind us of that (we don't want rogue images)
        //! find a better solution to limit this procedure's access in the future
        public void AddRankedImage(BaseImageModel image, bool validUseCase_dummyParam)
        {
            RankData[image.ImageType][image.Rank].Add(image);
        }

        public VariableRef<Dictionary<ImageType, ReactiveList<ReactiveHashSet<BaseImageModel>>>> CreateRankDataRef()
        {
            return new VariableRef<Dictionary<ImageType, ReactiveList<ReactiveHashSet<BaseImageModel>>>>(
                () => RankData, 
                dictionary => throw new Exception("Cannot set RankData"));
        }

        private bool ContainsRank(int rank, ImageType imageType) => rank >= 0 && rank < RankData[imageType].Count; // implies that everything is set up correctly but forgoes looping

        public BaseImageModel[] GetImagesOfRank(int rank, ImageType imageType) => RankData[imageType][rank].ToArray();

        public BaseImageModel[] GetImagesOfRank(int rank)
        {
            List<BaseImageModel> images = new List<BaseImageModel>();
            foreach (ImageType imageType in RankData.Keys)
            {
                images.AddRange(GetImagesOfRank(rank, imageType));
            }

            return images.ToArray();
        }

        public BaseImageModel[] GetAllUnrankedImages() => GetImagesOfRank(0);

        public BaseImageModel[] GetAllUnrankedImagesOfType(ImageType imageType) => GetImagesOfRank(0, imageType);

        public BaseImageModel[] GetAllRankedImages()
        {
            List<BaseImageModel> images = new List<BaseImageModel>();
            foreach (ImageType imageType in RankData.Keys)
            {
                images.AddRange(GetAllRankedImagesOfType(imageType));
            }

            return images.ToArray();
        }

        public BaseImageModel[] GetAllRankedImagesOfType(ImageType imageType)
        {
            List<BaseImageModel> images = new List<BaseImageModel>();
            for (int i = 1; i < RankData[imageType].Count; i++)  //? i = 1 ensures that rank 0 images are not included since they are 'unranked'
            {
                images.AddRange(GetImagesOfRank(i));
            }

            return images.ToArray();
        }

        public BaseImageModel[] GetAllImagesOfType(ImageType imageType)
        {
            List<BaseImageModel> images = new List<BaseImageModel>();
            for (int i = 0; i < RankData[imageType].Count; i++)
            {
                images.AddRange(GetImagesOfRank(i, imageType));
            }

            return images.ToArray();
        }

        public BaseImageModel GetRandomImageOfRank(int rank, ref Random rand)
        {
            BaseImageModel[] imagesOfRank = GetImagesOfRank(rank);
            int randomImage = rand.Next(0, imagesOfRank.Length);
            return imagesOfRank[randomImage];
        }

        public BaseImageModel GetRandomImageOfRank(int rank, ref Random rand, ImageType imageType)
        {
            return GetRandomImageOfRank(rank, ref rand, imageType, RankData);
        }

        public BaseImageModel GetRandomImageOfRank(int rank, ref Random rand, ImageType imageType, Dictionary<ImageType, ReactiveList<ReactiveHashSet<BaseImageModel>>> rankData)
        {
            int randomImage = rand.Next(0, rankData[imageType][rank].Count);
            return rankData[imageType][rank].ElementAt(randomImage);
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
                if (ThemeUtil.Theme != null)
                {
                    ThemeUtil.Theme.Settings.ThemeSettings.MaxRank = maxRank;
                }

            }
        }

        private void InitializeMaxRank(int maxRank)
        {
            foreach (ImageType imageType in RankData.Keys)
            {
                InitializeRankDataImageType(RankData, maxRank, imageType);
            }
        }

        public void InitializeRankDataImageType(Dictionary<ImageType, ReactiveList<ReactiveHashSet<BaseImageModel>>> rankData, int maxRank, ImageType imageType)
        {
            rankData[imageType].Add(new ReactiveHashSet<BaseImageModel>()); // this will be rank 0

            for (int i = 0; i < maxRank; i++)
            {
                // adds rank 1 at a time to the max, there will be 1 additional slot to account for rank 0 (added above)
                // due to this, you can directly reference an index by its rank
                rankData[imageType].Add(new ReactiveHashSet<BaseImageModel>());
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

                        RankData[imageType].Add(new ReactiveHashSet<BaseImageModel>());
                        Debug.WriteLine(i + " | " + imageType);
                    }
                }
            }

            //xif (!IsLoadingData) // no need to update ranks if you aren't actually changing anything
            //x{
            //! Do not loop this segment by ImageType, you will modify the rank of all images 3 times
            // Re-rank existing images
            string[] images = ThemeUtil.Theme.Images.GetAllImagePaths();//xFileData.Keys.ToArray();
            foreach (string image in images)
            {
                ImageModel imageModel = ThemeUtil.Theme.Images.GetImage(image);
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
        private void RankData_OnAddRank(object sender, ListChangedEventArgs<ReactiveHashSet<BaseImageModel>> e)
        {
            e.Item.OnHashSetAddItem += RankData_OnRankAddImage;
            e.Item.OnHashSetRemoveItem += RankData_OnRankRemoveImage;
        }

        private void RankData_OnRemoveRank(object sender, ListChangedEventArgs<ReactiveHashSet<BaseImageModel>> e)
        {
            e.Item.OnHashSetAddItem -= RankData_OnRankAddImage;
            e.Item.OnHashSetRemoveItem -= RankData_OnRankRemoveImage;
        }

        private void RankData_OnRankAddImage(object sender, HashSetChangedEventArgs<BaseImageModel> e)
        {
            //xif (!IsLoadingData) // UpdateRankPercentiles will be called once the loading ends
            //x{

            ReactiveHashSet<BaseImageModel> rank = (sender as ReactiveHashSet<BaseImageModel>);

            if (!e.Item.IsEnabled()) //? if the added image is not enabled, just remove it and move on
            {
                rank?.Remove(e.Item);
                return;
            }

            PercentileController.PotentialWeightedRankUpdate = true; // any update to a rank's collection could trigger a weighted rank update

            //? checks when the collection of a rank has a count of 1 image
            if (rank?.Count == 1) ValidateImageTypeExistence(e.Item);
            //x}
        }

        private void RankData_OnRankRemoveImage(object sender, HashSetChangedEventArgs<BaseImageModel> e)
        {
            //xif (!IsLoadingData) // UpdateRankPercentiles will be called once the loading ends
            //x{
            PercentileController.PotentialWeightedRankUpdate = true; // any update to a rank's collection could trigger a weighted rank update

            //? checks when the collection of a rank has a count of 0 images
            if (((ReactiveHashSet<BaseImageModel>)sender).Count == 0) ValidateImageTypeExistence(e.Item);
            //x}
        }

        private void ValidateImageTypeExistence(BaseImageModel image)
        {
            Debug.WriteLine("A recently adjusted rank is now empty");
            PercentileController.PotentialRegularRankUpdate = true; // ranks that do not have images should not be processed by the percentile controller, and vice versa for adding them back

            ImageUtil.PerformImageAction(image,
                imageModel => ThemeUtil.Theme.Settings.ThemeSettings.FrequencyCalc.VerifyImageTypeExistence(imageModel));
        }
        #endregion

        #region Rank Count & Weights/Sum
        /// <summary>
        /// Gets the sum of the collective ranks of all images of an image type, a rank 100 image adds 100 to the sum while a rank 0 image adds 0 to the sum
        /// </summary>
        /// <param name="imageType"></param>
        /// <returns></returns>
        public int GetImagesOfTypeRankSum(ImageType imageType)
        {
            int sum = 0;
            for (int i = 1; i < RankData[imageType].Count; i++) //? i starts at 1 since rank 0 images are not included (would be multiplied by 0 anyways, but this method reduces iterations)
            {
                sum += RankData[imageType][i].Count * i; // i = rank
            }

            return sum;
        }

        public int GetImagesOfTypeRankCount(ImageType imageType, int rank)
        {
            return RankData[imageType][rank].Count;
        }

        public int GetImagesOfTypeRankCountTotal(ImageType imageType)
        {
            int count = 0;
            for (int i = 1; i < RankData[imageType].Count; i++) //? i starts at 1 since rank 0 images are not included (would be multiplied by 0 anyways)
            {
                count += GetImagesOfTypeRankCount(imageType, i);
            }

            return count;
        }

        public int GetRankCount(int rank)
        {
            int count = 0;

            count += GetImagesOfTypeRankCount(ImageType.Static, rank);
            count += GetImagesOfTypeRankCount(ImageType.GIF, rank);
            count += GetImagesOfTypeRankCount(ImageType.Video, rank);

            return count;
        }

        public int GetRankCountTotal()
        {
            int count = 0;

            count += GetImagesOfTypeRankCountTotal(ImageType.Static);
            count += GetImagesOfTypeRankCountTotal(ImageType.GIF);
            count += GetImagesOfTypeRankCountTotal(ImageType.Video);

            return count;
        }

        public int GetRankCountOfTag(int rank, TagModel tag)
        {
            int count = 0;

            count += GetImagesOfTypeRankCountOfTag(ImageType.Static, rank, tag);
            count += GetImagesOfTypeRankCountOfTag(ImageType.GIF, rank, tag);
            count += GetImagesOfTypeRankCountOfTag(ImageType.Video, rank, tag);

            return count;
        }

        public int GetImagesOfTypeRankCountOfTag(ImageType imageType, int rank, TagModel tag)
        {
            int count = 0;

            Func<ImageModel, bool> verifyImageTags = imageModel =>
            {
                if (imageModel.ContainsTagOrChildTag(tag)) // TODO this function only exists on ImageModel yet you call this with a BaseImageModel
                {
                    count++;
                    return true;
                }

                return false;
            };
            
            foreach (BaseImageModel image in RankData[imageType][rank])
            {
                ImageUtil.PerformImageCheck(image, verifyImageTags);
            }

            return count;
        }

        public int GetRankSumOfImages(BaseImageModel[] images)
        {
            int count = 0;

            foreach (BaseImageModel image in images)
            {
                count += image.Rank;
            }

            return count;
        }

        public int GetRankSumOfImagesOfType(BaseImageModel[] images, ImageType type)
        {
            int count = 0;

            foreach (BaseImageModel image in images)
            {
                if (image.ImageType == type)
                {
                    count += image.Rank;
                }
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
            if (JsonUtil.IsLoadingData) return;
            if (FolderUtil.IsValidatingFolders) return;

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
            ThemeUtil.Theme.Settings.ThemeSettings.FrequencyCalc.UpdateFrequency(ImageType.Static, FrequencyType.Relative,
                ThemeUtil.Theme.Settings.ThemeSettings.FrequencyModel.RelativeFrequencyStatic, false);
        }

        public double GetImageOfTypeWeight(ImageType imageType) => ImageTypeWeights[imageType];
        #endregion
    }
}
