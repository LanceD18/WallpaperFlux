using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text;
using LanceTools;
using MvvmCross.Commands;
using Newtonsoft.Json;
using WallpaperFlux.Core.Models.Controls;
using WallpaperFlux.Core.Util;
using WallpaperFlux.Core.ViewModels;

namespace WallpaperFlux.Core.Models
{
    public abstract class BaseImageModel : ListBoxItemModel
    {
        public virtual bool IsVideo => false;

        public virtual bool IsImageSet => false;

        protected int _rank;
        public int Rank
        {
            get
            {
                if (this is ImageSetModel imageSet)
                {
                    int oldRank = _rank;

                    if (imageSet.RankingFormat == ImageSetRankingFormat.Average) _rank = imageSet.AverageRank;

                    if (imageSet.RankingFormat == ImageSetRankingFormat.Override) _rank = imageSet.OverrideRank;

                    if (imageSet.RankingFormat == ImageSetRankingFormat.WeightedOverride) _rank = imageSet.WeightedRank;

                    if (imageSet.RankingFormat == ImageSetRankingFormat.WeightedAverage) _rank = imageSet.WeightedAverage;

                    //! safety precaution ; if a calculation sends _rank above max rank or below 0 we will get a stack overflow as the old rank will always be bound to the rank range
                    _rank = MathE.Clamp(_rank, 0, ThemeUtil.RankController.GetMaxRank());

                    if (oldRank != _rank)
                    {
                        ThemeUtil.Theme.RankController.ModifyRank(this, oldRank, ref _rank, true);
                        WallpaperFluxViewModel.Instance.RaisePropertyChanged(() => WallpaperFluxViewModel.Instance.InspectedImageRankText);
                    }
                }

                return _rank;
            }

            set
            {
                switch (this)
                {
                    case ImageModel imageModel:

                        ThemeUtil.Theme.RankController.ModifyRank(this, _rank, ref value); //? this should be called first to allow the old rank to be identified

                        SetProperty(ref _rank, value);
                        RaisePropertyChanged(() => Rank);

                        if (imageModel.IsInImageSet) imageModel.ParentImageSet.UpdateAverageRankAndWeightedAverage();

                        break;

                    case ImageSetModel imageSet:

                        if (!imageSet.UsingAverageRank)
                        {
                            imageSet.OverrideRank = value;
                        }

                        break;
                }
            }
        }

        [DataMember(Name = "Enabled")]
        protected bool _enabled = true;

        public virtual bool Enabled // this is the image's individual configurable enabled state, if this is false then nothing else can make the image active
        {
            get => _enabled; //! This does NOT guarantee if an image is enabled or not, this represents the configurable Enabled SETTING that the user can TOGGLE, use IsEnabled() instead for that purpose

            set
            {
                SetProperty(ref _enabled, value);

                UpdateEnabledState();
            }
        }

        public bool Active { get; protected set; } = false; //? so that we don't have to check IsEnabled() every time we want to see if the image is available

        [DataMember(Name = "Image Type")]
        public ImageType ImageType { get; set; }


        // Video, Gif, & Animated Set Properties
        public bool IsAnimated
        {
            get
            {
                switch (this)
                {
                    case ImageModel image:
                        return image.IsVideoOrGif;

                    case ImageSetModel imageSet:
                        return imageSet.SetType == ImageSetType.Animate;
                }

                return false;
            }
        }

        #region Video / GIF / Animation Properties
        public double Speed { get; set; } = 1;

        public int MinimumLoops { get; set; }

        private bool _overrideMinimumLoops;
        public bool OverrideMinimumLoops
        {
            get => _overrideMinimumLoops;
            set => SetProperty(ref _overrideMinimumLoops, value);
        }

        public int MaximumTime { get; set; }

        private bool _overrideMaximumTime;
        public bool OverrideMaximumTime
        {
            get => _overrideMaximumTime;
            set => SetProperty(ref _overrideMaximumTime, value);
        }
        #endregion

        //! The following is intended for image sets
        /*x
        private ImageType _imageType;
        public ImageType ImageType
        {
            get => _imageType;
            set
            {
                if (!JsonUtil.IsLoadingData) //? if this is triggered while not loading,  we have changed the image type, ideally only related image sets should be capable of changing image type
                {
                    ThemeUtil.RankController.RemoveRankedImage(this, true); // we need to remove this and re-add it after changing the image type
                }

                _imageType = value;
                
                if (!JsonUtil.IsLoadingData) //? ideally only related image sets should be capable of changing image type
                {
                    ThemeUtil.RankController.AddRankedImage(this, true);
                }
            }
        }
        */

        // ----- XAML Values -----
        // TODO Replace this section with ResourceDictionaries at some point

        #region XAML Values
        [JsonIgnore] public int ImageSelectorSettingsHeight => 25;

        [JsonIgnore] public int ImageSelectorThumbnailHeight => 150;

        [JsonIgnore] public int ImageSelectorThumbnailWidth => 150;

        [JsonIgnore] public int ImageSelectorThumbnailWidthVideo => ImageSelectorThumbnailWidth - 20; // until the GroupBox is no longer needed this will account for it
        #endregion

        public IMvxCommand DecreaseRankCommand { get; set; }

        public IMvxCommand IncreaseRankCommand { get; set; }

        protected BaseImageModel()
        {
            DecreaseRankCommand = new MvxCommand(() =>
            {
                switch (this)
                {
                    case ImageModel imageModel:
                        imageModel.Rank--;

                        break;

                    case ImageSetModel imageSet:
                        if (imageSet.RankingFormat == ImageSetRankingFormat.Override || imageSet.RankingFormat == ImageSetRankingFormat.WeightedOverride)
                        {
                            imageSet.OverrideRank--;
                        }

                        break;
                }
            });

            IncreaseRankCommand = new MvxCommand(() =>
            {
                switch (this)
                {
                    case ImageModel imageModel:
                        imageModel.Rank++;

                        break;

                    case ImageSetModel imageSet:
                        if (imageSet.RankingFormat == ImageSetRankingFormat.Override || imageSet.RankingFormat == ImageSetRankingFormat.WeightedOverride)
                        {
                            imageSet.OverrideRank++;
                        }

                        break;
                }
            });
        }

        public virtual bool IsEnabled(bool ignoreSet = false)
        {
            Active = false; // recheck this every time IsEnabled is called

            if (JsonUtil.IsLoadingData) return false;

            if (!Enabled) return false;

            if (!ThemeUtil.Theme.Images.ContainsImage(this)) return false;

            Active = true; // if we reach this point, then the image is in fact Active
            return true;
        }

        public void UpdateEnabledState()
        {
            if (JsonUtil.IsLoadingData) return;

            //? Modifying the image's rank will both check if the image is enabled and add/remove the image as needed
            // ModifyRank will always have to check if the image is enabled or not and since this also determines if we remove/add the image we will perform the enabled state
            // check through ModifyRank instead of checking IsEnabled() directly
            ThemeUtil.RankController.ModifyRank(this, _rank, ref _rank);
        }

        protected virtual bool Equals(BaseImageModel other) => throw new NotImplementedException();

        public override bool Equals(object obj) => throw new NotImplementedException();

        public override int GetHashCode() => throw new NotImplementedException();
    }
}
