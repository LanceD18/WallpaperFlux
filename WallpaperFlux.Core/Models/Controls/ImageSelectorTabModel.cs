using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using MvvmCross.ViewModels;
using WallpaperFlux.Core.ViewModels;

namespace WallpaperFlux.Core.Models.Controls
{
    //? Represents a tab within the Image Selector
    public class ImageSelectorTabModel : MvxNotifyPropertyChanged
    {
        public string TabIndex { get; set; }

        private MvxObservableCollection<ImageModel> _images = new MvxObservableCollection<ImageModel>();

        public MvxObservableCollection<ImageModel> Images
        {
            get => _images;
            set => SetProperty(ref _images, value);
        }

        private ImageModel _selectedImage;

        public ImageModel SelectedImage
        {
            get => _selectedImage;
            set
            {
                SetProperty(ref _selectedImage, value);
                TagViewModel.Instance.HighlightTags(value.Tags);
            }
        }

        //? Set through the View.xaml.cs
        public double ImageSelectorTabWrapWidth { get; set; }

        public ImageSelectorTabModel()
        {
            Images.CollectionChanged += OnImagesOnCollectionChanged_RemoveInvalidItems;
        }

        public void RaisePropertyChangedImages() => RaisePropertyChanged(() => Images);

        private void OnImagesOnCollectionChanged_RemoveInvalidItems(object sender, NotifyCollectionChangedEventArgs args)
        {
            if (args.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (ImageModel item in args.NewItems)
                {
                    if (item == null)
                    {
                        Debug.WriteLine("Invalid item found, removing");
                        Images.Remove(item);
                    }
                }
            }
        }

        public ImageModel[] GetHighlightedSelectedImages() => Images.Where(f => f.IsSelected).ToArray();

        public ImageModel[] GetAllSelectedImages() => Images.ToArray();
    }
}
