using System;
using System.Diagnostics;
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
            get
            {
                if (this is ImageModel imageModel)
                {
                    if (WallpaperFluxViewModel.Instance.ImageSetInspectorToggle && !imageModel.IsInImageSet)
                    {
                        Debug.WriteLine("Attempted to get selection of image not in an image set while viewing the image set inspector");
                        return false; // ! if attempt to gather the selection of an image that is not in an image set, return false

                    }
                }

                return _isSelected;
            }

            set
            {
                if (this is ImageModel imageModel)
                {
                    //xDebug.WriteLine($"Updating Selection of {imageModel.Path} [{value}] | Is In Image Set: {imageModel.IsInImageSet}");
                    if (WallpaperFluxViewModel.Instance.ImageSetInspectorToggle && !imageModel.IsInImageSet)
                    {
                        Debug.WriteLine("Attempted to set selection of image not in an image set while viewing the image set inspector");
                        return; //? cannot change the selected state of images not in an image set while the image set is viewable
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
