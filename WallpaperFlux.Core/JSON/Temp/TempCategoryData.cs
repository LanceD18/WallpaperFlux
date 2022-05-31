using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace WallpaperFlux.Core.JSON.Temp
{
    public class TempCategoryData
    {
        private string name;
        public string Name
        {
            get => name;

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

                name = value;
            }
        }

        private bool _Enabled;
        public bool Enabled
        {
            get => _Enabled;

            set
            {
                if (_Enabled != value) // prevents unnecessary calls
                {
                    _Enabled = value;

                    /*
                    foreach (TagData tag in Tags)
                    {
                        if (!WallpaperData.IsLoadingData)
                        {
                            WallpaperData.EvaluateImageActiveStates(tag.GetLinkedImages(), !value); // will forceDisable if the value is set to false
                        }
                    }
                    */
                }
            }
        }

        private bool _UseForNaming;
        public bool UseForNaming
        {
            get => _UseForNaming;

            set
            {
                if (_UseForNaming != value) // prevents unnecessary calls | and yes this can happen
                {
                    _UseForNaming = value;

                    /*
                    HashSet<WallpaperData.ImageData> imagesToRename = new HashSet<WallpaperData.ImageData>();
                    foreach (TagData tag in Tags)
                    {
                        foreach (string imagePath in tag.GetLinkedImages())
                        {
                            imagesToRename.Add(WallpaperData.GetImageData(imagePath));
                        }
                    }
                    */
                }
            }
        }

        public HashSet<TempTagData> Tags;

        [JsonIgnore]
        public bool Initialized { get; private set; }

        public TempCategoryData(string name, HashSet<TempTagData> tags = null, bool enabled = true, bool useForNaming = true)
        {
            Tags = tags ?? new HashSet<TempTagData>(); //! must be placed first to avoid getter setter errors (ex: enabled's setter)
            
            Name = name;
            Enabled = enabled;
            UseForNaming = useForNaming;
            Initialized = false;
        }

        /*x
        public void Initialize(bool initializeImages)
        {
            Initialized = true;
            WallpaperData.TaggingInfo.AddCategory(this);

            foreach (TagData tag in Tags)
            {
                tag.Initialize(name, initializeImages);
            }
        }

        public bool ContainsTag(TagData tag)
        {
            return ContainsTag(tag.Name);
        }

        public bool ContainsTag(string tagName)
        {
            return GetTag(tagName) != null;
        }

        public TagData GetTag(string tagName)
        {
            foreach (TagData curTag in Tags)
            {
                if (tagName == curTag.Name)
                {
                    return curTag;
                }
            }

            return null;
        }

        public static bool operator ==(TempCategoryData category1, TempCategoryData category2)
        {
            return category1?.Name == category2?.Name;
        }

        public static bool operator !=(TempCategoryData category1, TempCategoryData category2)
        {
            return category1?.Name != category2?.Name;
        }
        */
    }

}
