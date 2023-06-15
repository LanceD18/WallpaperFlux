using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using Newtonsoft.Json;
using WallpaperFlux.Core.Models.Controls;
using WallpaperFlux.Core.Util;

namespace WallpaperFlux.Core.Models
{
    public abstract class BaseImageModel : ListBoxItemModel
    {
        public virtual bool IsVideo => false;

        public virtual bool IsRelatedImageSet => false;

        [DataMember(Name = "Enabled")]
        protected bool _enabled = true;


        public virtual bool Enabled // this is the image's individual enabled state, if this is false then nothing else can make the image active
        {
            get => _enabled;

            set => SetProperty(ref _enabled, value);
        }

        public bool Active { get; protected set; } //? so that we don't have to check IsEnabled() every time we want to see if the image is available

        [DataMember(Name = "Image Type")]
        public ImageType ImageType { get; set; }

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

        public virtual bool IsEnabled()
        {
            Active = false; // recheck this every time IsEnabled is called

            if (JsonUtil.IsLoadingData) return false;

            if (!Enabled) return false;

            Active = true; // if we reach this point, then the image is in fact Active
            return true;
        }
    }
}
