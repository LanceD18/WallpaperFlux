using System;
using System.Collections.Generic;
using System.Text;
using WallpaperFlux.Core.Collections;
using WallpaperFlux.Core.ViewModels;

namespace WallpaperFlux.Core.Util
{
    public enum TagSortType
    {
        Name,
        Count
    }


    public static class TaggingUtil
    {
        private static TagViewModel Instance;

        public const float TAGGING_WINDOW_WIDTH = 950;
        public const float TAGGING_WINDOW_HEIGHT = 700;

        public static int TagsPerPage = 50;

        //? This was supposed to remove the static references to Instance all together but a complication with the view creation has made this into an issue I'll look into later
        // TODO I'd imagine that this can be handled nicely with WallpaperFluxViewModel, however
        public static void SetInstance(TagViewModel instance)
        {
            Instance = instance;
        }

        public static bool InstanceExists() => Instance != null;

        public static bool GetTagAdderToggle() => InstanceExists() && Instance.TagAdderToggle;

        public static bool GetTagRemoverToggle() => InstanceExists() && Instance.TagRemoverToggle;

        public static void HighlightTags(TagCollection tags)
        {
            if (InstanceExists())
            {
                Instance.HighlightTags(tags);
            }
        }
    }
}
