using System;
using System.Collections.Generic;
using System.Text;

namespace WallpaperFlux.Core.JSON
{
    //? Simplified version of models/data used to minimize the size of the JSON

    public struct SimplifiedFolder
    {
        public string Path;

        public bool Enabled;

        public SimplifiedFolder(string path, bool enabled)
        {
            Path = path;
            Enabled = enabled;
        }
    }

    public struct SimplifiedCategory
    {
        public string Name;

        public bool Enabled;

        public bool UseForNaming;

        public SimplifiedTag[] Tags;

        public SimplifiedCategory(string name, bool enabled, bool useForNaming, SimplifiedTag[] tags)
        {
            Name = name;
            Enabled = enabled;
            UseForNaming = useForNaming;
            Tags = tags;
        }
    }

    public struct SimplifiedTag
    {
        public string Name;

        public bool Enabled;

        public bool UseForNaming;

        public string ParentCategoryName;

        public SimplifiedParentTag[] ParentTags;

        public SimplifiedTag(string name, bool enabled, bool useForNaming, string parentCategoryName, SimplifiedParentTag[] parentTags)
        {
            Name = name;
            Enabled = enabled;
            UseForNaming = useForNaming;
            ParentCategoryName = parentCategoryName;
            ParentTags = parentTags;
        }
    }

    public struct SimplifiedParentTag
    {
        public string Name;
        public string ParentCategoryName; //? we still need to know the name of the category, as different categories can have tags with the same name

        public SimplifiedParentTag(string name, string parentCategoryName)
        {
            Name = name;
            ParentCategoryName = parentCategoryName;
        }
    }

    public struct SimplifiedImage
    {
        public string Path;

        public int Rank;

        // TODO Consider handling similarly to SimplifiedParentTag or the other way around for both, analyze the benefits / costs to the JSON size (The dictionary method results in less category naming)
        // TODO The reason why SimplifiedParentTag was handled differently in the first place was because of it pulling directly from the property in the older version
        public Dictionary<string, List<string>> Tags; //? similar to SimplifiedParentTag, we want to know the category name to prevent duplicate name issues without loading in too much information

        public Dictionary<string, List<string>> TagNamingExceptions;

        public double Volume;

        public SimplifiedImage(string path, int rank, Dictionary<string, List<string>> tags, Dictionary<string, List<string>> tagNamingExceptions, double volume)
        {
            Path = path;
            Rank = rank;
            Tags = tags;
            TagNamingExceptions = tagNamingExceptions;
            Volume = volume;
        }
    }
}
