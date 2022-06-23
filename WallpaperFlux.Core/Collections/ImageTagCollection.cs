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

        public void Add(TagModel tag)
        {
            _tags.Add(tag); // we're using a hashset, no need to worry about duplicate tags
            tag.LinkImage(this);
            TaggingUtil.HighlightTags();

            WallpaperFluxViewModel.Instance.RaisePropertyChanged(() => WallpaperFluxViewModel.Instance.InspectedImageTags);
        }

        public void Remove(TagModel tag)
        {
            _tags.Remove(tag);
            tag.UnlinkImage(this);
            TaggingUtil.HighlightTags();
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

        public TagModel[] GetTags() => _tags.ToArray();

        public HashSet<TagModel> GetTags_HashSet() => _tags;

        // for use with the JSON
        public string[] GetTagsString() => _tags.Select(f => f.Name).ToArray();

        public string GetTaggedName()
        {
            string taggedName = "";

            // TODO Order tags by category ; go through categories in order then grabs the tags from said category ; order said group of tags alphabetically ; repeat

            List<string> orderedTags = new List<string>();
            foreach (CategoryModel category in DataUtil.Theme.Categories)
            {
                if (!category.UseForNaming) continue; // skipped categories tag have UseForNaming disabled

                // gets the active tags within this category group
                List<string> tagsInThisCategoryGroup = new List<string>();
                foreach (TagModel tag in _tags)
                {
                    if (category.GetTags().Contains(tag))
                    {
                        tagsInThisCategoryGroup.Add(tag.Name);
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
    }
}
