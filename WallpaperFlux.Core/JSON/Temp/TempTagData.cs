using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using AdonisUI.Controls;
using Newtonsoft.Json;

namespace WallpaperFlux.Core.JSON.Temp
{

    public class TempTagData
    {
        public string Name { get; private set; }

        private bool _Enabled;
        public bool Enabled
        {
            get => _Enabled;

            set
            {
                /*x
                if (_Enabled != value)  // prevents unnecessary calls
                {
                    _Enabled = value;

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

        [JsonIgnore]
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

                parentCategoryName = value;
                */
            }
        }

        private HashSet<string> LinkedImages;

        [JsonIgnore]
        public bool IsInitialized { get; private set; }

        [JsonConstructor]
        public TempTagData(string name, TempCategoryData parentCategory = null,
            bool enabled = true, bool useForNaming = true, HashSet<string> linkedImages = null,
            HashSet<Tuple<string, string>> parentTags = null, HashSet<Tuple<string, string>> childTags = null)
        {
            Name = name;
            IsInitialized = false;

            //? This scenario only occurs when a tag has been added after loading, otherwise Enabled and UseForNaming will have already been set
            if (parentCategory != null) // Adding New Tag | New tags will have the default settings of the category
            {
                ParentCategoryName = parentCategory.Name;
                Enabled = parentCategory.Enabled;
                UseForNaming = parentCategory.UseForNaming;
            }
            else // Loading Tag
            {
                Enabled = enabled;
                UseForNaming = useForNaming;
            }

            if (linkedImages != null) // Adding New Tag
            {
                LinkedImages = linkedImages;
            }
            else // Loading Tag | These will be linked through the LoadCoreData method instead
            {
                LinkedImages = new HashSet<string>();
            }

            ParentTags = parentTags ?? new HashSet<Tuple<string, string>>();
            ChildTags = childTags ?? new HashSet<Tuple<string, string>>();
        }

        public TempTagData(TempTagData otherTag)
        {
            Name = otherTag.Name;
            IsInitialized = false;

            ParentCategoryName = otherTag.ParentCategoryName;
            Enabled = otherTag.Enabled;
            UseForNaming = otherTag.UseForNaming;

            LinkedImages = new HashSet<string>(otherTag.LinkedImages);
            ParentTags = new HashSet<Tuple<string, string>>(otherTag.ParentTags);
            ChildTags = new HashSet<Tuple<string, string>>(otherTag.ChildTags);
        }

        /*x
        public bool Initialize(string parentCategoryName = "", bool initializeLinks = true)
        {
            /*x
            if (parentCategoryName != "") { ParentCategoryName = parentCategoryName; }

            if (!WallpaperData.TaggingInfo.ContainsTag(this)) // this tag is likely different but two tags cannot have the same name
            {
                WallpaperData.TaggingInfo.AddTag(this); // must be called before adding the tag to the linked images below

                if (initializeLinks) // not needed when loading | This section implies that the data has not yet been applied
                {
                    foreach (string image in LinkedImages)
                    {
                        WallpaperData.GetImageData(image).AddTag(this);
                    }

                    foreach (Tuple<string, string> tagInfo in ChildTags)
                    {
                        WallpaperData.TaggingInfo.GetTag(tagInfo.Item1, tagInfo.Item2).LinkTag(this);
                    }

                    foreach (Tuple<string, string> tagInfo in ParentTags)
                    {
                        LinkTag(WallpaperData.TaggingInfo.GetTag(tagInfo.Item1, tagInfo.Item2));
                    }
                }

                IsInitialized = true;
                return true;
            }

            // a tag with this tag's name already exists, initialization failed
            return false;
        }

        public void SetName(string newName)
        {
            if (Name != newName)
            {
                foreach (string image in LinkedImages)
                {
                    WallpaperData.GetImageData(image).RenameTag(ParentCategoryName, Name, newName);
                }

                foreach (Tuple<string, string> tagInfo in ParentTags)
                {
                    TempTagData parentTag = WallpaperData.TaggingInfo.GetTag(tagInfo.Item1, tagInfo.Item2);
                    parentTag.ChildTags.Remove(new Tuple<string, string>(ParentCategoryName, Name));
                    parentTag.ChildTags.Add(new Tuple<string, string>(ParentCategoryName, newName));
                }

                foreach (Tuple<string, string> tagInfo in ChildTags)
                {
                    TempTagData childTag = WallpaperData.TaggingInfo.GetTag(tagInfo.Item1, tagInfo.Item2);
                    childTag.ParentTags.Remove(new Tuple<string, string>(ParentCategoryName, Name));
                    childTag.ParentTags.Add(new Tuple<string, string>(ParentCategoryName, newName));
                }

                Name = newName;
            }
            else
            {
                Debug.WriteLine("Attempted to rename a tag to the same name, operation cancelled");
            }
        }

        /// <summary>
        /// Note that ImageData's AddTag method also calls this
        /// </summary>
        /// <param name="image"></param>
        public void LinkImage(WallpaperData.ImageData image)
        {
            LinkedImages.Add(image.Path);
        }

        /// <summary>
        /// Note that ImageData's RemoveTag method also calls this
        /// </summary>
        /// <param name="image"></param>
        public void UnlinkImage(WallpaperData.ImageData image)
        {
            LinkedImages.Remove(image.Path);
        }

        public void LinkTag(TempTagData tagToLink)
        {
            Tuple<string, string> tagToLinkInfo = new Tuple<string, string>(tagToLink.ParentCategoryName, tagToLink.Name);
            if (!ChildTags.Contains(tagToLinkInfo))
            {
                ParentTags.Add(tagToLinkInfo);

                // Also link all of the other tag's parent tags to this tag since they'll be used regardless
                foreach (Tuple<string, string> tagInfo in tagToLink.ParentTags)
                {
                    this.LinkTag(WallpaperData.TaggingInfo.GetTag(tagInfo.Item1, tagInfo.Item2));
                }

                // and finally link this tag to all child tags of the current tag
                foreach (Tuple<string, string> tagInfo in this.ChildTags)
                {
                    WallpaperData.TaggingInfo.GetTag(tagInfo.Item1, tagInfo.Item2).LinkTag(tagToLink);
                }

                tagToLink.ChildTags.Add(new Tuple<string, string>(this.ParentCategoryName, this.Name));
                CopyLinkedImagesToTag(tagToLink);
            }
            else
            {
                MessageBox.Show(tagToLink.Name + " cannot be made a parent tag of " + this.Name + " since it's already a child tag of " + this.Name);
            }
        }

        public void UnlinkTag(TempTagData tag)
        {
            Tuple<string, string> linkedTagInfo = new Tuple<string, string>(tag.ParentCategoryName, tag.Name);
            Tuple<string, string> thisTagInfo = new Tuple<string, string>(this.ParentCategoryName, this.Name);

            if (ParentTags.Contains(linkedTagInfo))
            {
                ParentTags.Remove(linkedTagInfo);
                WallpaperData.TaggingInfo.GetTag(linkedTagInfo.Item1, linkedTagInfo.Item2).ChildTags.Remove(thisTagInfo);
            }
            else if (ChildTags.Contains(linkedTagInfo))
            {
                ChildTags.Remove(linkedTagInfo);
                WallpaperData.TaggingInfo.GetTag(linkedTagInfo.Item1, linkedTagInfo.Item2).ParentTags.Remove(thisTagInfo);
            }
        }

        /// <summary>
        /// Untags and unlinks all images from this tag
        /// </summary>
        public void UntagAllImages()
        {
            while (LinkedImages.Count > 0)
            {
                WallpaperData.GetImageData(LinkedImages.First()).RemoveTag(this); // note that this will also unlink all images
            }
        }

        public void UnlinkAllTags()
        {
            while (ParentTags.Count > 0)
            {
                UnlinkTag(WallpaperData.TaggingInfo.GetTag(ParentTags.First().Item1, ParentTags.First().Item2));
            }

            while (ChildTags.Count > 0)
            {
                UnlinkTag(WallpaperData.TaggingInfo.GetTag(ChildTags.First().Item1, ChildTags.First().Item2));
            }
        }

        /// <summary>
        /// Removes this tag from all images using this tag and unlinks all linked tags
        /// </summary>
        public void ClearTag()
        {
            UnlinkAllTags();
            UntagAllImages();
        }

        public string[] GetLinkedImages()
        {
            return LinkedImages.ToArray();
        }

        public int GetLinkedImageCount()
        {
            return LinkedImages.Count;
        }

        public void CopyLinkedImagesToTag(TempTagData otherTag)
        {
            foreach (string image in LinkedImages)
            {
                WallpaperData.GetImageData(image).AddTag(otherTag);
            }
        }

        //! Unused
        public static bool operator ==(TempTagData tag1, TempTagData tag2)
        {
            return tag1?.ParentCategoryName == tag2?.ParentCategoryName && tag1?.Name == tag2?.Name;
        }

        //! Never actually compared with another tag
        public static bool operator !=(TempTagData tag1, TempTagData tag2)
        {
            return tag1?.ParentCategoryName != tag2?.ParentCategoryName && tag1?.Name != tag2?.Name;
        }


        private void UpdateLinkedTagsCategoryName(string newCategoryName)
        {
            Tuple<string, string> activeTagInfo = new Tuple<string, string>(parentCategoryName, Name);

            if (ChildTags != null)
            {
                foreach (Tuple<string, string> tagInfo in ChildTags)
                {
                    TempTagData childTag = WallpaperData.TaggingInfo.GetTag(tagInfo.Item1, tagInfo.Item2) ?? WallpaperData.TaggingInfo.GetTag(parentCategoryName, tagInfo.Item2);

                    if (childTag != null)
                    {
                        HashSet<Tuple<string, string>> updatedParentTags = new HashSet<Tuple<string, string>>();
                        foreach (Tuple<string, string> parentTagInfo in childTag.ParentTags)
                        {
                            if (parentTagInfo.Item2 == Name)
                            {
                                Tuple<string, string> updatedTagInfo = new Tuple<string, string>(newCategoryName, Name);
                                updatedParentTags.Add(updatedTagInfo);
                            }
                            else
                            {
                                updatedParentTags.Add(parentTagInfo);
                            }
                        }
                        childTag.ParentTags = updatedParentTags;
                    }
                }
            }

            if (ParentTags != null)
            {
                foreach (Tuple<string, string> tagInfo in ParentTags)
                {
                    TempTagData parentTag = WallpaperData.TaggingInfo.GetTag(tagInfo.Item1, tagInfo.Item2) ?? WallpaperData.TaggingInfo.GetTag(parentCategoryName, tagInfo.Item2);

                    if (parentTag != null)
                    {
                        HashSet<Tuple<string, string>> updatedChildTags = new HashSet<Tuple<string, string>>();
                        foreach (Tuple<string, string> childTagInfo in parentTag.ChildTags)
                        {
                            if (childTagInfo.Item2 == Name)
                            {
                                Tuple<string, string> updatedTagInfo = new Tuple<string, string>(newCategoryName, Name);
                                updatedChildTags.Add(updatedTagInfo);
                            }
                            else
                            {
                                updatedChildTags.Add(childTagInfo);
                            }
                        }
                        parentTag.ChildTags = updatedChildTags;
                    }
                }
            }
        }
        */

    }

}
