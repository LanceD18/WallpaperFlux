using System;
using System.Collections.Generic;
using System.Text;
using MvvmCross.ViewModels;
using WallpaperFlux.Core.Util;

namespace WallpaperFlux.Core.Models.Tagging
{
    public class CategoryModel : MvxNotifyPropertyChanged
    {
        private MvxObservableCollection<TagModel> _tags = new MvxObservableCollection<TagModel>();

        public MvxObservableCollection<TagModel> Tags
        {
            get => _tags;
            set => SetProperty(ref _tags, value);
        }

        private string _name;
        public string Name
        {
            get => _name;

            set
            {
                /*x
                if (Tags != null)
                {
                    HashSet<string> alteredImages = new HashSet<string>();

                    foreach (TagData tag in Tags)
                    {
                        tag.ParentCategoryName = value;

                        foreach (string image in tag.GetLinkedImages())
                        {
                            //? while the HashSet itself prevents duplicates, this contains reference is also done fastest through HashSet
                            //? which the rename category section needs
                            if (!alteredImages.Contains(image))
                            {
                                WallpaperData.GetImageData(image).RenameCategory(name, value);
                                alteredImages.Add(image);
                            }
                        }
                    }
                }
                */

                _name = value;
            }
        }

        private bool _enabled;
        public bool Enabled
        {
            get => _enabled;

            set
            {
                _enabled = value;
                // TODO Hopefully you won't need all the extra code below and stuff can be handled more dynamically
                /*x
                if (_Enabled != value) // prevents unnecessary calls
                {
                    _Enabled = value;

                    foreach (TagData tag in Tags)
                    {
                        if (!WallpaperData.IsLoadingData)
                        {
                            WallpaperData.EvaluateImageActiveStates(tag.GetLinkedImages(), !value); // will forceDisable if the value is set to false
                        }
                    }
                }
                */
            }
        }

        private bool _useForNaming;
        public bool UseForNaming
        {
            get => _useForNaming;

            set
            {
                _useForNaming = value;
                /*x
                if (_UseForNaming != value) // prevents unnecessary calls | and yes this can happen
                {
                    _UseForNaming = value;

                    HashSet<WallpaperData.ImageData> imagesToRename = new HashSet<WallpaperData.ImageData>();
                    foreach (TagData tag in Tags)
                    {
                        foreach (string imagePath in tag.GetLinkedImages())
                        {
                            imagesToRename.Add(WallpaperData.GetImageData(imagePath));
                        }
                    }
                }
                */
            }
        }

        public float Frequency { get; set; }

        // UI Bounds
        public float TagWrapWidth { get; set; } = TaggingUtil.TAGGING_WINDOW_WIDTH - 100;
        public float TagWrapHeight { get; set; } = TaggingUtil.TAGGING_WINDOW_HEIGHT - 50;

        public CategoryModel(string name)
        {
            Name = name;
        }

        public static bool operator ==(CategoryModel category1, CategoryModel category2)
        {
            return category1?.Name == category2?.Name;
        }

        public static bool operator !=(CategoryModel category1, CategoryModel category2)
        {
            return category1?.Name != category2?.Name;
        }
    }
}
