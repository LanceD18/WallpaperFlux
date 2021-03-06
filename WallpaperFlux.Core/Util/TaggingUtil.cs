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

    public enum TagSearchType
    {
        Mandatory,
        Optional,
        Excluded
    }

    //? This was supposed to remove the static references to Instance all together but a complication with the view creation has made this into an issue I'll look into later
    // TODO I'd imagine that this can be handled nicely with WallpaperFluxViewModel, however
    public static class TaggingUtil
    {
        public static bool InstanceExists => TagViewModel.Instance != null;
        //x private static TagViewModel Instance;

        public const float TAGGING_WINDOW_WIDTH = 950;
        public const float TAGGING_WINDOW_HEIGHT = 650;

        public static int TagsPerPage = 50;

        //? This was supposed to remove the static references to Instance all together but a complication with the view creation has made this into an issue I'll look into later
        // TODO I'd imagine that this can be handled nicely with WallpaperFluxViewModel, however
        /*x
        public static void SetInstance(TagViewModel instance)
        {
            Instance = instance;
        }
        */

        // Tag Sorting & Searching
        public const TagSearchType DEFAULT_TAG_SEARCH_TYPE = TagSearchType.Mandatory;
        
        private static TagSortType _activeSortType = TagSortType.Name;

        private static bool _sortByNameDirection = true; // default ascending option

        private static bool _sortByCountDirection;

        // Tag Highlights & Toggles
        public static bool GetTagAdderToggle() => InstanceExists && TagViewModel.Instance.TagAdderToggle;

        public static bool GetTagLinkerToggle() => InstanceExists && TagViewModel.Instance.TagLinkerToggle;

        //xpublic static void HighlightTags(ImageTagCollection tags) => HighlightTags(tags.GetTags_HashSet());

        public static void HighlightTags(/*xHashSet<TagModel> tags*/)
        {
            if (InstanceExists) TagViewModel.Instance.HighlightTags(/*xtags*/);
        }

        #region Category Control
        public static void UpdateCategoryView()
        {
            if (InstanceExists)
            {
                if (TagViewModel.Instance.Categories != null)
                {
                    //xTagViewModel.Instance.Categories = new MvvmCross.ViewModels.MvxObservableCollection<CategoryModel>(DataUtil.Theme.Categories);
                    //! each switch increases the minimum number of required events (CHECK THIS), could maybe use a 'new' statement instead (see above commented code)
                    TagViewModel.Instance.Categories.SwitchTo(DataUtil.Theme.Categories);

                    /*x
                    List<CategoryModel> categories = new List<CategoryModel>(DataUtil.Theme.Categories);
                    if (DataUtil.Theme.Categories.Count > 1)
                    {
                        TagViewModel.Instance.Categories.Add(categories[0]);
                    }
                    */
                    /*x
                    for (int i = 0; i < TagViewModel.Instance.Categories.Count; i++)
                    {
                        TagViewModel.Instance.Categories.Add(Taf)
                    }
                    */
                }
            }
        }

        public static bool ContainsCategory(string categoryName) => GetCategory(categoryName) != null;

        public static bool ContainsCategory(CategoryModel category) => DataUtil.Theme.Categories.Contains(category);

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
            CategoryModel category = new CategoryModel(categoryName, useForNaming, enabled);
            AddCategory(category);
            return category;
        }

        public static void AddCategory(CategoryModel newCategory) => AddCategoryRange(new CategoryModel[] { newCategory });

        public static void AddCategoryRange(CategoryModel[] newCategories)
        {
            foreach (CategoryModel category in newCategories)
            {
                if (!ContainsCategory(category))
                {
                    DataUtil.Theme.Categories.Add(category);
                }
            }

            UpdateCategoryView();
        }

        public static bool RemoveCategory(CategoryModel category)
        {
            bool removed = DataUtil.Theme.Categories.Remove(category);
            UpdateCategoryView();
            return removed;
        }

        public static void RemoveCategoryAt(int index)
        {
            DataUtil.Theme.Categories.RemoveAt(index);
            UpdateCategoryView();
        }

        public static void InsertCategory(int index, CategoryModel category)
        {
            DataUtil.Theme.Categories.Insert(index, category);
            UpdateCategoryView();

        }

        /// <summary>
        /// Ensures that the category with the given name exists and returns it
        /// </summary>
        /// <param name="categoryName"></param>
        /// <param name="useForNaming"></param>
        /// <param name="enabled"></param>
        /// <returns></returns>
        public static CategoryModel VerifyCategory(string categoryName, bool useForNaming = true, bool enabled = true, bool applyActualData = false)
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
                if (applyActualData) // don't want to override these with defaults if this is called again in the wrong context
                {
                    category.UseForNaming = useForNaming;
                    category.Enabled = enabled;
                }

                return category;
            }
        }
        
        // just a version with mandatory arguments
        public static CategoryModel VerifyCategoryWithData(string tagName, bool useForNaming, bool enabled, bool applyActualData) => VerifyCategory(tagName, useForNaming, enabled, applyActualData);

        /// <summary>
        /// Moves a category to the selected position via insertion & removal, shifting the categories in-between accordingly
        /// </summary>
        public static void ShiftCategories(CategoryModel sourceCategory, CategoryModel targetCategory)
        {
            // gather the required indexes to shift the source category
            int sourceIndex = DataUtil.Theme.Categories.IndexOf(sourceCategory);
            int targetIndex = DataUtil.Theme.Categories.IndexOf(targetCategory);

            // remove the original instance of the category from its source position
            DataUtil.Theme.Categories.RemoveAt(sourceIndex);
            
            // re-insert the category at the target position, the insertion will handle the shifting on its own
            InsertCategory(targetIndex, sourceCategory);

            UpdateCategoryView();
        }

        #region Prompt
        public static CategoryModel PromptAddCategory()
        {
            string categoryName = MessageBoxUtil.GetString("Create New Category", "Give a name for this category", "Category Name...");

            if (!string.IsNullOrEmpty(categoryName))
            {
                CategoryModel category = AddCategory(categoryName);

                return category;
            }

            return null;
        }

        public static void PromptAddTagToCategory(CategoryModel category)
        {
            if (category != null)
            {
                string tagName = MessageBoxUtil.GetString("Create New Tag", "Give a name for this tag", "Tag Name...");

                if (!string.IsNullOrEmpty(tagName))
                {
                    category.AddTag(tagName);
                }
            }
            else
            {
                MessageBoxUtil.ShowError("Selected category does not exist");
            }
        }
        #endregion
        #endregion

        #region Tag Control
        public static bool RemoveTag(TagModel tag) => tag.ParentCategory.RemoveTag(tag);
        #endregion

        #region Tag Sorting

        public static TagSortType GetActiveSortType() => _activeSortType;

        public static void SetActiveSortType(TagSortType sortType) => _activeSortType = sortType;

        public static bool GetSortByNameDirection() => _sortByNameDirection;

        public static void SetSortByNameDirection(bool sortByNameDirection) => _sortByNameDirection = sortByNameDirection;

        public static bool GetSortByCountDirection() => _sortByCountDirection;

        public static void SetSortByCountDirection(bool sortByCountDirection) => _sortByCountDirection = sortByCountDirection;

        #endregion
    }
}
