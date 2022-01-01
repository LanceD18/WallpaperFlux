using System.Collections.Specialized;
using System.Diagnostics;
using MvvmCross.ViewModels;

namespace WallpaperFlux.Core.Models.Controls
{
    public class ImageSelectorTabModel : MvxNotifyPropertyChanged
    {
        public string TabIndex { get; set; }

        private MvxObservableCollection<ImageModel> _images = new MvxObservableCollection<ImageModel>();

        public MvxObservableCollection<ImageModel> Images
        {
            get => _images;
            set => SetProperty(ref _images, value);
        }

        private ImageModel _selectedImageSelectorImage;

        public ImageModel SelectedImageSelectorImage
        {
            get => _selectedImageSelectorImage;
            set
            {
                SetProperty(ref _selectedImageSelectorImage, value);
                RaisePropertyChanged(() => SelectedImageSelectorImagePath);
            }
        }

        public string SelectedImageSelectorImagePath => SelectedImageSelectorImage?.Path;

        public ImageSelectorTabModel()
        {
            Images.CollectionChanged += (sender, args) =>
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
            };
        }

        public void RaisePropertyChangedImages()
        {
            RaisePropertyChanged(() => Images);
        }
    }
}
