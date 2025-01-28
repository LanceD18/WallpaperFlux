using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using LanceTools;
using LanceTools.WPF.Adonis.Util;
using MvvmCross.Commands;
using WallpaperFlux.Core.Models.Controls;
using WallpaperFlux.Core.Util;
using WallpaperFlux.Core.ViewModels;

namespace WallpaperFlux.Core.Models
{
    public sealed class ImageSetModel : BaseImageModel
    {
        private readonly List<ImageModel> RelatedImages;

        public override bool IsImageSet => true;

        private ImageSetType _setType;
        public ImageSetType SetType
        {
            get => _setType;
            set
            {
                if (value == _setType) return;
                _setType = value;
                RaisePropertyChanged(() => IsAnimated);
            }
        }

        #region Ranking Format

        private ImageSetRankingFormat _rankingFormat;
        public ImageSetRankingFormat RankingFormat
        {
            get => _rankingFormat;
            set
            {
                SetProperty(ref _rankingFormat, value);

                /*
                switch (value)
                {
                    case ImageSetRankingFormat.Average:
                        UsingAverageRank = true;
                        break;

                    case ImageSetRankingFormat.WeightedAverage:
                        UsingWeightedAverage = true;
                        break;

                    case ImageSetRankingFormat.Override:
                        UsingOverrideRank = true;
                        break;

                    case ImageSetRankingFormat.WeightedOverride:
                        UsingWeightedOverride = true;
                        break;

                    default: throw new NotImplementedException();
                }

                if (RankingFormat != ImageSetRankingFormat.Average) UsingAverageRank = false;
                if (RankingFormat != ImageSetRankingFormat.Override) UsingOverrideRank = false;
                if (RankingFormat != ImageSetRankingFormat.WeightedAverage) UsingWeightedAverage = false;
                if (RankingFormat != ImageSetRankingFormat.WeightedOverride) UsingWeightedOverride = false;
                */

                RaisePropertyChanged(() => UsingAverageRank);
                RaisePropertyChanged(() => UsingOverrideRank);
                RaisePropertyChanged(() => UsingWeightedAverage);
                RaisePropertyChanged(() => UsingWeightedOverride);

                UpdateImageSetRank();
            }
        }

        private bool _usingAverageRank;
        public bool UsingAverageRank
        {
            get => RankingFormat == ImageSetRankingFormat.Average;
            set
            {
                if (value) RankingFormat = ImageSetRankingFormat.Average;
            }
        }

        private bool _usingWeightedAverage = true;
        // uses the weight scaling for wallpaper randomization & applies it to determining an average rank
        //? while with other methods the ranking may act as being within the bubble of the image set, weighted average takes into perspective the actual rate an image's rank
        public bool UsingWeightedAverage
        {
            get => RankingFormat == ImageSetRankingFormat.WeightedAverage;
            set
            {
                if (value) RankingFormat = ImageSetRankingFormat.WeightedAverage;
            }
        }

        private bool _usingOverrideRank;
        public bool UsingOverrideRank
        {
            get => RankingFormat == ImageSetRankingFormat.Override;
            set
            {
                if (value) RankingFormat = ImageSetRankingFormat.Override;
            }
        }

        private bool _usingWeightedOverride;
        public bool UsingWeightedOverride // combines average rank & override rank using a weight slider
        {
            get => RankingFormat == ImageSetRankingFormat.WeightedOverride;
            set
            {
                if (value) RankingFormat = ImageSetRankingFormat.WeightedOverride;
            }
        }

        private int _averageRank;
        public int AverageRank
        {
            get => _averageRank;
            private set => SetProperty(ref _averageRank, value);

            //xUpdateImageSetRank(); // ? keep in mind that the set will not update until the next load if this isn't handled properly (be sure to test if changing)
            //xRaisePropertyChanged(() => WeightedOverrideRank);
            //xRaisePropertyChanged(() => Rank);
        }

        private int _weightedAverageRank;

        public int WeightedAverageRank
        {
            get => _weightedAverageRank;
            private set => SetProperty(ref _weightedAverageRank, value);

            //xUpdateImageSetRank(); // ? keep in mind that the set will not update until the next load if this isn't handled properly (be sure to test if changing)
            //xRaisePropertyChanged(() => Rank);
        }

        private int _weightedOverrideRank;
        public int WeightedOverrideRank
        {
            get => _weightedOverrideRank;
            set => SetProperty(ref _weightedOverrideRank, MathE.Clamp(value, 0, ThemeUtil.RankController.GetMaxRank()));
        }

        private int _overrideRank;
        public int OverrideRank
        {
            get => _overrideRank;
            set
            {
                SetProperty(ref _overrideRank, MathE.Clamp(value, 0, ThemeUtil.RankController.GetMaxRank()));
                UpdateImageSetRank();
            }
            // it is possible for the user to save a value out of the range despite the actual rank being bounded
        }

        private int _overrideRankWeight = 50;
        public int OverrideRankWeight // weight of the override while weighted ranking is active
        {
            get => _overrideRankWeight;
            set
            {
                value = MathE.Clamp(value, 0, 100);
                SetProperty(ref _overrideRankWeight, value);

                RaisePropertyChanged(() => OverrideRankWeightText);
                UpdateImageSetRank(); // ? keep in mind that the set will not update until the next load if this isn't handled properly (be sure to test if changing)
            }
        }

        public string OverrideRankWeightText => "Weight: " + OverrideRankWeight;

        public bool UsingOverride => UsingOverrideRank || UsingWeightedOverride;
        #endregion

        #region Animated Set Properties

        private bool _fractionIntervals = true;
        public bool FractionIntervals
        {
            get => _fractionIntervals;
            set
            {
                SetProperty(ref _fractionIntervals, value);

                _staticIntervals = !value;
                RaisePropertyChanged(() => StaticIntervals);
            }
        }

        private bool _staticIntervals;
        public bool StaticIntervals
        {
            get => _staticIntervals;
            set
            {
                SetProperty(ref _staticIntervals, value);

                _fractionIntervals = !value;
                RaisePropertyChanged(() => FractionIntervals);
            }
        }

        public bool WeightedIntervals { get; set; } = true;

        #endregion

        private bool _retainImageIndependence = false;

        public bool RetainImageIndependence
        {
            get => _retainImageIndependence;
            set
            {
                SetProperty(ref _retainImageIndependence, value);
                
                foreach (ImageModel image in RelatedImages) image.VerifyIfRankValid(); // re-add to RankController
            }
        } //? while this is enabled, independent images and the set will co-exist in the theme

        ImageSetModel(ImageModel[] relatedImages, ImageType imageType, ImageSetType setType, ImageSetRankingFormat rankingFormat,
            int overrideRank, int overrideRankWeight, bool enabled = true)
        {
            base.ImageType = imageType;

            SetType = setType;

            RelatedImages = new List<ImageModel>();
            AddImageRange(relatedImages);

            Enabled = enabled;

            OnIsSelectedChanged += (value) =>
            {
                Debug.WriteLine("Updating selection of all images in image set to " + "[" + this.IsSelected + "]");
                foreach (ImageModel image in RelatedImages)
                {
                    image.IsSelected = this.IsSelected;
                }
            };

            //! must be called after images have been set, added, removed, or re-ranked
            //! now handled by the below switch case due to setter functions
            //xUpdateAverageRankAndWeightedAverage();

            //x TODO Make this enum format work with the XAML
            //x ? remember that setting one of these options to true automatically sets the rest to false with setter functions
            RankingFormat = rankingFormat;
            /*x
            switch (rankingFormat)
            {
                case ImageSetRankingFormat.Average:
                    UsingAverageRank = true;
                    break;

                case ImageSetRankingFormat.WeightedAverage:
                    UsingWeightedAverage = true;
                    break;

                case ImageSetRankingFormat.Override:
                    UsingOverrideRank = true;
                    break;

                case ImageSetRankingFormat.WeightedOverride:
                    UsingWeightedOverride = true;
                    break;
            }
            */

            OverrideRank = overrideRank;

            OverrideRankWeight = overrideRankWeight;
        }

        public class Builder
        {
            private readonly ImageSetModel _imageSetModel;

            public Builder(ImageModel[] images)
            {
                // ! go through the following checks before setting the image set

                if (images == null) return; // likely a cancelled operation
                if (images.Length == 0) return; // invalid

                //? Having mixed image types in an image set is currently invalid, may implement in the future
                HashSet<ImageType> encounteredImageTypes = new HashSet<ImageType>();

                foreach (ImageModel image in images)
                {
                    encounteredImageTypes.Add(image.ImageType);

                    if (encounteredImageTypes.Count > 1)
                    {
                        MessageBoxUtil.ShowError(ImageUtil.INVALID_IMAGE_SET_MESSAGE);
                        return;
                    }
                }

                _imageSetModel = new ImageSetModel(images, encounteredImageTypes.First(), ImageSetType.Alt, ImageSetRankingFormat.WeightedAverage, 0, 50);
            }

            public ImageSetModel Build()
            {
                if (_imageSetModel == null) return null;

                ThemeUtil.Theme.Images.AddImageSet(_imageSetModel);

                 _imageSetModel.UpdateEnabledState();
                return _imageSetModel;
            }
        }

        public ImageModel[] GetRelatedImages(bool checkForEnabled = true)
        {
            if (checkForEnabled)
            {
                List<ImageModel> enabledRelatedImages = new List<ImageModel>();

                foreach (ImageModel image in RelatedImages)
                {
                    if (image.IsEnabled(true))
                    {
                        enabledRelatedImages.Add(image);
                    }
                }

                return enabledRelatedImages.ToArray();
            }
            else
            {
                return RelatedImages.ToArray();
            }
        }

        public ImageModel GetRelatedImage(int index, bool checkForEnabled = true)
        {
            ImageModel[] relatedImages = GetRelatedImages(checkForEnabled);

            if (index < relatedImages.Length)
            {
                return GetRelatedImages(checkForEnabled)[index];
            }
            else
            {
                return null; // invalid index
            }
        }

        private bool AddImage(ImageModel image)
        {
            if (ValidateType(image.ImageType))
            {
                // if the image was selected, deselect it on entering the image set (they should only be selectable on selecting or inspecting the image set)
                image.IsSelected = false;
                image.ParentImageSet = this;
                RelatedImages.Add(image);
                return true;
            }

            return false;
        }

        public bool AddImageRange(ImageModel[] images)
        {
            bool success = true;
            foreach (ImageModel image in images) // ? images need to have their type validated
            {
                if (!AddImage(image))
                {
                    success = false;
                }
            }

            if (!success)
            {
                MessageBoxUtil.ShowError(ImageUtil.INVALID_IMAGE_SET_MESSAGE);
            }

            return success;
        }

        public bool RemoveImage(ImageModel image)
        {
            image.ParentImageSet = null;

            // for just in case the image was selected
            image.IsSelected = false; // ! WARNING: you cannot change an image's selection state after it has been removed from an image set while the set is under inspection

            bool success = RelatedImages.Remove(image);
            return success;
        }

        public bool ValidateType(ImageType otherType)
        {
            if (otherType != ImageType)
            {
                MessageBoxUtil.ShowError("Image Type mismatch between image and set, operation cancelled");
                return false;
            }

            return true;
        }

        //! Kept as a reminder that this is not supported at the moment
        public void SetRelatedImages(ImageModel[] newRelatedImages, ImageType imageType)
        {
            throw new Exception("We will only set this once, if you wish to add/remove from the Image set, create a new ImageSetModel");
        }

        public string[] GetImagePaths()
        {
            List<string> imagePaths = new List<string>();

            foreach (ImageModel image in RelatedImages)
            {
                imagePaths.Add(image.Path);
            }

            return imagePaths.ToArray();
        }

        public ImageModel GetHighestRankedImage(bool checkForEnabled = true)
        {
            // returns the highest ranked image in the set
            // if multiple images have the highest rank, we'll just return whichever one comes first

            int rank = -1;
            ImageModel highestRankedImage = null;

            foreach (ImageModel image in GetRelatedImages(checkForEnabled))
            {
                if (image.Rank > rank)
                {
                    highestRankedImage = image;
                    rank = image.Rank;
                }
            }

            return highestRankedImage;
        }


        public void UpdateAverageRank(bool checkForEnabled = true)
        {
            int rankSum = 0;

            foreach (ImageModel image in GetRelatedImages(checkForEnabled))
            {
                rankSum += image.Rank;
            }

            AverageRank = (int)Math.Round((double)rankSum / RelatedImages.Count);
        }

        public void UpdateWeightedOverrideRank()
        {
            float weightRatio = (float)OverrideRankWeight / 100;
            float weightedAverage = WeightedAverageRank * (1 - weightRatio);
            float weightedOverride = OverrideRank * weightRatio;

            WeightedOverrideRank = (int)Math.Round(weightedAverage + weightedOverride);
        }

        public void UpdateWeightedAverageRank()
        {
            ImageModel[] filteredRelatedImages = GetRelatedImages(!JsonUtil.IsLoadingData);

            //? follows how the Rank Percentiles function
            // TODO consider merging the logic of this and the PercentileController into a tool

            // Gather Weights
            int maxRank = ThemeUtil.Theme.RankController.GetMaxRank();

            double[] weights = new double[filteredRelatedImages.Length];
            double rankMultiplier = 10.0 / maxRank;

            double weightNumerator = 0;
            double weightDivisor = 0;
            for (int i = 0; i < filteredRelatedImages.Length; i++)
            {
                // if the rank of an image is 0 just set the weight to 0, calculating it will give us a weight of 1
                weights[i] = filteredRelatedImages[i].Rank != 0 ? Math.Pow(2, filteredRelatedImages[i].Rank * rankMultiplier) : 0;

                weightNumerator += filteredRelatedImages[i].Rank * weights[i];
                weightDivisor += weights[i];
            }

            if (weightDivisor == 0)
            {
                WeightedAverageRank = 0; // all images are of rank 0
                return;
            }

            WeightedAverageRank = (int)Math.Round(weightNumerator / weightDivisor);
        }

        private void UpdateAverageDependentRanks()
        {
            switch (RankingFormat)
            {
                case ImageSetRankingFormat.Average:
                    UpdateAverageRank();
                    break;

                case ImageSetRankingFormat.WeightedAverage:
                case ImageSetRankingFormat.WeightedOverride:
                    UpdateWeightedAverageRank();

                    // ! weighted override is dependent on the value of the Weighted Average rank
                    if (RankingFormat == ImageSetRankingFormat.WeightedOverride) UpdateWeightedOverrideRank();
                    break;
            }

            //xRaisePropertyChanged(() => WeightedOverrideRank);
            //xRaisePropertyChanged(() => Rank);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>Returns true if the rank was updated</returns>
        public bool UpdateImageSetRank()
        {
            int oldRank = _rank;
            int newRank = -1;

            if (RankingFormat != ImageSetRankingFormat.Override) UpdateAverageDependentRanks();

            switch (RankingFormat)
            {
                case ImageSetRankingFormat.Average: newRank = AverageRank; break;
                case ImageSetRankingFormat.Override: newRank = OverrideRank; break;
                case ImageSetRankingFormat.WeightedAverage: newRank = WeightedAverageRank; break;
                case ImageSetRankingFormat.WeightedOverride: newRank = WeightedOverrideRank; break;
                default: throw new ArgumentOutOfRangeException();
            }

            //! safety precaution ; if a calculation sends _rank above max rank or below 0 we will get a stack overflow as the old rank will always be bound to the rank range
            newRank = ThemeUtil.Theme.RankController.ClampValueToRankRange(newRank);

            if (oldRank != newRank || !ThemeUtil.Theme.RankController.VerifyImageRanking(this)) // ? if verification fails, update the rank anyways
            {
                ThemeUtil.Theme.RankController.ModifyRank(this, oldRank, ref newRank);

                SetProperty(ref _rank, newRank);
                RaisePropertyChanged(() => Rank); // ! required for ui update despite SetProperty() (will call Rank.Get(), make sure it doesn't reference back to here)
                WallpaperFluxViewModel.Instance.RaisePropertyChanged(() => WallpaperFluxViewModel.Instance.InspectedImageRankText);

                return true;
            }

            return false;
        }

        private bool Equals(ImageSetModel other) => RelatedImages.Equals(other.RelatedImages);

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ImageSetModel)obj);
        }

        public override int GetHashCode() => RelatedImages != null ? RelatedImages.GetHashCode() : 0;
    }
}
