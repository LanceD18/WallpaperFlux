﻿using System;
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
