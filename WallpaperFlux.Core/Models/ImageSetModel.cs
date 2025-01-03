﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LanceTools;
using LanceTools.WPF.Adonis.Util;
using MvvmCross.Commands;
using WallpaperFlux.Core.Util;

namespace WallpaperFlux.Core.Models
{
    public sealed class ImageSetModel : BaseImageModel
    {
        private readonly ImageModel[] RelatedImages;

        public override bool IsImageSet => true;

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
        public ImageSetRankingFormat RankingFormat { get; set; }

        private bool _usingAverageRank;
        public bool UsingAverageRank
        {
            get => _usingAverageRank;
            set
            {
                SetProperty(ref _usingAverageRank, value);

                if (value)
                {
                    RankingFormat = ImageSetRankingFormat.Average;

                    UsingOverrideRank = false;
                    UsingWeightedOverride = false;
                    UsingWeightedAverage = false;
                    UpdateAverageRankAndWeightedAverage();

                }
            }
        }

        private bool _usingOverrideRank;
        public bool UsingOverrideRank
        {
            get => _usingOverrideRank;
            set
            {
                SetProperty(ref _usingOverrideRank, value);

                if (value)
                {
                    RankingFormat = ImageSetRankingFormat.Override;

                    UsingAverageRank = false;
                    UsingWeightedOverride = false;
                    UsingWeightedAverage = false;
                    RaisePropertyChanged(() => Rank);

                }
            }
        }

        private bool _usingWeightedOverride;
        public bool UsingWeightedOverride // combines average rank & override rank using a weight slider
        {
            get => _usingWeightedOverride;
            set
            {
                SetProperty(ref _usingWeightedOverride, value);
                
                if (value)
                {
                    RankingFormat = ImageSetRankingFormat.WeightedOverride;

                    UsingAverageRank = false;
                    UsingOverrideRank = false;
                    UsingWeightedAverage = false;

                    RaisePropertyChanged(() => WeightedRank);
                    RaisePropertyChanged(() => Rank);

                }
            }
        }

        private bool _usingWeightedAverage = true;
        // uses the weight scaling for wallpaper randomization & applies it to determining an average rank
        //? while with other methods the ranking may act as being within the bubble of the image set, weighted average takes into perspective the actual rate an image's rank
        public bool UsingWeightedAverage
        {
            get => _usingWeightedAverage;
            set
            {
                SetProperty(ref _usingWeightedAverage, value);

                if (value)
                {
                    RankingFormat = ImageSetRankingFormat.WeightedAverage;

                    UsingAverageRank = false;
                    UsingOverrideRank = false;
                    UsingWeightedOverride = false;

                    UpdateWeightedAverage();
                }
            }
        }

        private int _averageRank;
        public int AverageRank
        {
            get => _averageRank;
            private set
            {
                SetProperty(ref _averageRank, value);

                VerifyImageSetRank(); // ? keep in mind that the set will not update until the next load if this isn't handled properly (be sure to test if changing)
                RaisePropertyChanged(() => WeightedRank);
                RaisePropertyChanged(() => Rank);
            }
        }

        public int WeightedRank => GetWeightedRank();

        private int _overrideRank;
        public int OverrideRank
        {
            get => _overrideRank;
            set
            {
                // it is possible for the user to save a value out of the range despite the actual rank being bounded
                SetProperty(ref _overrideRank, MathE.Clamp(value, 0, ThemeUtil.RankController.GetMaxRank()));

                VerifyImageSetRank(); // ? keep in mind that the set will not update until the next load if this isn't handled properly (be sure to test if changing)
                RaisePropertyChanged(() => WeightedRank);
                RaisePropertyChanged(() => Rank);
            }
        }

        private int _overrideRankWeight = 50;
        public int OverrideRankWeight // weight of the override while weighted ranking is active
        {
            get => _overrideRankWeight;
            set
            {
                value = MathE.Clamp(value, 0, 100);
                SetProperty(ref _overrideRankWeight, value);

                VerifyImageSetRank(); // ? keep in mind that the set will not update until the next load if this isn't handled properly (be sure to test if changing)
                RaisePropertyChanged(() => OverrideRankWeightText);
                RaisePropertyChanged(() => Rank);
            }
        }

        private int _weightedAverage;
        private ImageSetType _setType;

        public int WeightedAverage
        {
            get => _weightedAverage;
            private set
            {
                SetProperty(ref _weightedAverage, value);

                VerifyImageSetRank(); // ? keep in mind that the set will not update until the next load if this isn't handled properly (be sure to test if changing)
                RaisePropertyChanged(() => Rank);
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
                
                foreach (ImageModel image in RelatedImages) image.VerifyIfRankValid();
            }
        } //? while this is enabled, independent images and the set will co-exist in the theme

        public bool InvalidSet { get; set; } = false;

        public ImageSetModel(ImageModel[] relatedImages, ImageType imageType, ImageSetType setType, ImageSetRankingFormat rankingFormat,
            int overrideRank, int overrideRankWeight, bool enabled = true)
        {
            base.ImageType = imageType;

            SetType = setType;

            RelatedImages = relatedImages;

            Enabled = enabled;

            if (relatedImages.Length == 0) //? used to help clean up empty sets
            {
                InvalidSet = true;
                return;
            }

            foreach (ImageModel relatedImage in RelatedImages)
            {
                relatedImage.ParentImageSet = this;

                if (relatedImage.ImageType != ImageType)
                {
                    RelatedImages[RelatedImages.IndexOf(relatedImage)] = null;
                    InvalidSet = true;
                    MessageBoxUtil.ShowError(ImageUtil.INVALID_IMAGE_SET_MESSAGE);
                    return;
                }
            }

            OnIsSelectedChanged += (value) =>
            {
                foreach (ImageModel image in RelatedImages)
                {
                    image.IsSelected = this.IsSelected;
                }
            };

            //! must be called after images have been set, added, removed, or re-ranked
            //! now handled by the below switch case due to setter functions
            //xUpdateAverageRankAndWeightedAverage();

            // TODO Make this enum format work with the XAML
            //? remember that setting one of these options to true automatically sets the rest to false with setter functions
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

            OverrideRank = overrideRank;

            OverrideRankWeight = overrideRankWeight;
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
                return RelatedImages;
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

        public int GetAverageRank()
        {
            ImageModel[] filteredRelatedImages = GetRelatedImages(!JsonUtil.IsLoadingData);

            int rankSum = 0;

            foreach (ImageModel image in filteredRelatedImages)
            {
                rankSum += image.Rank;
            }

            return (int)Math.Round((double)rankSum / RelatedImages.Length);
        }

        public int GetWeightedRank()
        {
            float weightRatio = (float)OverrideRankWeight / 100;

            float weightedAverage = AverageRank * (1 - weightRatio);

            float weightedOverride = OverrideRank * weightRatio;

            return (int)Math.Round(weightedAverage + weightedOverride);
        }

        public void UpdateAverageRankAndWeightedAverage()
        {
            //! These are only updated when a rank is adjusted to reduce looping
            if (RankingFormat == ImageSetRankingFormat.Average) AverageRank = GetAverageRank();

            if (RankingFormat == ImageSetRankingFormat.WeightedAverage) UpdateWeightedAverage();
        }

        public void UpdateWeightedAverage()
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
                WeightedAverage = 0; // all images are of rank 0
                return;
            }

            WeightedAverage = (int)Math.Round(weightNumerator / weightDivisor);
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
