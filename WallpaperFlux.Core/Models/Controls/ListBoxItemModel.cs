using System;
using MvvmCross.ViewModels;
using Newtonsoft.Json;

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
                OnIsSelectedChanged?.Invoke(value);
                SetProperty(ref _isSelected, value);
            }
        }

        protected Action<bool> OnIsSelectedChanged;
    }
}
