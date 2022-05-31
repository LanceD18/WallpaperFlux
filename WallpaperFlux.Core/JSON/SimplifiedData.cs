using System;
using System.Collections.Generic;
using System.Text;

namespace WallpaperFlux.Core.JSON
{
    //? Simplified version of models/data used to minimize the size of the JSON

    public struct SimplifiedTag
    {
        public string Name;
        public string CategoryName;
    }

    public struct SimplifiedTagCollection
    {
        public HashSet<SimplifiedTag> Tags;
    }
}
