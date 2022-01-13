using System;
using System.Collections.Generic;
using System.Text;
using WallpaperFlux.Core.Models;
using WallpaperFlux.Core.Models.Tagging;

namespace WallpaperFlux.Core.Collections
{
    public class TagCollection
    {
        public readonly ImageModel ParentImage;

        // TODO Try to avoid adding parent tags into this list, find another way around this
        // TODO Try to avoid adding parent tags into this list, find another way around this
        // TODO Try to avoid adding parent tags into this list, find another way around this
        // TODO Try to avoid adding parent tags into this list, find another way around this
        private readonly HashSet<TagModel> _tags = new HashSet<TagModel>();

        // this exists to help restrict control of the information in both TagModel and ImageModel that allows the two to refer to each other
        public TagCollection(ImageModel parentImage)
        {
            ParentImage = parentImage;
        }

        public void Add(TagModel tag)
        {
            _tags.Add(tag);
            tag.LinkImage(this);
        }

        public void Remove(TagModel tag)
        {
            _tags.Remove(tag);
            tag.UnlinkImage(this);
        }
    }
}
