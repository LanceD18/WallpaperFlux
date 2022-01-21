using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MvvmCross.ViewModels;
using WallpaperFlux.Core.Models.Tagging;

namespace WallpaperFlux.Core.Models.Controls
{
    //? Represents a tab within the Tab View
    public class TagTabModel : MvxNotifyPropertyChanged
    {
        public string TabIndex { get; set; }

        public double TagWrapWidth { get; set; }

        public double TagWrapHeight { get; set; }

        private MvxObservableCollection<TagModel> _visibleTags = new MvxObservableCollection<TagModel>();

        public MvxObservableCollection<TagModel> VisibleTags
        {
            get => _visibleTags;
            set => SetProperty(ref _visibleTags, value);
        }
        
        public TagTabModel(int index)
        {
            TabIndex = index.ToString();
        }

        public void SetTagWrapSize(double width, double height)
        {
            TagWrapWidth = width;
            TagWrapHeight = height; // the bottom tends to be cut off
            RaisePropertyChanged(() => TagWrapWidth);
            RaisePropertyChanged(() => TagWrapHeight);
        }

        public TagModel[] GetSelectedVisibleTags() => VisibleTags.Where(f => f.IsSelected).ToArray();

        public TagModel[] GetAllVisibleTags() => VisibleTags.ToArray();
    }
}
