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
        private ImageModel _selectedImage;
        public ImageModel SelectedImage
        {
            get
            {
                if (_selectedImage != null && !_selectedImage.IsSelected)
                {
                    SelectedImage = null; //? Deselecting the image won't ensure that this is nullified
                }

                return _selectedImage;
            }

            set
            {
                SetProperty(ref _selectedImage, value);

                // no need to do any of this if it's not the active tab (Which can cause delays on large selections)
                if (WallpaperFluxViewModel.Instance.SelectedImageSelectorTab == this)
                {
                    WallpaperFluxViewModel.Instance.RaisePropertyChanged(() => WallpaperFluxViewModel.Instance.SelectedImage);
                    WallpaperFluxViewModel.Instance.RaisePropertyChanged(() => WallpaperFluxViewModel.Instance.SelectedImagePathText);
                    WallpaperFluxViewModel.Instance.RaisePropertyChanged(() => WallpaperFluxViewModel.Instance.SelectedImageDimensionsText);
                    WallpaperFluxViewModel.Instance.RaisePropertyChanged(() => WallpaperFluxViewModel.Instance.IsImageSelected);
                    WallpaperFluxViewModel.Instance.RaisePropertyChanged(() => WallpaperFluxViewModel.Instance.InspectedImageTags);
                    
                    if (value != null) // allows us to see what tags this image has if the TagView is open
                    {
                        TaggingUtil.HighlightTags();
                    }

                    WallpaperFluxViewModel.Instance.MuteIfInspectorHasAudio();
                }
            }
        }

        public ImageSelectorTabModel(int index) : base(index) { }

        //? Set through the View.xaml.cs
        public double ImageSelectorTabWrapWidth { get; set; }

        //x//? The items won't on time without this (tags won't be properyl added)
        //xpublic void RaisePropertyChangedImages() => RaisePropertyChanged(() => Items);

        public ImageModel[] GetSelectedItems() => Items.Where(f => f.IsSelected).ToArray();

        public ImageModel[] GetAllItems() => Items.ToArray();

        public void SelectAllItems()
        {
            // so that methods such as DeselectItems() can function as intended to images being selected in tabs that are not visible
            if (SelectedImage == null) SelectedImage = Items[0];

            foreach (ImageModel image in Items)
            {
                image.IsSelected = true;
            }
        }

        public void DeselectAllItems()
        {
            if (SelectedImage != null) // if this is null, then no images have been selected here so we have no need to deselect
            {
                foreach (ImageModel image in Items)
                {
                    image.IsSelected = false;
                }
            }

            //xSelectedImage = null;
        }

        public void RemoveImage(ImageModel image) => RemoveImageRange(new ImageModel[] { image });

        public void RemoveImageRange(ImageModel[] images) => Items.RemoveItems(images);

        /// <summary>
        /// Checks for potentially deleted images and removes them accordingly
        /// </summary>
        public void VerifyImages()
        {
            for (var i = 0; i < Items.Count; i++)
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
