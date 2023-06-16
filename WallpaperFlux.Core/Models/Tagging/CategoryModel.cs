using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using HandyControl.Controls;
using LanceTools.WPF.Adonis.Util;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using MvvmCross.Views;
using Newtonsoft.Json;
using WallpaperFlux.Core.Models.Controls;
using WallpaperFlux.Core.Util;
using WallpaperFlux.Core.ViewModels;
using MessageBox = AdonisUI.Controls.MessageBox;

namespace WallpaperFlux.Core.Models.Tagging
{
    public class CategoryModel : MvxNotifyPropertyChanged
    {
        private HashSet<TagModel> Tags = new HashSet<TagModel>();

        private string _name;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private bool _enabled;
        public bool Enabled
        {
            get => _enabled;

            set
            {
                SetProperty(ref _enabled, value);

                foreach (TagModel tag in Tags)
                {
                    tag.UpdateLinkedImagesEnabledState();
                }
            }
        }

        private bool _useForNaming = true;

        public bool UseForNaming
        {
            get => _useForNaming;

            set
            {
                if (_useForNaming != value) // no need to run through the entire list of tags again if UseForNaming receives the same value
                {
                    SetProperty(ref _useForNaming, value);

                    foreach (TagModel tag in Tags)
                    {
                        tag.RaisePropertyChanged(() => tag.UseForNaming_IncludeCategory);
                        tag.RaisePropertyChanged(() => tag.ExceptionColor); //? depends on the value of UseForNaming_IncludeCategory
                    }

                    /*x
                    HashSet<WallpaperData.ImageData> imagesToRename = new HashSet<WallpaperData.ImageData>();
                    foreach (TagData tag in Tags)
                    {
                        foreach (string imagePath in tag.GetLinkedImages())
                        {
                            imagesToRename.Add(WallpaperData.GetImageData(imagePath));
                        }
                    }
                    */
                }
            }
        }

        public float Frequency { get; set; }

        #region Search & Sorting

        // Search Filter
        private string _searchFilter;

        [JsonIgnore]
        public string SearchFilter
        {
            get => _searchFilter;
            set
            {
                SetProperty(ref _searchFilter, value);
                RaisePropertyChanged(() => IsSearching);

                VerifyTagTabs();
            }
        }

        // Tabs
        private MvxObservableCollection<TagTabModel> _tagTabs = new MvxObservableCollection<TagTabModel>();

        public MvxObservableCollection<TagTabModel> TagTabs
        {
            get => _tagTabs;
            set => SetProperty(ref _tagTabs, value);
        }

        private TagTabModel _selectedTagTab;
        public TagTabModel SelectedTagTab
        {
            get => _selectedTagTab;
            set
            {
                //! Workaround to the EnsureSingularSelection() methods from WPF ControlUtil ; ideally we'd look for a less brute force solution
                // TODO Apply this to the new VisibleTags collection
                SelectedTagTab?.DeselectAllItems(); //? this is the previously selected tab as we are calling this before the value is set
                //! Workaround to the EnsureSingularSelection() methods from WPF ControlUtil ; ideally we'd look for a less brute force solution

                SetProperty(ref _selectedTagTab, value);
                TagViewModel.Instance.VerifyVisibleTags();
            }
        }

        public TagModel[] SortedTags;
        public TagModel[] FilteredTags;

        #endregion

        #region View Variables

        [JsonIgnore] public string TagCountString => "Contains " + Tags.Count + " tag(s)";

        public bool IsSearching => string.IsNullOrEmpty(SearchFilter);

        #endregion

        #region Commands

        [JsonIgnore] public IMvxCommand ToggleSortByNameCommand { get; set; }

        [JsonIgnore] public IMvxCommand ToggleSortByCountCommand { get; set; }

        [JsonIgnore] public IMvxCommand ViewTagBoardCommand { get; set; }

        [JsonIgnore] public IMvxCommand AddSelectedTagsToTagBoardCommand { get; set; }

        [JsonIgnore] public IMvxCommand ClearTagBoardCommand { get; set; }

        [JsonIgnore] public IMvxCommand RenameCategoryCommand { get; set; }

        [JsonIgnore] public IMvxCommand RemoveCategoryCommand { get; set; }

        [JsonIgnore] public IMvxCommand SelectImagesWithEverySelTag { get; set; }

        [JsonIgnore] public IMvxCommand SelectImagesWithAnySelTag { get; set; }

        [JsonIgnore] public IMvxCommand SelectImagesWithAnyCategoryTag { get; set; }

        public IMvxCommand ClearSearchCommand { get; set; }

        #endregion

        public CategoryModel(string name, bool useForNaming = true, bool enabled = true)
        {
            Name = name;
            UseForNaming = useForNaming;
            Enabled = enabled;

            ToggleSortByNameCommand = new MvxCommand(() => ToggleSortOption(TagSortType.Name));
            ToggleSortByCountCommand = new MvxCommand(() => ToggleSortOption(TagSortType.Count));

            ViewTagBoardCommand = new MvxCommand(() => TagViewModel.Instance.ToggleTagBoard());
            AddSelectedTagsToTagBoardCommand = new MvxCommand(() => TagViewModel.Instance.AddTagsToTagBoard(GetSelectedTags()));
            ClearTagBoardCommand = new MvxCommand(() => TagViewModel.Instance.ClearTagBoard());

            RenameCategoryCommand = new MvxCommand(PromptRename);
            RemoveCategoryCommand = new MvxCommand(PromptRemoveCategory);
            
            SelectImagesWithEverySelTag = new MvxCommand(() => TaggingUtil.RebuildImageSelectorWithTagOptions(SelectValidImages(TagSearchType.Mandatory, GetSelectedTags())));
            SelectImagesWithAnySelTag = new MvxCommand(() => TaggingUtil.RebuildImageSelectorWithTagOptions(SelectValidImages(TagSearchType.Optional, GetSelectedTags())));
            SelectImagesWithAnyCategoryTag = new MvxCommand(() => TaggingUtil.RebuildImageSelectorWithTagOptions(SelectValidImages(TagSearchType.Optional, Tags.ToArray())));

            ClearSearchCommand = new MvxCommand(ClearSearchFilter);
        }

        public void PromptRename()
        {
            string name = MessageBoxUtil.GetString("Rename", "Give a new name for this category", "Category Name...");

            if (!string.IsNullOrWhiteSpace(name)) Name = name;
        }

        public void PromptRemoveCategory()
        {
            //xif (category != null)
            //x{
                if (MessageBoxUtil.PromptYesNo("Are you sure you want to remove the category: [" + Name + "] ?"))
                {
                    if (!TaggingUtil.RemoveCategory(this)) MessageBoxUtil.ShowError("The category, " + Name + ", does not exist");
                }
            //x}
            //xelse
            //x{
            //x    MessageBoxUtil.ShowError("Selected category does not exist");
            //x}
        }

        public void ClearSearchFilter() => SearchFilter = "";

        #region Tag Control

        public TagModel AddTag(string tagName, bool useForNaming = true, bool enabled = true, string renameFolderPath = "")
        {
            TagModel tag = new TagModel(tagName, this, useForNaming, enabled, renameFolderPath);
            AddTag(tag);
            return tag;
        }

        public TagModel[] AddTagRange(string[] newTags)
        {
            List<TagModel> tags = new List<TagModel>();
            foreach (string tag in newTags)
            {
                tags.Add(new TagModel(tag, this));
            }

            TagModel[] tagArray = tags.ToArray();
            AddTagRange(tagArray);
            return tagArray;
        }

        public void AddTag(TagModel newTag) => AddTagRange(new TagModel[] { newTag });

        public void AddTagRange(TagModel[] newTags)
        {
            bool displayWarning = false;
            int errorCount = 0;
            string existingTagWarning = "";
            foreach (TagModel tag in newTags)
            {
                if (!ContainsTag(tag.Name))
                {
                    Tags.Add(tag);
                }
                else
                {
                    displayWarning = true;
                    errorCount++;
                    existingTagWarning += "\n" + tag.Name;
                }
            }

            if (displayWarning && !JsonUtil.IsLoadingData)
            {
                MessageBoxUtil.ShowError(errorCount <= 1 ? "The following tag already exists: " + existingTagWarning : "The following tags already exist: " + existingTagWarning);
            }

            RaisePropertyChanged(() => TagCountString);
            
            VerifyTagTabs(); //? Required for the addition and sorting to be visible right-away. All AddTag() methods should trace back to this method
        }

        public bool RemoveTag(TagModel tag)
        {
            tag.UnlinkAllImages(false);
            tag.UnlinkAllParentAndChildTags(false);
            bool removed = Tags.Remove(tag);
            VerifyTagTabs(); //? needed to visually update the tag's removal

            TaggingUtil.HighlightTags(/*xParentChildTagsUnion_IncludeSelf()*/);

            return removed;
        }

        public bool ContainsTag(string tagName) => GetTag(tagName) != null;

        public TagModel GetTag(string tagName)
        {
            foreach (TagModel tag in Tags)
            {
                if (tag.Name == tagName) return tag;
            }

            return null;
        }

        public HashSet<TagModel> GetTags() => Tags;

        /// <summary>
        /// Ensures that the tag with the given name exists and returns it
        /// </summary>
        /// <param name="tagName"></param>
        /// <param name="useForNaming"></param>
        /// <param name="enabled"></param>
        /// <param name="renameFolderPath"></param>
        /// <param name="applyActualData"></param>
        /// <returns></returns>
        public TagModel VerifyTag(string tagName, bool useForNaming = true, bool enabled = true, string renameFolderPath = "", bool applyActualData = false)
        {
            if (!ContainsTag(tagName))
            {
                //xDebug.WriteLine("Tag " + tagName + " is missing, adding");
                // tag is missing, add it
                return AddTag(tagName, useForNaming, enabled, renameFolderPath);
            }
            else
            {
                TagModel tag = GetTag(tagName);

                //? In the context that this method is being used, in some cases the category will be added before these values are
                //? set (A tag is found as a parent to another tag first), but it will eventually reach this point
                if (applyActualData) // don't want to override these with defaults if this is called again in the wrong context
                {
                    tag.UseForNaming = useForNaming;
                    tag.Enabled = enabled;
                    tag.RenameFolderPath = renameFolderPath;
                }

                return tag;
            }
        }

        // just a version with mandatory arguments
        public TagModel VerifyTagWithData(string tagName, bool useForNaming, bool enabled, string renameFolderPath, bool applyActualData) 
            => VerifyTag(tagName, useForNaming, enabled, renameFolderPath, applyActualData);

        #endregion

        #region Tag Tab Contol

        public TagModel[] GetSelectedTags() => Tags.Where((f) => f.IsSelected).ToArray();

        /// <summary>
        /// verifies the number of pages, tag sorting, and what tags are visible based on the given search
        /// </summary>
        public void VerifyTagTabs()
        {
            if (JsonUtil.IsLoadingData) return;

            //? this also covers the case where the theme is being loaded
            if (!TaggingUtil.InstanceExists) return; // no need to do this if the instance does not exist

            SortTags();

            FilteredTags = GetSortedTagsWithSearchFilter();

            //! we take the length - 1 so that we don't get a miscellaneous extra page when on exactly the tag limit
            //! if the tag amount per page is 25, then while we are at 25 tags we will get (25/25) + 1, leading to 2 pages instead of 1
            //! so subtracting by 1 would gives us (24/25) + 1 instead, fixing the issue
            int totalTagTabCount = ((FilteredTags.Length - 1) / TaggingUtil.TagsPerPage) + 1; //? remember that this 'rounds' off the last page since it's an int

            if (TagTabs.Count != totalTagTabCount)
            {
                while (TagTabs.Count < totalTagTabCount)
                {
                    TagTabs.Add(new TagTabModel(TagTabs.Count + 1)); // + 1 since it doesn't exist yet so the count is still - 1
                }

                while (TagTabs.Count > totalTagTabCount)
                {
                    TagTabs.RemoveAt(TagTabs.Count - 1);
                }
            }

            // auto-selects the first available tab when none are selected
            if (SelectedTagTab == null && TagTabs.Count > 0)
            {
                SelectedTagTab = TagTabs[0];
                RaisePropertyChanged(() => SelectedTagTab);
            }

            TagViewModel.Instance.VerifyVisibleTags();
        }

        public void SortTags()
        {
            // Sort
            IEnumerable<TagModel> sortedItems = string.IsNullOrEmpty(SearchFilter)
                ? Tags.ToArray()
                : FilteredTags.ToArray();

            switch (TaggingUtil.GetActiveSortType())
            {
                case TagSortType.Name:
                    sortedItems = TaggingUtil.GetSortByNameDirection()
                        ? (from f in Tags orderby f.Name select f) // ascending
                        : (from f in Tags orderby f.Name descending select f);
                    break;

                case TagSortType.Count:
                    sortedItems = TaggingUtil.GetSortByCountDirection()
                        ? (from f in Tags orderby f.GetLinkedImageCount() descending select f) // with descending as the default we'll start from the highest number
                        : (from f in Tags orderby f.GetLinkedImageCount() select f); // ascending
                    break;
            }

            SortedTags = sortedItems.ToArray();
        }

        public TagModel[] GetSortedTagsWithSearchFilter()
        {
            if (string.IsNullOrEmpty(SearchFilter))
            {
                return SortedTags.Where(f => !TagViewModel.Instance.HideDisabledTags || f.IsEnabled()).ToArray();
            }
            else
            {
                string lowerCaseSearchFilter = SearchFilter.ToLower();
                return SortedTags.Where(f =>
                    f.Name.ToLower().Contains(lowerCaseSearchFilter) &&
                    (!TagViewModel.Instance.HideDisabledTags || f.IsEnabled())).ToArray(); //? applies search filter
            }
        }
        #endregion

        #region Command Methods

        public void ToggleSortOption(TagSortType sortType)
        {
            // Toggle
            switch (sortType)
            {
                case TagSortType.Name:
                    TaggingUtil.SetActiveSortType(TagSortType.Name);
                    TaggingUtil.SetSortByNameDirection(!TaggingUtil.GetSortByNameDirection());

                    // the next time the following option is selected it'll default to one direction
                    TaggingUtil.SetSortByCountDirection(false);
                    break;

                case TagSortType.Count:
                    TaggingUtil.SetActiveSortType(TagSortType.Count);
                    TaggingUtil.SetSortByCountDirection(!TaggingUtil.GetSortByCountDirection());

                    // the next time the following option is selected it'll default to one direction
                    TaggingUtil.SetSortByNameDirection(false);
                    break;
            }

            // Re-Verify
            VerifyTagTabs();
        }

        /// <summary>
        /// Check all potential images for validity then select the valid images
        /// </summary>
        /// <returns></returns>
        public BaseImageModel[] SelectValidImages(TagSearchType searchType, TagModel[] selectedTags)
        {
            HashSet<BaseImageModel> validImages = new HashSet<BaseImageModel>(); //? we don't want the same image to appear twice, so we'll use a HashSet

            foreach (TagModel tag in selectedTags)
            {
                foreach (ImageModel image in tag.GetLinkedImages())
                {
                    bool validImage = true;

                    if (searchType == TagSearchType.Mandatory)
                    {
                        foreach (TagModel tagToCheck in selectedTags)
                        { 
                            if (!tagToCheck.ContainsLinkedImage(image)) validImage = false;

                            if (validImage == false) break; // if validity is falsified, no need to continue checking
                        }
                    }

                    if (validImage) 
                    {
                        if (!image.IsInRelatedImageSet)
                        {
                            validImages.Add(image); // if it's still a valid image by now we can add it
                        }
                        else
                        {
                            validImages.Add(image.ParentRelatedImageModel);
                        }
                    }
                }
            }

            return validImages.ToArray();
        }

        #endregion

        // ----- Operators -----
        // not the most secure but serviceable for now (categories should not be able to have the same name)
        //! do not use the auto-generated operators & equals, they will clash with the xaml and cause indescribable errors (you won't be pointed to the cause)
        public static bool operator ==(CategoryModel category1, CategoryModel category2)
        {
            return category1?._name == category2?._name;
        }

        public static bool operator !=(CategoryModel category1, CategoryModel category2)
        {
            return category1?._name != category2?._name;
        }

        protected bool Equals(CategoryModel other)
        {
            return _name == other._name;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((CategoryModel)obj);
        }

        public override int GetHashCode()
        {
            return (_name != null ? _name.GetHashCode() : 0);
        }
    }
}
