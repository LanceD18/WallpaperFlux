using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Newtonsoft.Json;
using WallpaperFlux.Core.Collections;

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

        [JsonIgnore] public HashSet<ImageModel> LinkedImages; //? should get implemented on loading in the images through their TagCollection

        public TagModel(string name)
        {
            Name = name;
        }

        public void LinkImage(TagCollection tagLinker)
        {
            LinkedImages.Add(tagLinker.ParentImage);
        }

        public void UnlinkImage(TagCollection tagLinker)
        {
            LinkedImages.Remove(tagLinker.ParentImage);
        }

        public int GetLinkedImageCount()
        {
            Debug.WriteLine("Find an efficient way to get the number of images linked to a tag without having multiple references like in the previous TagData vs ImageData");
            return 0;
        }
    }
}
