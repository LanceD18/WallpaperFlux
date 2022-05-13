using System;
using System.Collections.Generic;
using System.Text;

namespace WallpaperFlux.Core.Models.Tagging
{
    public class TagModelJSON
    {
        //? Will be used to save the theme, converts ParentTags, ChildTags, and LinkedImages into strings so that we aren't saving the entire object 

        public HashSet<Tuple<string, string>> ParentTags = new HashSet<Tuple<string, string>>();
        public HashSet<Tuple<string, string>> ChildTags = new HashSet<Tuple<string, string>>();
        public HashSet<string> LinkedImages = new HashSet<string>();
    }
}
