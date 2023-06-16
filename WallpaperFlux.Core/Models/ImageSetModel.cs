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

        private bool _usingAverageRank = true;
        public bool UsingAverageRank
        {
            get => _usingAverageRank;
            set
            {
                SetProperty(ref _usingAverageRank, value);

                if (value) UsingOverrideRank = false;
                if (value) UsingWeightedRank = false;
                if (value) UpdateAverageRank();
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
                if (value)
                {
                    RaisePropertyChanged(() => WeightedRank);
                    RaisePropertyChanged(() => Rank);
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

        public string OverrideRankWeightText => "Weight: " + OverrideRankWeight;

        public bool InvalidSet { get; set; } = false;

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

            UpdateAverageRank(); //! must be called after images have been set, added, removed, or re-ranked
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

        public void UpdateAverageRank() => AverageRank = GetAverageRank();

        //! Kept as a reminder that this is not supported at the moment
        public void SetRelatedImages(ImageModel[] newRelatedImages, ImageType imageType)
        {
            throw new Exception("We will only set this once, if you wish to add/remove from the Image set, create a new RelatedImageModel");
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
