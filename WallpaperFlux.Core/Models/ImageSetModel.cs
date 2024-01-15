using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LanceTools;
using LanceTools.WPF.Adonis.Util;
using MvvmCross.Commands;
using WallpaperFlux.Core.Util;

namespace WallpaperFlux.Core.Models
{
    public class ImageSetModel : BaseImageModel
    {
        public readonly ImageModel[] RelatedImages;

        public override bool IsImageSet => true;

        public RelatedImageType RelatedImageType { get; set; } = RelatedImageType.None;

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
                    UsingOverrideRank = false;
                    UsingWeightedRank = false;
                    UsingWeightedAverage = false;
                    UpdateAverageRankAndWeightedAverage();

                    RankingFormat = ImageSetRankingFormat.Average;
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
                    UsingAverageRank = false;
                    UsingWeightedRank = false;
                    UsingWeightedAverage = false;
                    RaisePropertyChanged(() => Rank);

                    RankingFormat = ImageSetRankingFormat.Override;
                }
            }
        }

        private bool _usingWeightedRank;
        public bool UsingWeightedRank // combines average rank & override rank using a weight slider
        {
            get => _usingWeightedRank;
            set
            {
                SetProperty(ref _usingWeightedRank, value);
                
                if (value)
                {
                    UsingAverageRank = false;
                    UsingOverrideRank = false;
                    UsingWeightedAverage = false;

                    RaisePropertyChanged(() => WeightedRank);
                    RaisePropertyChanged(() => Rank);

                    RankingFormat = ImageSetRankingFormat.WeightedOverride;
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
                    UsingAverageRank = false;
                    UsingOverrideRank = false;
                    UsingWeightedRank = false;
                    UpdateWeightedAverage();

                    RankingFormat = ImageSetRankingFormat.WeightedAverage;
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
                SetProperty(ref _overrideRank, value);
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

                RaisePropertyChanged(() => OverrideRankWeightText);
                RaisePropertyChanged(() => Rank);
            }
        }

        private int _weightedAverage;
        public int WeightedAverage
        {
            get => _weightedAverage;
            private set
            {
                SetProperty(ref _weightedAverage, value);
                RaisePropertyChanged(() => Rank);
            }
        }

        public string OverrideRankWeightText => "Weight: " + OverrideRankWeight;

        public bool InvalidSet { get; set; } = false;

        public IMvxCommand SetWallpaperCommand { get; set; }

        public ImageSetModel(ImageModel[] relatedImages, ImageType imageType, RelatedImageType relatedImageType, ImageSetRankingFormat rankingFormat,
            int overrideRank, int overrideRankWeight, bool enabled = true)
        {
            base.ImageType = imageType;

            RelatedImageType = relatedImageType;

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
                    UsingWeightedRank = true;
                    break;
            }

            OverrideRank = overrideRank;

            OverrideRankWeight = overrideRankWeight;

            SetWallpaperCommand = new MvxCommand(() => ImageUtil.SetWallpaper(this));
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

        //! Kept as a reminder that this is not supported at the moment
        public void SetRelatedImages(ImageModel[] newRelatedImages, ImageType imageType)
        {
            throw new Exception("We will only set this once, if you wish to add/remove from the Image set, create a new RelatedImageModel");
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
            if (UsingAverageRank)
            {
                AverageRank = GetAverageRank();
            }

            if (UsingWeightedAverage) // relevant to be called whenever average rank is updated
            {
                UpdateWeightedAverage();
            }
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

        protected bool Equals(ImageSetModel other) => RelatedImages.Equals(other.RelatedImages);

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ImageSetModel)obj);
        }

        public override int GetHashCode() => (RelatedImages != null ? RelatedImages.GetHashCode() : 0);
    }
}
