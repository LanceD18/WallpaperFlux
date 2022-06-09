using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Drawing;
using System.Dynamic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using WallpaperFlux.Core.Collections;
using WallpaperFlux.Core.Models;
using WallpaperFlux.Core.Models.Controls;
using WallpaperFlux.Core.Models.Tagging;
using WallpaperFlux.Core.Util;

namespace WallpaperFlux.Core.ViewModels
{
    public class TagViewModel : MvxViewModel
    {
        public static TagViewModel Instance; // allows the data to remain persistent without having to reload everything once the view is closed

        #region View Variables

        // Categories
        private MvxObservableCollection<CategoryModel> _categories = new MvxObservableCollection<CategoryModel>();
        public MvxObservableCollection<CategoryModel> Categories
        {
            get => _categories;
            set => SetProperty(ref _categories, value);
        }

        // Selected Category
        private CategoryModel _selectedCategory;
        public CategoryModel SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                //! Workaround to the EnsureSingularSelection() methods from WPF ControlUtil ; ideally we'd look for a less brute force solution
                SelectedCategory?.SelectedTagTab?.DeselectAllItems(); //? this is the previously selected tab as we are calling this before the value is set 
                //! Workaround to the EnsureSingularSelection() methods from WPF ControlUtil ; ideally we'd look for a less brute force solution

                SetProperty(ref _selectedCategory, value);

                HighlightTags(SelectedCategory);
                RaisePropertyChanged(() => CategoryIsSelected);
            }
        }

        private HashSet<TagModel> TagsToHighlight = new HashSet<TagModel>();


        #region ----- Filters -----
        private bool _tagAdderToggle;
        public bool TagAdderToggle
        {
            get => _tagAdderToggle;
            set
            {
                SetProperty(ref _tagAdderToggle, value);

                if (value)
                {
                    // toggles off the other toggles, we don't want both of them active at the same time
                    TagRemoverToggle = false;
                    TagLinkerToggle = false;
                    RaisePropertyChanged(() => TagRemoverToggle);
                    RaisePropertyChanged(() => TagLinkerToggle);
                }

                RaisePropertyChanged(() => WindowBorderThickness);
                RaisePropertyChanged(() => WindowBorderBrushColor);
                RaisePropertyChanged(() => EditingTagsOfAnImage);
                RaisePropertyChanged(() => EditingTagLinks);
            }
        }

        private bool _tagRemoverToggle;

        public bool TagRemoverToggle
        {
            get => _tagRemoverToggle;
            set
            {
                SetProperty(ref _tagRemoverToggle, value);

                if (value)
                {
                    // toggles off the other toggles, we don't want both of them active at the same time
                    TagAdderToggle = false;
                    TagLinkerToggle = false;
                    RaisePropertyChanged(() => TagAdderToggle);
                    RaisePropertyChanged(() => TagLinkerToggle);
                }

                RaisePropertyChanged(() => WindowBorderThickness);
                RaisePropertyChanged(() => WindowBorderBrushColor);
                RaisePropertyChanged(() => EditingTagsOfAnImage);
                RaisePropertyChanged(() => EditingTagLinks);
            }
        }

        private bool _tagLinkerToggle;
        public bool TagLinkerToggle
        {
            get => _tagLinkerToggle;
            set
            {
                SetProperty(ref _tagLinkerToggle, value);

                if (value)
                {
                    // toggles off the other toggles, we don't want both of them active at the same time
                    TagAdderToggle = false;
                    TagRemoverToggle = false;
                    RaisePropertyChanged(() => TagAdderToggle);
                    RaisePropertyChanged(() => TagRemoverToggle);

                    if (TagLinkingSource != null)
                    {
                        TaggingUtil.HighlightTags(TagLinkingSource.ParentChildTagsUnion_IncludeSelf()); // since the selected tag only changes when the tag-linker is off, we will highlight here
                    }
                }
                else
                {
                    // the linking source will not be set to null on turning off the linker, so we'll just send in an empty HashSet instead to un-highlight everything
                    TaggingUtil.HighlightTags(new HashSet<TagModel>());
                }

                RaisePropertyChanged(() => WindowBorderThickness);
                RaisePropertyChanged(() => WindowBorderBrushColor);
                RaisePropertyChanged(() => EditingTagsOfAnImage);
                RaisePropertyChanged(() => EditingTagLinks);
            }
        }

        private TagModel _tagLinkingSource;

        public TagModel TagLinkingSource
        {
            get => _tagLinkingSource;
            set
            {
                _tagLinkingSource = value;

                RaisePropertyChanged(() => CanUseTagLinker);
            }
        }

        public string TagLinkingSourceName => TagLinkingSource?.Name;

        public double WindowBorderThickness => (TagAdderToggle || TagRemoverToggle || TagLinkerToggle) ? 5 : 0;

        public Color WindowBorderBrushColor
        {
            get
            {
                if (TagAdderToggle) return Color.LimeGreen;
                if (TagRemoverToggle) return Color.Red;
                if (TagLinkerToggle) return Color.Yellow;

                return Color.Transparent;
            }
        }

        public bool EditingTagsOfAnImage => TagAdderToggle || TagRemoverToggle;

        public bool EditingTagLinks => TagLinkerToggle;

        #endregion

        #region ----- TagBoard -----
        private MvxObservableCollection<TagModel> _tagBoardTags = new MvxObservableCollection<TagModel>();

        public MvxObservableCollection<TagModel> TagBoardTags
        {
            get => _tagBoardTags;
            set => SetProperty(ref _tagBoardTags, value);
        }

        private double _tagBoardHeight;
        public double TagBoardHeight
        {
            get => _tagBoardHeight;
            set => SetProperty(ref _tagBoardHeight, value); //? needed to update the height when resizing the window
        }

        #endregion

        #endregion

        #region Enablers

        public bool CategoryIsSelected => SelectedCategory != null;

        public bool CategoriesExist => Categories.Count > 0;

        private bool _tagboardToggle;

        public bool TagboardToggle
        {
            get => _tagboardToggle;
            set => SetProperty(ref _tagboardToggle, value);
        }

        // need to also check if the tag-linking source is null for just in case the selected tag is deselected
        public bool CanUseTagLinker => SelectedCategory?.SelectedTagTab?.SelectedTag != null || TagLinkingSource != null;

        #endregion

        #region Commands

        public IMvxCommand AddCategoryCommand { get; set; }

        public IMvxCommand AddTagToSelectedCategoryCommand { get; set; }

        #region ----- TagBoard -----

        public IMvxCommand CloseTagBoardCommand { get; set; }

        public IMvxCommand SelectImagesFromTagBoardCommand { get; set; }

        public IMvxCommand SetMandatoryTagBoardSelectionCommand { get; set; }

        public IMvxCommand SetOptionalTagBoardSelectionCommand { get; set; }

        public IMvxCommand SetExcludedTagBoardSelectionCommand { get; set; }

        #endregion

        #endregion

        // TODO Add a ToolTip explaining how Category Order determines the order of image-naming
        public TagViewModel()
        {
            //? We will use this theme reference for categories so that tags and categories can be referenced outside of this control
            //! So do NOT add Category functionality here, give it to TaggingUtil
            Categories.SwitchTo(DataUtil.Theme.Categories);

            AddCategoryCommand = new MvxCommand(() =>
            {
                SelectedCategory = TaggingUtil.PromptAddCategory();

                RaisePropertyChanged(() => CategoriesExist);
                RaisePropertyChanged(() => SelectedCategory);
            });

            AddTagToSelectedCategoryCommand = new MvxCommand(() =>
            {
                TaggingUtil.PromptAddTagToCategory(SelectedCategory);

                if (SelectedCategory != null) { AddDebugTags(SelectedCategory); }
            });

            // TagBoard
            CloseTagBoardCommand = new MvxCommand(() => TagboardToggle = false); //? the open/toggle TagBoard is initially called by CategoryModel and sent to a method here
            SelectImagesFromTagBoardCommand = new MvxCommand(() => WallpaperFluxViewModel.Instance.RebuildImageSelector(SearchValidImagesWithTagBoard()));
            SetMandatoryTagBoardSelectionCommand = new MvxCommand(() => SetAllTagBoardTagsSearchType(TagSearchType.Mandatory));
            SetOptionalTagBoardSelectionCommand = new MvxCommand(() => SetAllTagBoardTagsSearchType(TagSearchType.Optional));
            SetExcludedTagBoardSelectionCommand = new MvxCommand(() => SetAllTagBoardTagsSearchType(TagSearchType.Excluded));

            TagBoardTags.CollectionChanged += TagBoardTagsOnCollectionChanged;
        }

        private void TagBoardTagsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null && e.NewItems.Count > 0)
            {
                // reset search type to default on re-adding tag
                foreach (TagModel tag in e.NewItems)
                {
                    tag.SearchType = TaggingUtil.DEFAULT_TAG_SEARCH_TYPE;
                }
            }
        }

        public void VerifyImagesOfCategories()
        {

        }

        #region Tag Highlighting
        public void SetTagsToHighlight(HashSet<TagModel> tags)
        {
            TagsToHighlight = new HashSet<TagModel>(tags);
            HighlightTags(SelectedCategory);
        }

        public void HighlightTags(CategoryModel category)
        {
            if (TagsToHighlight == null) return;
            if (category == null) return;

            HashSet<TagModel> tags = category.GetTags();
            
            foreach (TagModel tag in tags)
            {
                tag.IsHighlighted = TagsToHighlight.Contains(tag);

                if (TagLinkingSource != null)
                {
                    tag.IsHighlightedParent = TagLinkingSource.HasParent(tag);
                    tag.IsHighlightedChild = TagLinkingSource.HasChild(tag);
                }
            }
        }
        #endregion

        #region TagBoard

        public void ToggleTagBoard() => TagboardToggle = !TagboardToggle;

        public void AddTagsToTagBoard(TagModel[] tags) //! Range actions [ i.e., AddRange() ] are not supported for observable collections so we must do this manually
        {
            foreach (TagModel tag in tags)
            {
                if (!TagBoardTags.Contains(tag))
                {
                    TagBoardTags.Add(tag);
                }
            }
        }

        public void ClearTagBoardTags() => TagBoardTags.Clear();

        public void RemoveTagFromTagBoard(TagModel tag) => TagBoardTags.Remove(tag);

        public void SetTagBoardHeight(double newHeight) => TagBoardHeight = newHeight;

        /// <summary>
        /// Check all potential images for validity then select the valid images. Use the tag's search type for validity comparisons.
        /// </summary>
        /// <returns></returns>
        public ImageModel[] SearchValidImagesWithTagBoard()
        {
            HashSet<ImageModel> validImages = new HashSet<ImageModel>(); //? we don't want the same image to appear twice, so we'll use a HashSet

            foreach (TagModel tag in TagBoardTags)
            {
                foreach (ImageModel image in tag.GetLinkedImages())
                {
                    bool validImage = true;
                    foreach (TagModel tagToCheck in TagBoardTags)
                    {
                        if (tagToCheck.SearchType == TagSearchType.Optional) continue; // if optional, do nothing

                        switch (tagToCheck.SearchType)
                        {
                            case TagSearchType.Mandatory: // if mandatory, not containing this tag will set validity to false
                                if (!tagToCheck.ContainsLinkedImage(image)) validImage = false; 
                                break;

                            case TagSearchType.Excluded: // if excluded, containing this tag will set validity to false
                                if (tagToCheck.ContainsLinkedImage(image)) validImage = false;
                                break;
                        }

                        if (validImage == false) break; // if validity is falsified, no need to continue checking
                    }

                    if (validImage) validImages.Add(image); // if it's still a valid image by now we can add it
                }
            }

            return validImages.ToArray();
        }

        public void SetAllTagBoardTagsSearchType(TagSearchType searchType)
        {
            foreach (TagModel tag in TagBoardTags) tag.SearchType = searchType;
        }

        #endregion

        #region Command Methods

        // TODO REMOVE AT SOME POiNT
        //! temp debug code for generating a bunch of random tags
        private void AddDebugTags(CategoryModel category)
        {
            Debug.WriteLine("Adding Debug Tags...");
            string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
            Random charRand = new Random();
            Random intRand = new Random();

            int nextCount = intRand.Next(50, 100);

            TagModel[] tagsToAdd = new TagModel[nextCount];

            for (int i = 0; i < nextCount; i++)
            {
                int nextSize = intRand.Next(10, 15);
                char[] stringChars = new char[nextSize];

                for (int j = 0; j < stringChars.Length; j++)
                {
                    stringChars[j] = chars[charRand.Next(chars.Length)];
                }

                string finalString = new string(stringChars);
                tagsToAdd[i] = new TagModel(finalString, category);
            }

            category.AddTagRange(tagsToAdd);
        }
        //! temp debug code
        #endregion
    }
}