using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using MvvmCross.ViewModels;
using WallpaperFlux.Core.Util;
using WallpaperFlux.Core.ViewModels;

namespace WallpaperFlux.Core.Models.Controls
{
    //? Represents a tab within the Image Selector
    public class ImageSelectorTabModel : TabModel<ImageModel>, ITabModel<ImageModel>
    {
        public ImageSelectorTabModel(int index) : base(index) { }

        //x//? The items won't on time without this (tags won't be properyl added)
        //xpublic void RaisePropertyChangedImages() => RaisePropertyChanged(() => Items);

        public ImageModel[] GetSelectedItems() => Items.Where(f => f.IsSelected).ToArray();

        public ImageModel[] GetAllItems() => Items.ToArray();

        public void SelectAllItems()
        {
            // so that methods such as DeselectItems() can function as intended to images being selected in tabs that are not visible
            if (WallpaperFluxViewModel.Instance.SelectedImage == null) WallpaperFluxViewModel.Instance.SelectedImage = Items[0];

            foreach (ImageModel image in Items)
            {
                image.IsSelected = true;
            }

            //! TaggingUtil.HighlightTags(); [THIS IS HANDLED IN WallpaperFluxViewModel!!!]
        }

        public void DeselectAllItems()
        {
            if (WallpaperFluxViewModel.Instance.SelectedImage != null) // if this is null, then no images have been selected here so we have no need to deselect
            {
                foreach (ImageModel image in Items)
                {
                    image.IsSelected = false;
                }
            }

            //! TaggingUtil.HighlightTags(); [THIS IS HANDLED IN WallpaperFluxViewModel!!!]
        }

        public void RemoveImage(ImageModel image) => RemoveImageRange(new ImageModel[] { image });

        public void RemoveImageRange(ImageModel[] images)
        {
            foreach (ImageModel image in images)
            {
                image.IsSelected = false;
            }

            Items.RemoveItems(images);
        }

        /// <summary>
        /// Checks for potentially deleted images and removes them accordingly
        /// </summary>
        public void VerifyImages()
        {
            for (int i = 0; i < Items.Count; i++)
            {
                ImageModel image = Items[i];
                if (!ThemeUtil.Theme.Images.ContainsImage(image))
                {
                    Items.RemoveAt(i);
                    i--;
                }
            }
        }
    }
}
