using System;
using System.Collections.Generic;
using System.Text;

namespace WallpaperFlux.Core.Util
{
    public enum TagSortType
    {
        Name,
        Count
    }


    public static class TaggingUtil
    {
        public const float TAGGING_WINDOW_WIDTH = 700;
        public const float TAGGING_WINDOW_HEIGHT = 625;

        public static int TagsPerPage = 50;
    }
}
