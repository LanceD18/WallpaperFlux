using System;
using System.Collections.Generic;
using System.Text;
using MvvmCross.ViewModels;

namespace WallpaperFlux.Core.Models
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


        public void RaisePropertyChangedImages()
        {
            RaisePropertyChanged(() => Images);
        }
    }
}
