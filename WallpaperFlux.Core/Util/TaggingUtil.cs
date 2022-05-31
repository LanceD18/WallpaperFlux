using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public static void HighlightTags(ImageTagCollection tags)
        {
            if (InstanceExists()) TagViewModel.Instance.SetTagsToHighlight(tags.GetTags_HashSet());
        }

        public static void HighlightTags(HashSet<TagModel> tags)
        {
            if (InstanceExists()) TagViewModel.Instance.SetTagsToHighlight(tags);
        }

        public static bool ContainsCategory(string categoryName) => GetCategory(categoryName) != null;

        public static CategoryModel GetCategory(string categoryName)
        {
            foreach (CategoryModel category in DataUtil.Theme.Categories)
            {
                if (categoryName == category.Name) return category;
            }

            return null;
        }

        public static CategoryModel AddCategory(string categoryName, bool useForNaming = true, bool enabled = true)
        {
            if (!ContainsCategory(categoryName))
            {
                CategoryModel category = new CategoryModel(categoryName, useForNaming, enabled);
                AddCategory(category);
                return category;
            }
            else // this category already exists
            {
                return null;
            }
        }

        public static void AddCategory(CategoryModel newCategory) => AddCategoryRange(new CategoryModel[] { newCategory });

        public static void AddCategoryRange(CategoryModel[] newCategories)
        {
            foreach (CategoryModel category in newCategories)
            {
                DataUtil.Theme.Categories.Add(category);
            }
        }

        /// <summary>
        /// Ensures that the category with the given name exists and returns it
        /// </summary>
        /// <param name="categoryName"></param>
        /// <param name="useForNaming"></param>
        /// <param name="enabled"></param>
        /// <returns></returns>
        public static CategoryModel VerifyCategory(string categoryName, bool useForNaming = true, bool enabled = true)
        {
            if (!ContainsCategory(categoryName))
            {
                Debug.WriteLine("Category " + categoryName + " is missing, adding");
                // category is missing, add it
                return AddCategory(categoryName, useForNaming, enabled);
            }
            else
            {
                CategoryModel category = GetCategory(categoryName);

                //? In the context that this method is being used, in some cases the category will be added before these values are
                //? set (A parent tag present in the category is found first), but it will eventually reach this point
                category.UseForNaming = useForNaming;
                category.Enabled = enabled;

                return category;
            }
        }
    }
}
