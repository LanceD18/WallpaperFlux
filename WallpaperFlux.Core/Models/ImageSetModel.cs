using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LanceTools;
using LanceTools.WPF.Adonis.Util;
using WallpaperFlux.Core.Util;

namespace WallpaperFlux.Core.Models
{
    public class ImageSetModel : BaseImageModel
    {
        public ImageModel[] RelatedImages;

        public override bool IsRelatedImageSet => true;

        public RelatedImageType RelatedImageType { get; set; } = RelatedImageType.None;

        private bool _usingAverageRank;
        public bool UsingAverageRank
        {
            get => _usingAverageRank;
            set
            {
                SetProperty(ref _usingAverageRank, value);

                if (value) UsingOverrideRank = false;
                if (value) UsingWeightedRank = false;
                if (value) UsingWeightedAverage = false;
                if (value) UpdateAverageRankAndWeightedAverage();
            }
        }

        private bool _usingOverrideRank;
        public bool UsingOverrideRank
        {
            get => _usingOverrideRank;
            set
            {
                SetProperty(ref _usingOverrideRank, value);

                if (value) UsingAverageRank = false;
                if (value) UsingWeightedRank = false;
                if (value) UsingWeightedAverage = false;
                if (value) RaisePropertyChanged(() => Rank);
            }
        }

        private bool _usingWeightedRank;
        public bool UsingWeightedRank // combines average rank & override rank using a weight slider
        {
            get => _usingWeightedRank;
            set
            {
                SetProperty(ref _usingWeightedRank, value);

                if (value) UsingAverageRank = false;
                if (value) UsingOverrideRank = false;
                if (value) UsingWeightedAverage = false;
                if (value)
                {
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

                if (value) UsingAverageRank = false;
                if (value) UsingOverrideRank = false;
                if (value) UsingWeightedRank = false;
                if (value)
                {
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

        public string RankText => "Rank: " + Rank;

        public ImageSetModel(ImageModel[] relatedImages, ImageType imageType, bool enabled = true)
        {
            base.ImageType = imageType;

            RelatedImages = relatedImages;

            Enabled = enabled;

            if (relatedImages.Length == 0) //? used to help clean up empty sets
            {
                InvalidSet = true;
                return;
            }

            foreach (ImageModel relatedImage in RelatedImages)
            {
                relatedImage.ParentRelatedImageModel = this;

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

            UpdateAverageRankAndWeightedAverage(); //! must be called after images have been set, added, removed, or re-ranked
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
            int rankSum = 0; 

            foreach (ImageModel image in RelatedImages)
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
            //? follows how the Rank Percentiles function
            // TODO consider merging the logic of this and the PercentileController into a tool

            // Gather Weights
            int maxRank = ThemeUtil.Theme.RankController.GetMaxRank();

            double[] weights = new double[RelatedImages.Length];
            double rankMultiplier = 10.0 / maxRank;

            double weightNumerator = 0;
            double weightDivisor = 0;
            for (int i = 0; i < RelatedImages.Length; i++)
            {
                // if the rank of an image is 0 just set the weight to 0, calculating it will give us a weight of 1
                weights[i] = RelatedImages[i].Rank != 0 ? Math.Pow(2, RelatedImages[i].Rank * rankMultiplier) : 0;

                weightNumerator += RelatedImages[i].Rank * weights[i];
                weightDivisor += weights[i];
            }

            WeightedAverage = (int)Math.Round(weightNumerator / weightDivisor);
        }

        public ImageModel GetHighestRankedImage()
        {
            // returns the highest ranked image in the set
            // if multiple images have the highest rank, we'll just return whichever one comes first

            int rank = -1;
            ImageModel highestRankedImage = null;

            foreach (ImageModel image in RelatedImages)
            {
                if (image.Rank > rank)
                {
                    highestRankedImage = image;
                }
            }

            return highestRankedImage;
        }

        protected bool Equals(ImageSetModel other)
        {
            bool equal = true;

            if (RelatedImages.Length == other.RelatedImages.Length)
            {
                for (int i = 0; i < RelatedImages.Length; i++)
                {
                    if (!RelatedImages[i].Equals(other.RelatedImages[i]))
                    {
                        equal = false;
                    }
                }
            }
            else
            {
                equal = false;
            }

            return equal && _rank == other._rank;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ImageSetModel)obj);
        }
    }
}
