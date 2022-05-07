using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MvvmCross.ViewModels;
using WallpaperFlux.Core.Models.Tagging;
using WallpaperFlux.Core.Util;

namespace WallpaperFlux.Core.Models.Controls
{
    //? Represents a page (tab) within a categories' tag Tab View
    public class TagTabModel : TabModel<TagModel>, ITabModel<TagModel>
    {
        public double TagWrapWidth { get; set; }

        public double TagWrapHeight { get; set; }

        public TagTabModel(int index) : base(index) { }

        public void SetTagWrapSize(double width, double height)
        {
            TagWrapWidth = width;
            TagWrapHeight = height; // the bottom tends to be cut off
            RaisePropertyChanged(() => TagWrapWidth);
            RaisePropertyChanged(() => TagWrapHeight);
        }

        public TagModel[] GetSelectedItems() => Items.Where(f => f.IsSelected).ToArray();

        public TagModel[] GetAllItems() => Items.ToArray();

        public void SelectAllItems()
        {
            foreach (TagModel tag in Items)
            {
                tag.IsSelected = true;
            }
        }

        public void DeselectAllItems()
        {
            foreach (TagModel tag in Items)
            {
                tag.IsSelected = false;
            }
        }
    }
}
