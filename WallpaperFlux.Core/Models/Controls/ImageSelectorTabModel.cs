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

        private List<BaseImageModel> _items = new List<BaseImageModel>();

        private HashSet<ImageModel> ImageItems = new HashSet<ImageModel>();

        private HashSet<ImageSetModel> ImageSetItems = new HashSet<ImageSetModel>();

        private Thread VerifyImageThread;

        public int ImageStartIndex { get; }

        //? Set through the View.xaml.cs
        public double ImageSelectorTabWrapWidth { get; set; }

        public ImageSelectorTabModel(int tabIndex, int imageStartIndex) : base(tabIndex)
        {
            ImageStartIndex = imageStartIndex;
            //x Items.CollectionChanged += ItemsOnCollectionChanged;
        }

        /*x
        private void ItemsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // automates addition and removal of images and image sets so that we can have split collections (useful for more efficient selections)
            SplitImagesAndSets(e);

            if (VerifyImageThread == null || !VerifyImageThread.IsAlive)
            {
                VerifyImageThread = new Thread(() =>
                {
                    Thread.Sleep(250); // ? delay implemented so that we will wait on all potential changes to the collection to be processed before scanning this
                    VerifyImages();
                });

                VerifyImageThread.Start();
            }
        }

        /*x
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
        */

        //x//? The items won't on time without this (tags won't be properyl added)
        //xpublic void RaisePropertyChangedImages() => RaisePropertyChanged(() => Items);

        /*x
        public BaseImageModel[] GetSelectedItems() => Items.Where(f => f.IsSelected).ToArray();

        //xpublic ImageModel[] GetSelectedImages() => ImageItems.Where(f => f.IsSelected).ToArray();
        public ImageModel[] GetSelectedImages() => Items.OfType<ImageModel>().Where(f => f.IsSelected).ToArray();

        //xpublic ImageSetModel[] GetSelectedSets() => ImageSetItems.Where(f => f.IsSelected).ToArray();
        public ImageSetModel[] GetSelectedSets() => Items.OfType<ImageSetModel>().Where(f => f.IsSelected).ToArray();

        public BaseImageModel[] GetAllItems() => Items.ToArray();

        //xpublic ImageModel[] GetAllImages() => ImageItems.ToArray();
        public ImageModel[] GetAllImages() => Items.OfType<ImageModel>().ToArray();

        //xpublic ImageSetModel[] GetAllSets() => ImageSetItems.ToArray();
        public ImageSetModel[] GetAllSets() => Items.OfType<ImageSetModel>().ToArray();

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

        public void AddImage(BaseImageModel image, int insertIndex) => AddImageRange(new BaseImageModel[] { image }, insertIndex);


        public void AddImageRange(BaseImageModel[] images, int insertIndex)
        {
            //! Range actions are not supported by MvxObservableCollections!!!
            //! Range actions are not supported by MvxObservableCollections!!!
            //! Range actions are not supported by MvxObservableCollections!!!

            List<BaseImageModel> allImages = Items.ToList();
            allImages.AddRange(images); //? we don't want to reapetedly call the add() command on the observable collection

            Items = new MvxObservableCollection<BaseImageModel>(allImages);
        }

        public void RemoveImage(BaseImageModel image) => RemoveImageRange(new BaseImageModel[] { image });

        public void RemoveImageRange(IEnumerable<BaseImageModel> images)
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
        */

        // TODO Apply the changed logic to other TabModels
        //? we only want to update the observable collection when the tab is being displayed
        public void UpdateTabDisplay()
        {
            if (ImageStartIndex >= WallpaperFluxViewModel.Instance.ImageSelectorItems.Count)
            {
                Items.Clear();
                return;
            }

            int pageSize = Math.Min(WallpaperFluxViewModel.IMAGES_PER_PAGE, WallpaperFluxViewModel.Instance.ImageSelectorItems.Count - ImageStartIndex);
            List<BaseImageModel> itemsThisPage = WallpaperFluxViewModel.Instance.ImageSelectorItems.GetRange(ImageStartIndex, pageSize);

            Items = new MvxObservableCollection<BaseImageModel>(itemsThisPage);
            VerifyImages();
        }

        /// <summary>
        /// Checks for potentially deleted images and removes them accordingly (in the event of externally deleted or invalid images)
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
