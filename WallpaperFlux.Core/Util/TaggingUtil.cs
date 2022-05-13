using System;
using System.Collections.Generic;
using System.Text;
using WallpaperFlux.Core.Collections;
using WallpaperFlux.Core.Models.Tagging;
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
        //? This was supposed to remove the static references to Instance all together but a complication with the view creation has made this into an issue I'll look into later
        // TODO I'd imagine that this can be handled nicely with WallpaperFluxViewModel, however
        //x private static TagViewModel Instance;

        public const float TAGGING_WINDOW_WIDTH = 950;
        public const float TAGGING_WINDOW_HEIGHT = 625;

        public static int TagsPerPage = 50;

        //? This was supposed to remove the static references to Instance all together but a complication with the view creation has made this into an issue I'll look into later
        // TODO I'd imagine that this can be handled nicely with WallpaperFluxViewModel, however
        /*x
        public static void SetInstance(TagViewModel instance)
        {
            Instance = instance;
        }
        */

        public static bool InstanceExists() => TagViewModel.Instance != null;

        public static bool GetTagAdderToggle() => InstanceExists() && TagViewModel.Instance.TagAdderToggle;

        public static bool GetTagRemoverToggle() => InstanceExists() && TagViewModel.Instance.TagRemoverToggle;

        public static bool GetTagLinkerToggle() => InstanceExists() && TagViewModel.Instance.TagLinkerToggle;

        public static void HighlightTags(TagCollection tags)
        {
            if (InstanceExists()) TagViewModel.Instance.HighlightTags(tags.GetTags_HashSet());
        }

        public static void HighlightTags(HashSet<TagModel> tags)
        {
            if (InstanceExists()) TagViewModel.Instance.HighlightTags(tags);
        }
    }
}
