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
            get => _selectedImage;
            set
            {
                SetProperty(ref _selectedImage, value);

                WallpaperFluxViewModel.Instance.RaisePropertyChanged(() => WallpaperFluxViewModel.Instance.SelectedImagePathText);
                WallpaperFluxViewModel.Instance.RaisePropertyChanged(() => WallpaperFluxViewModel.Instance.SelectedImageDimensionsText);

                if (value != null) // allows us to see what tags this image has if the TagView is open
                {
                    TaggingUtil.HighlightTags(value.Tags);
                }
            }
        }

        public ImageSelectorTabModel(int index) : base(index) { }

        //? Set through the View.xaml.cs
        public double ImageSelectorTabWrapWidth { get; set; }

        //? Not sure if this actually helps, *seems* to work just fine without this but there might have been some issue you forgot about
        public void RaisePropertyChangedImages() => RaisePropertyChanged(() => Items);

        public ImageModel[] GetSelectedItems() => Items.Where(f => f.IsSelected).ToArray();

        public ImageModel[] GetAllItems() => Items.ToArray();

        public void SelectAllItems()
        {
            // so that methods such as DeselectItems() can function as intended to images being selected in tabs that are not visible
            if (SelectedImage == null) SelectedImage = (ImageModel)Items[0];

            foreach (ImageModel image in Items)
            {
                image.IsSelected = true;
            }
        }

        public void DeselectAllItems()
        {
            if (SelectedImage != null)
            {
                foreach (ImageModel image in Items)
                {
                    image.IsSelected = false;
                }
            }

            SelectedImage = null;
        }
    }
}
