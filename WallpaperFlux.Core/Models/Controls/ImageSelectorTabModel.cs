using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MvvmCross.ViewModels;
using WallpaperFlux.Core.Util;
using WallpaperFlux.Core.ViewModels;

namespace WallpaperFlux.Core.Models.Controls
{
    //? Represents a tab within the Image Selector
    public class ImageSelectorTabModel : TabModel<BaseImageModel>, ITabModel<BaseImageModel>
    {
        private BaseImageModel _selectedImage;
        public BaseImageModel SelectedImage
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
                if (WallpaperFluxViewModel.Instance.ImageSetInspectorToggle)
                {
                    return; //? cannot change the selected state of images not in an image set while the image set is viewable
                }

                SetProperty(ref _selectedImage, value);

                // no need to do any of this if it's not the active tab (Which can cause delays on large selections)
                if (WallpaperFluxViewModel.Instance.SelectedImageSelectorTab == this)
                {
                    WallpaperFluxViewModel.Instance.SelectedImage = value; //! do not change this in WallpaperFluxViewModel to allow for changes from other sources
                }
            }
        }

        public HashSet<ImageModel> ImageItems = new HashSet<ImageModel>();

        public HashSet<ImageSetModel> ImageSetItems = new HashSet<ImageSetModel>();

        private Thread VerifyImageThread;

        public ImageSelectorTabModel(int index) : base(index)
        {
            Items.CollectionChanged += ItemsOnCollectionChanged;
        }

        private void ItemsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // automates addition and removal of images and image sets so that we can have split collections (useful for more efficient searching)
            SplitImagesAndSets(e);

            if (VerifyImageThread == null || (VerifyImageThread != null && !VerifyImageThread.IsAlive))
            {
                VerifyImageThread = new Thread(() =>
                {
                    Thread.Sleep(1000);
                    VerifyImages();
                });

                VerifyImageThread.Start();
            }
        }

        private void SplitImagesAndSets(NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems)
                {
                    switch (item)
                    {
                        case ImageModel imageModel:
                            ImageItems.Remove(imageModel);
                            break;

                        case ImageSetModel imageSet:
                            ImageSetItems.Remove(imageSet);
                            break;
                    }
                }
            }

            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems)
                {
                    switch (item)
                    {
                        case ImageModel imageModel:
                            ImageItems.Add(imageModel);
                            break;

                        case ImageSetModel imageSet:
                            ImageSetItems.Add(imageSet);
                            break;
                    }
                }
            }
        }

        //? Set through the View.xaml.cs
        public double ImageSelectorTabWrapWidth { get; set; }

        //x//? The items won't on time without this (tags won't be properyl added)
        //xpublic void RaisePropertyChangedImages() => RaisePropertyChanged(() => Items);

        public BaseImageModel[] GetSelectedItems() => Items.Where(f => f.IsSelected).ToArray();

        public ImageModel[] GetSelectedImages() => ImageItems.Where(f => f.IsSelected).ToArray();

        public ImageSetModel[] GetSelectedSets() => ImageSetItems.Where(f => f.IsSelected).ToArray();

        public BaseImageModel[] GetAllItems() => Items.ToArray();

        public ImageModel[] GetAllImages() => ImageItems.ToArray();

        public ImageSetModel[] GetAllSets() => ImageSetItems.ToArray();

        public void SelectAllItems()
        {
            // so that methods such as DeselectItems() can function as intended to images being selected in tabs that are not visible
            if (SelectedImage == null) SelectedImage = Items[0];

            foreach (BaseImageModel image in Items)
            {
                image.IsSelected = true;
            }

            //! TaggingUtil.HighlightTags(); [THIS IS HANDLED IN WallpaperFluxViewModel!!!]
        }

        public void DeselectAllItems()
        {
            if (SelectedImage != null) // if SelectedImage is null, then no images have been selected here so we have no need to deselect
            {
                foreach (BaseImageModel image in Items)
                {
                    image.IsSelected = false;
                }
            }

            //! TaggingUtil.HighlightTags(); [THIS IS HANDLED IN WallpaperFluxViewModel!!!]
        }

        public void AddImage(BaseImageModel image) => AddImageRange(new BaseImageModel[] { image });

        public void AddImageRange(BaseImageModel[] images)
        {
            //! Range actions are not supported by MvxObservableCollections!!!
            //! Range actions are not supported by MvxObservableCollections!!!
            //! Range actions are not supported by MvxObservableCollections!!! (odd)

            foreach (BaseImageModel image in images)
            {
                Items.Add(image);
            }
        }

        public void RemoveImage(BaseImageModel image) => RemoveImageRange(new BaseImageModel[] { image });

        public void RemoveImageRange(BaseImageModel[] images)
        {
            foreach (BaseImageModel image in images)
            {
                image.IsSelected = false;
            }

            Items.RemoveItems(images);
        }

        public void ReplaceImage(BaseImageModel oldImage, BaseImageModel newImage)
        {
             //? without this process the thumbnail of the image won't update
             int index = Items.IndexOf(oldImage);
             RemoveImage(oldImage);
             Items.Insert(index, newImage);
        }

        /// <summary>
        /// Checks for potentially deleted images and removes them accordingly
        /// </summary>
        private void VerifyImages()
        {
            List<BaseImageModel> imagesToRemove = new List<BaseImageModel>();
            for (var i = 0; i < Items.Count; i++)
            {
                BaseImageModel image = Items[i];
            
                if (!ThemeUtil.Theme.Images.ContainsImage(image))
                {
                    imagesToRemove.Add(image);
                }
            }

            if (imagesToRemove.Count > 0) //? only call this when the count is > 0, otherwise the tab will redraw each time this is called
            {
                Items.RemoveItems(imagesToRemove.ToArray());
            }
        }
    }
}
