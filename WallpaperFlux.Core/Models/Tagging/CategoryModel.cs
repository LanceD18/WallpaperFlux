﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using HandyControl.Controls;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using Newtonsoft.Json;
using WallpaperFlux.Core.Models.Controls;
using WallpaperFlux.Core.Util;
using WallpaperFlux.Core.ViewModels;

namespace WallpaperFlux.Core.Models.Tagging
{
    public class CategoryModel : MvxNotifyPropertyChanged
    {
        private HashSet<TagModel> Tags = new HashSet<TagModel>();

        private string _name;
        public string Name
        {
            get => _name;

            set
            {
                /*x
                if (Tags != null)
                {
                    HashSet<string> alteredImages = new HashSet<string>();

                    foreach (TagData tag in Tags)
                    {
                        tag.ParentCategoryName = value;

                        foreach (string image in tag.GetLinkedImages())
                        {
                            //? while the HashSet itself prevents duplicates, this contains reference is also done fastest through HashSet
                            //? which the rename category section needs
                            if (!alteredImages.Contains(image))
                            {
                                WallpaperData.GetImageData(image).RenameCategory(name, value);
                                alteredImages.Add(image);
                            }
                        }
                    }
                }
                */

                SetProperty(ref _name, value);
            }
        }

        private bool _enabled;
        public bool Enabled
        {
            get => _enabled;

            set
            {
                _enabled = value;
                // TODO Hopefully you won't need all the extra code below and stuff can be handled more dynamically
                /*x
                if (_Enabled != value) // prevents unnecessary calls
                {
                    _Enabled = value;

                    foreach (TagData tag in Tags)
                    {
                        if (!WallpaperData.IsLoadingData)
                        {
                            WallpaperData.EvaluateImageActiveStates(tag.GetLinkedImages(), !value); // will forceDisable if the value is set to false
                        }
                    }
                }
                */
            }
        }

        private bool _useForNaming;

        public bool UseForNaming
        {
            get => _useForNaming;

            set
            {
                _useForNaming = value;
                /*x
                if (_UseForNaming != value) // prevents unnecessary calls | and yes this can happen
                {
                    _UseForNaming = value;

                    HashSet<WallpaperData.ImageData> imagesToRename = new HashSet<WallpaperData.ImageData>();
                    foreach (TagData tag in Tags)
                    {
                        foreach (string imagePath in tag.GetLinkedImages())
                        {
                            imagesToRename.Add(WallpaperData.GetImageData(imagePath));
                        }
                    }
                }
                */
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

                VerifyTagTabs();
            }
        }

        // Sorting
        public TagSortType ActiveSortType = TagSortType.Name;

        public bool SortByNameDirection { get; set; } = true; // default ascending option

        public bool SortByCountDirection { get; set; }

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
                SelectedTagTab?.DeselectAllItems(); //? this is the previously selected tab as we are calling this before the value is set
                //! Workaround to the EnsureSingularSelection() methods from WPF ControlUtil ; ideally we'd look for a less brute force solution

                SetProperty(ref _selectedTagTab, value);
                VerifyVisibleTags();
            }
        }

        private TagModel[] _sortedTags;
        private TagModel[] _filteredTags;

        #endregion

        #region View Variables

        [JsonIgnore] public string TagCountString => "Contains " + Tags.Count + " tag(s)";

        #endregion

        #region Commands

        [JsonIgnore] public IMvxCommand ToggleSortByNameCommand { get; set; }

        [JsonIgnore] public IMvxCommand ToggleSortByCountCommand { get; set; }

        [JsonIgnore] public IMvxCommand ViewTagBoardCommand { get; set; }

        [JsonIgnore] public IMvxCommand AddSelectedTagsToTagBoardCommand { get; set; }

        [JsonIgnore] public IMvxCommand ClearTagBoardCommand { get; set; }

        [JsonIgnore] public IMvxCommand RenameCategoryCommand { get; set; }

        [JsonIgnore] public IMvxCommand RemoveCategoryCommand { get; set; }

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
            ClearTagBoardCommand = new MvxCommand(() => TagViewModel.Instance.ClearTagBoardTags());

            RenameCategoryCommand = new MvxCommand(PromptRename);
            RemoveCategoryCommand = new MvxCommand(PromptRemoveCategory);
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

        #region Tag Control

        public TagModel AddTag(string tagName, bool useForNaming = true, bool enabled = true)
        {
            TagModel tag = new TagModel(tagName, this, useForNaming, enabled);
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

            if (displayWarning)
            {
                MessageBoxUtil.ShowError(errorCount <= 1 ? "The following tag already exists: " + existingTagWarning : "The following tags already exist: " + existingTagWarning);
            }

            RaisePropertyChanged(() => TagCountString);
            
            VerifyTagTabs(); //? Required for the addition and sorting to be visible right-away. All AddTag() methods should trace back to this method
        }

        public bool RemoveTag(TagModel tag)
        {
            tag.UnlinkAllImages();
            bool removed = Tags.Remove(tag);
            VerifyTagTabs(); // needed to visually update the tag's removal
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
        /// <returns></returns>
        public TagModel VerifyTag(string tagName, bool useForNaming = true, bool enabled = true)
        {
            if (!ContainsTag(tagName))
            {
                Debug.WriteLine("Tag " + tagName + " is missing, adding");
                // tag is missing, add it
                return AddTag(tagName, useForNaming, enabled);
            }
            else
            {
                TagModel tag = GetTag(tagName);

                //? In the context that this method is being used, in some cases the category will be added before these values are
                //? set (A tag is found as a parent to another tag first), but it will eventually reach this point
                tag.UseForNaming = useForNaming;
                tag.Enabled = enabled;

                return tag;
            }
        }
        #endregion

        #region Tag Tab Contol

        public TagModel[] GetSelectedTags() => Tags.Where((f) => f.IsSelected).ToArray();

        /// <summary>
        /// verifies the number of pages, tag sorting, and what tags are visible based on the given search
        /// </summary>
        public void VerifyTagTabs()
        {
            SortTags();

            if (string.IsNullOrEmpty(SearchFilter))
            {
                _filteredTags = _sortedTags.ToArray();
            }
            else
            {
                string lowerCaseSearchFilter = SearchFilter.ToLower();
                _filteredTags = _sortedTags.Where(f => f.Name.ToLower().Contains(lowerCaseSearchFilter)).ToArray(); //? applies search filter
            }

            int totalTagTabCount = (_filteredTags.Length / TaggingUtil.TagsPerPage) + 1; //? remember that this 'rounds' off the last page since it's an int

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

            VerifyVisibleTags();
        }
        
        /// <summary>
        /// verifies what tags are visible based on the given search
        /// </summary>
        public void VerifyVisibleTags()
        {
            if (SelectedTagTab == null)
            {
                Debug.WriteLine("Null Selected Tab");
                return;
            }

            if (_sortedTags == null)
            {
                Debug.WriteLine("Null Sorted Tags");
                SortTags();
            }

            if (string.IsNullOrEmpty(SearchFilter)) _filteredTags = _sortedTags.ToArray();

            int pageNumber = int.Parse(SelectedTagTab.TabIndex);
            int minIndex = TaggingUtil.TagsPerPage * (pageNumber - 1);
            int maxIndex = TaggingUtil.TagsPerPage * pageNumber;

            //xDebug.WriteLine("minIndex: " + minIndex + " | maxIndex: " + maxIndex);

            List<TagModel> pageTags = new List<TagModel>();
            for (int i = minIndex; i < maxIndex; i++)
            {
                //xDebug.WriteLine("i: " + i + " | filterLength: " + _filteredTags.Length);
                if (i > _filteredTags.Length - 1) break; // we're on the last page and we've run out of tags, break the loop to avoid an index error
                pageTags.Add(_filteredTags[i]);
            }

            SelectedTagTab.Items.SwitchTo(pageTags);
        }

        public void SortTags()
        {
            // Sort
            IEnumerable<TagModel> sortedItems = string.IsNullOrEmpty(SearchFilter)
                ? Tags.ToArray()
                : _filteredTags.ToArray();

            switch (ActiveSortType)
            {
                case TagSortType.Name:
                    sortedItems = SortByNameDirection
                        ? (from f in Tags orderby f.Name select f) // ascending
                        : (from f in Tags orderby f.Name descending select f);
                    break;

                case TagSortType.Count:
                    sortedItems = SortByCountDirection
                        ? (from f in Tags orderby f.GetLinkedImageCount() select f) // ascending
                        : (from f in Tags orderby f.GetLinkedImageCount() descending select f);
                    break;
            }

            _sortedTags = sortedItems.ToArray();
        }
        #endregion

        #region Command Methods

        public void ToggleSortOption(TagSortType sortType)
        {
            // Toggle
            switch (sortType)
            {
                case TagSortType.Name:
                    SortByNameDirection = !SortByNameDirection;

                    // the next time the following option is selected it'll default to one direction
                    SortByCountDirection = false;
                    break;

                case TagSortType.Count:
                    SortByCountDirection = !SortByCountDirection;

                    // the next time the following option is selected it'll default to one direction
                    SortByNameDirection = false;
                    break;
            }

            // Re-Verify
            VerifyTagTabs();
        }

        #endregion

        // ----- Operators -----
        public static bool operator ==(CategoryModel category1, CategoryModel category2)
        {
            return category1?.Name == category2?.Name;
        }

        public static bool operator !=(CategoryModel category1, CategoryModel category2)
        {
            return category1?.Name != category2?.Name;
        }
    }
}
