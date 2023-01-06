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
using HandyControl.Tools.Extension;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using WallpaperFlux.Core.Collections;
using WallpaperFlux.Core.JSON;
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

                //? needed to ensure that we can see the tags on swapping to a category
                SelectedCategory.VerifyTagTabs(); //! Do NOT call this from TagView.xaml.cs, after a few attempts an error will be thrown complaining about modifications during generation!

                HighlightTags();
                RaisePropertyChanged(() => CategoryIsSelected);
            }
        }

        private List<TagModel> previouslyHighlightedTags = new List<TagModel>();

        #region ----- Filters [Tag-Adder & Tag-Linker] -----
        //? utilizes the TagInteractCommand from TagModel to function
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
                    TagLinkerToggle = false;
                    RaisePropertyChanged(() => TagLinkerToggle);
                }

                RaisePropertyChanged(() => WindowBorderThickness);
                RaisePropertyChanged(() => WindowBorderBrushColor);
                RaisePropertyChanged(() => EditingTags);
                RaisePropertyChanged(() => EditingTagsText);
            }
        }

        //? utilizes the TagInteractCommand from TagModel to function
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
                    RaisePropertyChanged(() => TagAdderToggle);

                    TagLinkingSource = SelectedCategory.SelectedTagTab.SelectedTag; // update the linking source to the currently selected tag on activating the tag linker
                    //xTaggingUtil.HighlightTags(TagLinkingSource.ParentChildTagsUnion_IncludeSelf()); // since the selected tag only changes when the tag-linker is off, we will highlight here
                }
                else
                {
                    //? un-highlighting everything
                    //! what happens if this is called while an image is selected?
                    TagLinkingSource = null;
                    //xTaggingUtil.HighlightTags(new HashSet<TagModel>());
                }

                RaisePropertyChanged(() => WindowBorderThickness);
                RaisePropertyChanged(() => WindowBorderBrushColor);
                RaisePropertyChanged(() => EditingTags);
                RaisePropertyChanged(() => EditingTagsText);
            }
        }

        private TagModel _tagLinkingSource;

        public TagModel TagLinkingSource
        {
            get => _tagLinkingSource;
            set
            {
                _tagLinkingSource = value;

                HighlightTags();
                RaisePropertyChanged(() => CanUseTagLinker);
            }
        }

        public double WindowBorderThickness => (TagAdderToggle || TagLinkerToggle) ? 5 : 0;

        public Color WindowBorderBrushColor
        {
            get
            {
                if (TagAdderToggle) return Color.LimeGreen;
                if (TagLinkerToggle) return Color.Yellow;

                return Color.Transparent;
            }
        }

        public bool EditingTags => TagAdderToggle || TagLinkerToggle;

        public string EditingTagsText
        {
            get
            {
                if (TagAdderToggle)
                    return "Select a tag to add or remove from the currently highlighted image(s)";

                if (TagLinkerToggle)
                    return "Editing linked tags of the tag: " + TagLinkingSource?.Name + " (Select an non-child tag to link or unlink as a parent to the active tag)";

                return "";
            }
        }

        #endregion

        #region ----- Drawers -----

        #region --- TagBoard ---
        private MvxObservableCollection<TagModel> _tagBoardTags = new MvxObservableCollection<TagModel>();

        public MvxObservableCollection<TagModel> TagBoardTags
        {
            get => _tagBoardTags;
            set => SetProperty(ref _tagBoardTags, value);
        }

        private double _drawerHeight;
        public double DrawerHeight
        {
            get => _drawerHeight;
            set => SetProperty(ref _drawerHeight, value); //? needed to update the height when resizing the window
        }

        #endregion

        #region --- FolderPriority ---

        private MvxObservableCollection<FolderPriorityModel> _folderPriorities = new MvxObservableCollection<FolderPriorityModel>();

        public MvxObservableCollection<FolderPriorityModel> FolderPriorities
        {
            get => _folderPriorities;
            set => SetProperty(ref _folderPriorities, value);
        }

        private FolderPriorityModel _selectedFolderPriority;

        public FolderPriorityModel SelectedFolderPriority
        {
            get => _selectedFolderPriority;
            set
            {
                SetProperty(ref _selectedFolderPriority, value);
                RaisePropertyChanged(() => CanDeletePriorities);
            }
        }

        #endregion

        #endregion

        #endregion

        #region Enablers & Toggles

        public bool CategoryIsSelected => SelectedCategory != null;

        public bool CategoriesExist => Categories.Count > 0;

        // need to also check if the tag-linking source is null for just in case the selected tag is deselected
        public bool CanUseTagLinker => SelectedCategory?.SelectedTagTab?.SelectedTag != null || TagLinkingSource != null;

        private bool _randomizeSelection;
        public bool RandomizeSelection
        {
            get => _randomizeSelection;
            set
            {
                SetProperty(ref _randomizeSelection, value);

                if (value) ReverseSelection = false; //? you cannot randomize and reverse the selection at the same time
            }
        }

        private bool _reverseSelection;
        public bool ReverseSelection
        {
            get => _reverseSelection;
            set
            {
                SetProperty(ref _reverseSelection, value);

                if (value) RandomizeSelection = false; //? you cannot randomize and reverse the selection at the same time
            }
        }

        // --- Drawers ---
        private bool _tagboardToggle;

        public bool TagboardToggle
        {
            get => _tagboardToggle;
            set => SetProperty(ref _tagboardToggle, value);
        }

        private bool _folderPriorityToggle;

        public bool FolderPriorityToggle
        {
            get => _folderPriorityToggle;
            set => SetProperty(ref _folderPriorityToggle, value);
        }

        public bool CanDeletePriorities => SelectedFolderPriority != null;
        #endregion

        #region Commands

        public IMvxCommand AddCategoryCommand { get; set; }

        public IMvxCommand AddTagToSelectedCategoryCommand { get; set; }

        #region ----- Drawers -----

        #region --- TagBoard ---

        public IMvxCommand CloseTagBoardCommand { get; set; }

        public IMvxCommand SelectImagesFromTagBoardCommand { get; set; }

        public IMvxCommand SetMandatoryTagBoardSelectionCommand { get; set; }

        public IMvxCommand SetOptionalTagBoardSelectionCommand { get; set; }

        public IMvxCommand SetExcludedTagBoardSelectionCommand { get; set; }

        #endregion

        #region --- FolderPriority ---

        public IMvxCommand ViewFolderPriorityCommand { get; set; }

        public IMvxCommand CloseFolderPriorityCommand { get; set; }

        public IMvxCommand CreatePriorityCommand { get; set; }

        public IMvxCommand DeleteSelectedPrioritiesCommand { get; set; }

        #endregion

        #endregion

        #endregion

        // TODO Add a ToolTip explaining how Category Order determines the order of image-naming
        public TagViewModel()
        {
            //? We will use this theme reference for categories so that tags and categories can be referenced outside of this control
            //! So do NOT add Category functionality here, give it to TaggingUtil
            Categories.SwitchTo(ThemeUtil.Theme.Categories);

            AddCategoryCommand = new MvxCommand(() =>
            {
                SelectedCategory = TaggingUtil.PromptAddCategory();

                RaisePropertyChanged(() => CategoriesExist);
                RaisePropertyChanged(() => SelectedCategory);
            });

            //? If the category's use for naming state is disabled, the tag should reference that when needed instead of changing its own UseForNaming parameter
            AddTagToSelectedCategoryCommand = new MvxCommand(() => TaggingUtil.PromptAddTagToCategory(SelectedCategory));

            InitDrawers();
        }

        public void InitDrawers()
        {
            // --- TagBoard ---
            CloseTagBoardCommand = new MvxCommand(() => TagboardToggle = false); //? the open/toggle TagBoard is initially called by CategoryModel and sent to a method here
            SelectImagesFromTagBoardCommand = new MvxCommand(() => RebuildImageSelector(SearchValidImagesWithTagBoard()));
            SetMandatoryTagBoardSelectionCommand = new MvxCommand(() => SetAllTagBoardTagsSearchType(TagSearchType.Mandatory));
            SetOptionalTagBoardSelectionCommand = new MvxCommand(() => SetAllTagBoardTagsSearchType(TagSearchType.Optional));
            SetExcludedTagBoardSelectionCommand = new MvxCommand(() => SetAllTagBoardTagsSearchType(TagSearchType.Excluded));

            TagBoardTags.CollectionChanged += TagBoardTagsOnCollectionChanged;

            // --- Folder Priority ---
            ViewFolderPriorityCommand = new MvxCommand(ToggleFolderPriority);
            CloseFolderPriorityCommand = new MvxCommand(() => FolderPriorityToggle = false);
            CreatePriorityCommand = new MvxCommand(CreatePriority);
            DeleteSelectedPrioritiesCommand = new MvxCommand(DeleteSelectedPriorities);

            if (ThemeUtil.Theme.PreLoadedFolderPriorities != null) // only access this if a load has been processed
            {
                RebuildFolderPriorities(ThemeUtil.Theme.PreLoadedFolderPriorities);
            }
        }

        /// <summary>
        /// Rebuilds the image selector using the randomization and reversal options of the TagView
        /// </summary>
        /// <param name="images">The images to rebuild the image selector with</param>
        public void RebuildImageSelector(ImageModel[] images) => WallpaperFluxViewModel.Instance.RebuildImageSelector(images, RandomizeSelection, ReverseSelection);

        #region Tag Highlighting

        public void HighlightTags()
        {
            if (JsonUtil.IsLoadingData) return;

            Debug.WriteLine("Highlighting tags...");
            foreach (TagModel tag in previouslyHighlightedTags) tag.IsHighlightedInSomeImages = false; //? this should be undone on each re-run in one way or another

            if (WallpaperFluxViewModel.Instance.SelectedImage != null && !WallpaperFluxViewModel.Instance.SelectedImage.IsSelected
                                                                      && !TagLinkerToggle) return; // one of these need to be true to highlight anything
            if (SelectedCategory == null) return;

            HashSet<TagModel> tagsToHighlight = new HashSet<TagModel>();

            if (TagLinkerToggle) // has priority of if an image is selected
            {
                tagsToHighlight = new HashSet<TagModel>(TagLinkingSource.GetParentChildTagsUnion_IncludeSelf());
            }
            else if (WallpaperFluxViewModel.Instance.SelectedImage != null && WallpaperFluxViewModel.Instance.SelectedImage.IsSelected)
            {
                //? if multiple images are selected, only highlight tags that all images have
                ImageModel[] selectedImages = WallpaperFluxViewModel.Instance.GetAllHighlightedImages();

                if (selectedImages.Length <= 1) // highlight tags of 1 image
                {
                    tagsToHighlight = new HashSet<TagModel>(WallpaperFluxViewModel.Instance.SelectedImage.Tags.GetTags());
                }
                else // highlight tags of multiple images
                {
                    tagsToHighlight = new HashSet<TagModel>(); //? duplicate tags will be filtered out by HashSet
                    foreach (ImageModel image in selectedImages)
                    {
                        tagsToHighlight.UnionWith(image.Tags.GetTags());
                    }

                    foreach (ImageModel image in selectedImages)
                    {
                        // check for tags that do not exist in an image's tag-list, if so, make the tag classified as highlighted in only *some* images
                        foreach (TagModel tag in tagsToHighlight)
                        {
                            if (!image.ContainsTag(tag))
                            {
                                tag.IsHighlightedInSomeImages = true;
                            }
                        }
                    }
                }
            }

            HashSet<TagModel> activeTags = SelectedCategory.GetTags();

            foreach (TagModel tag in activeTags)
            {
                tag.IsHighlighted = tagsToHighlight.Contains(tag);

                if (TagLinkingSource != null)
                {
                    tag.IsHighlightedInSomeImages = false; // this doesn't apply to this scenario so turn it off, unlike the selected images scenario it won't be automatically turned off
                    tag.IsHighlightedParent = TagLinkingSource.HasParent(tag);
                    tag.IsHighlightedChild = TagLinkingSource.HasChild(tag);
                }
                else // ensures that the highlights were turned off so that if we say, select an image, the parent & child highlights won't remain
                {
                    tag.IsHighlightedParent = false;
                    tag.IsHighlightedChild = false;
                }
            }

            previouslyHighlightedTags = new List<TagModel>(tagsToHighlight);
        }

        #endregion

        #region --- Drawers ---

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

        public void SetDrawerHeight(double newHeight) => DrawerHeight = newHeight;

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

        #endregion

        #region Folder Priority

        public void ToggleFolderPriority() => FolderPriorityToggle = !FolderPriorityToggle;

        public FolderPriorityModel[] GetSelectedFolderPriorities() => FolderPriorities.Where(f => f.IsSelected).ToArray();

        /// <summary>
        /// Return the winning priority
        /// </summary>
        /// <param name="folderA"></param>
        /// <param name="folderB"></param>
        /// <returns></returns>
        public string CompareFolderPriorities(string folderA, string folderB)
        {
            FolderModel folderModelA = FolderUtil.GetFolderModel(folderA);
            FolderModel folderModelB = FolderUtil.GetFolderModel(folderB);

            // give a significantly lower value if no folder model is given, allowing one of the two options to be forcefully picked
            int priorityA = folderModelA == null ? -10 : folderModelA.PriorityIndex;
            int priorityB = folderModelB == null ? -10 : folderModelB.PriorityIndex;

            if (priorityB > priorityA) // higher priority folder found
            {
                return folderB;
            }

            if (priorityB == priorityA) // conflict resolution needed
            {
                if (priorityB == -1 || priorityB == -10) return string.Empty; // if there is no priority, return the empty string

                return FolderPriorities[priorityA].ConflictResolutionFolder;
            }

            return folderA;
        }

        public void RebuildFolderPriorities(SimplifiedFolderPriority[] newFolderPriorities)
        {
            FolderPriorities = new MvxObservableCollection<FolderPriorityModel>();

            foreach (SimplifiedFolderPriority priority in newFolderPriorities)
            {
                FolderPriorities.Add(new FolderPriorityModel(priority.Name, priority.ConflictResolutionFolder));
            }
        }

        #endregion

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

        private void CreatePriority()
        {
            string priorityName = MessageBoxUtil.GetString("Folder Priority Name", "Give a name for your priority", "Priority name...");

            if (priorityName != "") FolderPriorities.Add(new FolderPriorityModel(priorityName));
        }
        private void DeleteSelectedPriorities()
        {
            FolderPriorityModel[] priorities = GetSelectedFolderPriorities();

            foreach (FolderPriorityModel priority in priorities) priority.ClearFolders(); // set all affected folders to the default priority value

            FolderPriorities.RemoveItems(GetSelectedFolderPriorities());
        }
        #endregion
    }
}