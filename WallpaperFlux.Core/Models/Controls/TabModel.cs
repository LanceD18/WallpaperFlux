using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MvvmCross.ViewModels;
using WallpaperFlux.Core.Util;

namespace WallpaperFlux.Core.Models.Controls
{
    public abstract class TabModel<T> : MvxNotifyPropertyChanged
    {
        private MvxObservableCollection<T> _items = new MvxObservableCollection<T>();

        public MvxObservableCollection<T> Items
        {
            get => _items;
            set => SetProperty(ref _items, value);
        }

        #region XAML

        public string TabIndex { get; set; } // allows us to visually display this tab's index

        #endregion

        protected TabModel(int index)
        {
            TabIndex = index.ToString();
            Items.CollectionChanged += (sender, args) => ControlUtil.VerifyListBoxCollectionChange(args, Items);
        }
    }
}
