using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WallpaperFlux.Core.Models;
using WallpaperFlux.Core.Models.Tagging;
using WallpaperFlux.Core.Util;
using WallpaperFlux.Core.ViewModels;
using System.Diagnostics;

namespace WallpaperFlux.Core.Collections
{
    public class ImageTagCollection
    {
        public readonly ImageModel ParentImage;
        
        private readonly HashSet<TagModel> _tags = new HashSet<TagModel>();

        private readonly HashSet<TagModel> _tagNamingExceptions = new HashSet<TagModel>(); // these tags can be used for naming regardless of constraints

        /// <summary>
        /// this exists to help restrict control of the information in both TagModel and ImageModel that allows the two to refer to each other
        /// </summary>
        /// <param name="parentImage"> the image the tag collection will be linked to </param>
        /// <param name="relinkTagCollection"> we will likely create an image and tag collection simultaneously, hence this is defaulted to true </param>
        public ImageTagCollection(ImageModel parentImage, bool relinkTagCollection = true)
        {
            ParentImage = parentImage;

            if (relinkTagCollection) parentImage.Tags = this;
        }

        public void Add(TagModel tag, bool highlightTags)
        {
            _tags.Add(tag); // we're using a hashset, no need to worry about duplicate tags
            tag.LinkImage(this);
            TaggingUtil.HighlightTags();

            WallpaperFluxViewModel.Instance.RaisePropertyChanged(() => WallpaperFluxViewModel.Instance.InspectedImageTags);
        }

        public void Remove(TagModel tag, bool highlightTags)
        {
            _tags.Remove(tag);
            tag.UnlinkImage(this);
            if (highlightTags) TaggingUtil.HighlightTags();
            _tagNamingExceptions.Remove(tag); // the naming exception status will be reset on re-add, also, this reduces potential JSON bloat

            WallpaperFluxViewModel.Instance.RaisePropertyChanged(() => WallpaperFluxViewModel.Instance.InspectedImageTags);
        }

        /// <summary>
        /// For removing the image itself
        /// </summary>
        /// <param name="tag"></param>
        public void RemoveAllTags()
        {
            foreach (TagModel tag in _tags)
            {
                tag.UnlinkImage(this);
            }

            TaggingUtil.HighlightTags(/*xthis*/); // may still have the highlight of this image active while doing this, so re-highlight

            _tags.Clear();
        }

        public bool Contains(TagModel tag) => _tags.Contains(tag);

        public HashSet<TagModel> GetTags() => _tags;

        public HashSet<TagModel> GetTagNamingExceptions() => _tagNamingExceptions;
        
        public Dictionary<string, List<string>> GetConvertTagsToDictionary() => ConvertTagsToDictionary(_tags);

        public Dictionary<string, List<string>> GetConvertTagNamingExceptionsToDictionary()
        {
            return ConvertTagsToDictionary(_tagNamingExceptions);
        }

        //? for use with the JSON
        private Dictionary<string, List<string>> ConvertTagsToDictionary(HashSet<TagModel> tags)
        {
            Dictionary<string, List<string>> tagsDictionary = new Dictionary<string, List<string>>();

            foreach (TagModel tag in tags)
            {
                if (!tagsDictionary.ContainsKey(tag.ParentCategory.Name))
                {
                    tagsDictionary.Add(tag.ParentCategory.Name, new List<string>());
                }

                tagsDictionary[tag.ParentCategory.Name].Add(tag.Name);
            }

            return tagsDictionary;
        }

        public string GetTaggedName()
        {
            string taggedName = "";

            //? Process: Order tags by category ; go through categories in order then grabs the tags from said category ; order said group of tags alphabetically ; repeat

            List<string> orderedTags = new List<string>();
            foreach (CategoryModel category in DataUtil.Theme.Categories)
            {
                //? skip categories that do not have the UseForNaming tag IF the number of tagNamingExceptions is 0, otherwise, scan every tag
                if (!category.UseForNaming && _tagNamingExceptions.Count == 0) continue; 

                // gets the active tags within this category group
                HashSet<string> tagsInThisCategoryGroup = new HashSet<string>(); //? using a HashSet to prevent potential duplication when applying the naming exceptions
                foreach (TagModel tag in _tags)
                {
                    // tag must have UseForNaming & it's category's UseForNaming enabled unless it was given an exception
                    if ((tag.UseForNaming && tag.ParentCategory.UseForNaming) || _tagNamingExceptions.Contains(tag))
                    {
                        if (category.GetTags().Contains(tag))
                        {
                            tagsInThisCategoryGroup.Add(tag.Name);
                        }
                    }
                }

                // alphabetically orders the given tag group then drops this group into the ordered tags list
                orderedTags.AddRange(tagsInThisCategoryGroup.OrderBy(f => f).ToList());
            }

            foreach (string tag in orderedTags)
            {
                taggedName += tag;
            }

            return taggedName.Replace(' ', '_');
        }

        public void AddNamingException(TagModel tag)
        {
            if (_tags.Contains(tag)) _tagNamingExceptions.Add(tag);
        }

        // not checking if this tag is in the collection will allow us to remove rogue / leftover tags in the naming exceptions
        public void RemoveNamingException(TagModel tag) => _tagNamingExceptions.Remove(tag);
    }
}
