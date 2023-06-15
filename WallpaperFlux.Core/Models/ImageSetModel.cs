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

        public bool UsingAverageRank { get; set; } = true;

        public int AverageRank { get; set; }

        public int OverrideRank { get; set; }

        public bool InvalidSet { get; } = false;

        public ImageSetModel(ImageModel[] relatedImages, ImageType imageType)
        {
            base.ImageType = imageType;

            RelatedImages = relatedImages;

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
        }

        public void SetRelatedImages(ImageModel[] newRelatedImages, ImageType imageType)
        {
            throw new Exception("We will only set this once, if you wish to change the Image set, create a new RelatedImageModel");
        }
    }
}
