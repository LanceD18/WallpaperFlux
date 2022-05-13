using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using MvvmCross.ViewModels;
using WallpaperFlux.Core.Models.Tagging;
using WallpaperFlux.Core.Util;
using WallpaperFlux.Core.ViewModels;

namespace WallpaperFlux.Core.Models.Controls
{
    //? Represents a page (tab) within a categories' tag Tab View
    public class TagTabModel : TabModel<TagModel>, ITabModel<TagModel>
    {
        private TagModel _selectedTag;
        public TagModel SelectedTag
        {
            get => _selectedTag;
            set
            {
                SetProperty(ref _selectedTag, value);
                TagViewModel.Instance.RaisePropertyChanged(() => TagViewModel.Instance.CanUseTagLinker);

                // The selected tag will become the linking source when the linker is turned on, but shouldn't be modified while it is on
                if (!TaggingUtil.GetTagLinkerToggle())
                {
                    TagViewModel.Instance.TagLinkingSource = value;
                    TagViewModel.Instance.RaisePropertyChanged(() => TagViewModel.Instance.TagLinkingSourceName);
                }
            }
        }

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
