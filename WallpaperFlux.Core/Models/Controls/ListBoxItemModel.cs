using System;
using MvvmCross.ViewModels;
using Newtonsoft.Json;
using WallpaperFlux.Core.ViewModels;

namespace WallpaperFlux.Core.Models.Controls
{
    public class ListBoxItemModel : MvxNotifyPropertyChanged
    {
        private bool _isSelected;
        [JsonIgnore]
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (this is ImageModel imageModel)
                {
                    if (WallpaperFluxViewModel.Instance.ImageSetInspectorToggle)
                    {
                        if (!imageModel.IsInRelatedImageSet)
                        {
                            return; //? cannot change the selected state of images not in an image set while the image set is viewable
                        }
                    }

                }

                bool changed = _isSelected != value;

                SetProperty(ref _isSelected, value);
                if (changed) OnIsSelectedChanged?.Invoke(value);
            }
        }

        private bool _isHidden;
        [JsonIgnore]
        public bool IsHidden
        {
            get => _isHidden;
            set => SetProperty(ref _isHidden, value);
        }

        protected Action<bool> OnIsSelectedChanged;
    }
}
