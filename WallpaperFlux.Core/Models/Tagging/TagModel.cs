using System;
using System.Collections.Generic;
using System.Text;

namespace WallpaperFlux.Core.Models.Tagging
{
    public class TagModel
    {
        public string Name { get; private set; }

        private bool _enabled;
        public bool Enabled
        {
            get => _enabled;

            set
            {
                _enabled = value;
                /*x

                if (_enabled != value)  // prevents unnecessary calls
                {
                    _enabled = value;

                    if (LinkedImages != null)
                    {
                        if (!WallpaperData.IsLoadingData)
                        {
                            WallpaperData.EvaluateImageActiveStates(LinkedImages.ToArray(), !value); // will forceDisable if the value is set to false
                        }
                    }
                }
                */
            }
        }

        private bool _UseForNaming;
        public bool UseForNaming
        {
            get => _UseForNaming;

            set
            {
                _UseForNaming = value;
                /*x
                if (_UseForNaming != value)  // prevents unnecessary calls
                {
                    _UseForNaming = value;

                    if (LinkedImages != null)
                    {
                        HashSet<WallpaperData.ImageData> imagesToRename = new HashSet<WallpaperData.ImageData>();
                        foreach (string imagePath in GetLinkedImages())
                        {
                            imagesToRename.Add(WallpaperData.GetImageData(imagePath));
                        }
                    }
                }
                */
            }
        }

        public HashSet<Tuple<string, string>> ParentTags;
        public HashSet<Tuple<string, string>> ChildTags;

        private string parentCategoryName;
        
        public string ParentCategoryName
        {
            get => parentCategoryName;

            set
            {
                /*x
                if (parentCategoryName != "")
                {
                    UpdateLinkedTagsCategoryName(value);
                }
                */

                parentCategoryName = value;
            }
        }

        public TagModel(string name)
        {
            Name = name;
        }
    }
}
