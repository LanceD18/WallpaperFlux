using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Drawing;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HandyControl.Tools.Extension;
using LanceTools;
using LanceTools.WPF.Adonis.Util;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using SkiaSharp;
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
                SelectedCategory?.VerifyTagTabs(); //! Do NOT call this from TagView.xaml.cs, after a few attempts an error will be thrown complaining about modifications during generation!

                HighlightTags();
                RaisePropertyChanged(() => CategoryIsSelected);
            }
        }

        public string SelectedTagName => SelectedTag?.Name;

        private MvxObservableCollection<TagModel> _visibleTags = new MvxObservableCollection<TagModel>();

        public MvxObservableCollection<TagModel> VisibleTags
        {
            get => _visibleTags;
            set => SetProperty(ref _visibleTags, value);
        }

        private TagModel _selectedTag;
        public TagModel SelectedTag
        {
            get => _selectedTag;
            set
            {
                SetProperty(ref _selectedTag, value);
                Instance.RaisePropertyChanged(() => Instance.CanUseTagLinker);
                Instance.RaisePropertyChanged(() => Instance.SelectedTagName);

                if (Instance.RankGraphToggle) // if the rank graph drawer is open, update the graph
                {
                    Instance.UpdateRankGraph();
                }

                //x // The selected tag will become the linking source when the linker is turned on, but shouldn't be modified while it is on
                //x if (!TaggingUtil.GetTagLinkerToggle()) TagViewModel.Instance.TagLinkingSource = value;
            }
        }

        private List<TagModel> previouslyHighlightedTags = new List<TagModel>();

        public double TagWrapWidth { get; set; }

        public double TagWrapHeight { get; set; }

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

                    TagLinkingSource = SelectedTag; // update the linking source to the currently selected tag on activating the tag linker
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

        private string _defaultConflictResolutionPathText;
        public string DefaultConflictResolutionPathText
        {
            get => _defaultConflictResolutionPathText;
            set => SetProperty(ref _defaultConflictResolutionPathText, value);
        } //? can't set this via a lambda because MVVM will break (might have something to do with the static source)

        #endregion

        #region --- Rank Graph ---

        public ColumnSeries<int> AllColumnSeries = new ColumnSeries<int>();
        public ColumnSeries<int> StaticColumnSeries = new ColumnSeries<int>();
        public ColumnSeries<int> GifColumnSeries = new ColumnSeries<int>();
        public ColumnSeries<int> VideoColumnSeries = new ColumnSeries<int>();

        public ISeries[] RankSeries { get; set; }

        #endregion

        #endregion

        #endregion

        #region Enablers & Toggles

        public bool CategoryIsSelected => SelectedCategory != null;

        public bool CategoriesExist => Categories.Count > 0;

        // need to also check if the tag-linking source is null for just in case the selected tag is deselected
        public bool CanUseTagLinker => SelectedTag != null || TagLinkingSource != null;

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

        private bool _hideDisabledTags;
        public bool HideDisabledTags
        {
            get => _hideDisabledTags;
            set
            {
                SetProperty(ref _hideDisabledTags, value);

                SelectedCategory?.VerifyTagTabs();
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

        #region Rank Graph

        private bool _rankGraphToggle;
        public bool RankGraphToggle
        {
            get => _rankGraphToggle;
            set
            {
                SetProperty(ref _rankGraphToggle, value);

                if (value) UpdateRankGraph();
            }
        }


        private bool _allColumnToggle = true;
        public bool AllColumnToggle
        {
            get => _allColumnToggle;
            set
            {
                SetProperty(ref _allColumnToggle, value);
                AllColumnSeries.IsVisible = value;
            }
        }

        private bool _staticColumnToggle;
        public bool StaticColumnToggle
        {
            get => _staticColumnToggle;
            set
            {
                SetProperty(ref _staticColumnToggle, value);
                StaticColumnSeries.IsVisible = value;
            }
        }

        private bool _gifColumnToggle;
        public bool GifColumnToggle
        {
            get => _gifColumnToggle;
            set
            {
                SetProperty(ref _gifColumnToggle, value);
                GifColumnSeries.IsVisible = value;
            }
        }

        private bool _videoColumnToggle;
        public bool VideoColumnToggle
        {
            get => _videoColumnToggle;
            set
            {
                SetProperty(ref _videoColumnToggle, value);
                VideoColumnSeries.IsVisible = value;
            }
        }

        #endregion

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

        public IMvxCommand AssignDefaultResolutionCommand { get; set; }

        public IMvxCommand RemoveDefaultResolutionCommand { get; set; }

        #endregion

        #region --- RankGraph ---

        public IMvxCommand CloseRankGraphCommand { get; set; }

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
            //? --- TagBoard ---
            CloseTagBoardCommand = new MvxCommand(CloseTagBoard); //? the open/toggle TagBoard is initially called by CategoryModel and sent to a method here
            SelectImagesFromTagBoardCommand = new MvxCommand(() => RebuildImageSelectorWithTagOptions(SearchValidImagesWithTagBoard()));
            SetMandatoryTagBoardSelectionCommand = new MvxCommand(() => SetAllTagBoardTagsSearchType(TagSearchType.Mandatory));
            SetOptionalTagBoardSelectionCommand = new MvxCommand(() => SetAllTagBoardTagsSearchType(TagSearchType.Optional));
            SetExcludedTagBoardSelectionCommand = new MvxCommand(() => SetAllTagBoardTagsSearchType(TagSearchType.Excluded));

            TagBoardTags.CollectionChanged += TagBoardTagsOnCollectionChanged;

            //? --- Folder Priority ---
            /*x
            if (ThemeUtil.Theme.PreLoadedFolderPriorities != null) // only access this if a load has been processed
            {
                RebuildFolderPriorities(ThemeUtil.Theme.PreLoadedFolderPriorities);
            }
            */

            if (Directory.Exists(TaggingUtil.DefaultConflictResolutionPath)) //? remember, this value will be "" in new themes
            {
                DefaultConflictResolutionPathText = new DirectoryInfo(TaggingUtil.DefaultConflictResolutionPath).Name;
            }

            ViewFolderPriorityCommand = new MvxCommand(ToggleFolderPriority);
            CloseFolderPriorityCommand = new MvxCommand(CloseFolderPriority);
            CreatePriorityCommand = new MvxCommand(CreatePriority);
            DeleteSelectedPrioritiesCommand = new MvxCommand(DeleteSelectedPriorities);

            AssignDefaultResolutionCommand = new MvxCommand(() =>
            {
                string path = FolderUtil.PromptValidFolderPath();
                if (!string.IsNullOrEmpty(path))
                {
                    TaggingUtil.DefaultConflictResolutionPath = path;
                }
            });

            RemoveDefaultResolutionCommand = new MvxCommand(() => TaggingUtil.DefaultConflictResolutionPath = string.Empty);

            //? --- Rank Graph ---

            AllColumnSeries.Name = "All";
            StaticColumnSeries.Name = "Static";
            GifColumnSeries.Name = "GIF";
            VideoColumnSeries.Name = "Video";

            AllColumnSeries.IsVisible = AllColumnToggle;
            StaticColumnSeries.IsVisible = StaticColumnToggle;
            GifColumnSeries.IsVisible = GifColumnToggle;
            VideoColumnSeries.IsVisible = VideoColumnToggle;

            RankSeries = new ISeries[]
            {
                AllColumnSeries,
                StaticColumnSeries,
                GifColumnSeries,
                VideoColumnSeries
            };

            CloseRankGraphCommand = new MvxCommand(CloseRankGraph);
        }

        /// <summary>
        /// Rebuilds the image selector using the randomization and reversal options of the TagView
        /// </summary>
        /// <param name="images">The images to rebuild the image selector with</param>
        public void RebuildImageSelectorWithTagOptions(ImageModel[] images) => WallpaperFluxViewModel.Instance.RebuildImageSelector(images, RandomizeSelection, ReverseSelection);

        #region Visible Tags

        public void SetTagWrapSize(double width, double height)
        {
            TagWrapWidth = width;
            TagWrapHeight = height; // the bottom tends to be cut off
            RaisePropertyChanged(() => TagWrapWidth);
            RaisePropertyChanged(() => TagWrapHeight);
        }


        /// <summary>
        /// verifies what tags are visible based on the given search
        /// </summary>
        public void VerifyVisibleTags()
        {
            if (SelectedCategory == null)
            {
                Debug.WriteLine("Null Selected Category");
                return;
            }

            if (SelectedCategory.SelectedTagTab == null)
            {
                Debug.WriteLine("Null Selected Tab");
                return;
            }

            if (SelectedCategory.SortedTags == null)
            {
                Debug.WriteLine("Null Sorted Tags");
                SelectedCategory.SortTags();
            }

            TagModel nullTag = new TagModel(string.Empty, null) { IsHidden = true };
            // adjust the indexes to the page tag limit
            while (VisibleTags.Count < TaggingUtil.TagsPerPage /*x&& VisibleTags.Count < SelectedCategory.FilteredTags.Length*/)
            {
                // hiding instead of setting to null since removing and adding controls adds significant processing time
                VisibleTags.Add(nullTag);
            }
            
            while (VisibleTags.Count > TaggingUtil.TagsPerPage)
            {
                VisibleTags.Remove(VisibleTags.Last());
            }

            // adjust the indexes to the page tag limit
            // for when searching
            if (SelectedCategory.FilteredTags.Length < TaggingUtil.TagsPerPage)
            {
                for (int i = TaggingUtil.TagsPerPage - 1; i >= SelectedCategory.FilteredTags.Length; i--)
                {
                    VisibleTags[i] = nullTag; //! do NOT directly set a tag to IsHidden here, this does not remove the reference to the original tag and can cause duplicates!
                }
            }

            SelectedCategory.FilteredTags = SelectedCategory.GetSortedTagsWithSearchFilter();

            int pageNumber = int.Parse(SelectedCategory.SelectedTagTab.TabIndex);
            int minIndex = TaggingUtil.TagsPerPage * (pageNumber - 1);
            int maxIndex = TaggingUtil.TagsPerPage * pageNumber;

            for (int i = minIndex; i < maxIndex; i++)
            {
                int pageIndex = i - minIndex;
                if (i > SelectedCategory.FilteredTags.Length - 1) // we're on the last page and we've run out of tags, fill the rest with null tags to hide them
                {
                    VisibleTags[pageIndex] = nullTag;
                    continue;
                }

                if (HideDisabledTags && !SelectedCategory.FilteredTags[i].IsEnabled())
                {
                    maxIndex++; // this tag is hidden, skip it, we now have space for an additional tag so increase the max index
                    continue;
                }

                VisibleTags[pageIndex] = SelectedCategory.FilteredTags[i];
                VisibleTags[pageIndex].IsHidden = false;
            }
            
            SelectedCategory.SelectedTagTab.Items.SwitchTo(VisibleTags); // TODO should probably remove this duplication at some point

            //? prevents tags from remaining selected out of view whenever we search or change the sort option
            //? and unlike images, tags already deselect on every page swap
            foreach (TagModel tag in SelectedCategory.GetSelectedTags())
            {
                if (!VisibleTags.Contains(tag))
                {
                    tag.IsSelected = false;
                }
            }

            HighlightTags();
        }
        #endregion

        #region Tag Highlighting

        private Thread highlightTagThread;
        public async void HighlightTags()
        {
            if (JsonUtil.IsLoadingData) return;
            if (FolderUtil.IsValidatingFolders) return;
            if (SelectedCategory == null) return; // can't show any highlights or unhighlight anything if a category isn't selected
            if (VisibleTags == null) return;

            HashSet<TagModel> visibleTags = new HashSet<TagModel>(VisibleTags);

            await Task.Run(() =>
            {
                Debug.WriteLine("Highlighting tags...");

                //? IsHighlightedInSomeImages is only set to true so we need to revert it back to false at some point
                foreach (TagModel tag in previouslyHighlightedTags) tag.IsHighlightedInSomeImages = false;

                HashSet<TagModel> tagsToHighlight = new HashSet<TagModel>();

                if (TagLinkerToggle) // Linking has priority over image selection
                {
                    tagsToHighlight = new HashSet<TagModel>(TagLinkingSource.GetParentChildTagsUnion_IncludeSelf());
                }
                else if (WallpaperFluxViewModel.Instance.SelectedImageCount == 0 && !TagLinkerToggle) // unhighlight all
                {
                    tagsToHighlight = new HashSet<TagModel>();
                }
                else if (WallpaperFluxViewModel.Instance.SelectedImage != null && WallpaperFluxViewModel.Instance.SelectedImage.IsSelected) // image(s) selected
                {
                    //? if multiple images are selected, only highlight tags that all images have
                    ImageModel[] selectedImages = WallpaperFluxViewModel.Instance.GetAllHighlightedImages();

                    if (selectedImages.Length == 0) // no images actually selected, erroneous case
                    {
                        // TODO Theoretically, you should never reach this statement, double check this case
                        tagsToHighlight = new HashSet<TagModel>(); // empty list
                    }
                    if (selectedImages.Length == 1) // highlight tags of 1 image
                    {
                        ImageModel targetImage = selectedImages[0];
                        //! Using the SelectedImage from WallpaperFluxViewModel can cause null reference crashes due to the multi-threading used
                        try
                        {
                            if (targetImage != null)
                            {
                                tagsToHighlight = new HashSet<TagModel>(targetImage.Tags.GetTags());
                            }
                        }
                        catch (Exception e)
                        {
                            // async fail, the image doesn't exist but got past the null check, just make tagsToHighlight empty
                            tagsToHighlight = new HashSet<TagModel>();
                        }
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
                                if (!tag.IsHighlightedInSomeImages && !image.ContainsTag(tag))
                                {
                                    tag.IsHighlightedInSomeImages = true; //? we only want to set this once, otherwise later images can overwrite this back to false
                                }
                            }
                        }
                    }
                }

                //xHashSet<TagModel> visibleTags = SelectedCategory.SelectedTagTab.Items.ToHashSet();

                //? we are using new HashSet<TagModel>(VisibleTags) to avoid a collection modified error, remember that this is in a thread
                foreach (TagModel tag in visibleTags)
                {
                    tag.IsHighlighted = tagsToHighlight.Contains(tag);

                    // handle tag control highlights
                    if (TagLinkingSource != null)
                    {
                        tag.IsHighlightedInSomeImages = false; // this doesn't apply to this scenario so turn it off, unlike the selected images scenario it won't be automatically turned off
                        tag.IsHighlightedParent = TagLinkingSource.HasParent(tag);
                        tag.IsHighlightedChild = TagLinkingSource.HasChild(tag);
                    }
                    else // doesn't apply to this scenario, ensures that the highlights were turned off so that if we say, select an image, the parent & child highlights won't remain
                    {
                        tag.IsHighlightedParent = false;
                        tag.IsHighlightedChild = false;
                    }
                }

                previouslyHighlightedTags = new List<TagModel>(tagsToHighlight);
            }).ConfigureAwait(false);
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

        public void ClearTagBoard() => TagBoardTags.Clear();

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

        public void CloseTagBoard()
        {
            TagboardToggle = false;
        }

        #endregion

        #region Folder Priority

        public void ToggleFolderPriority() => FolderPriorityToggle = !FolderPriorityToggle;

        public FolderPriorityModel[] GetSelectedFolderPriorities() => FolderPriorities.Where(f => f.IsSelected).ToArray();

        public void RebuildFolderPriorities(SimplifiedFolderPriority[] newFolderPriorities)
        {
            FolderPriorities = new MvxObservableCollection<FolderPriorityModel>();

            foreach (SimplifiedFolderPriority priority in newFolderPriorities)
            {
                FolderPriorities.Add(new FolderPriorityModel(priority.Name, priority.ConflictResolutionFolder, priority.PriorityOverride));
            }
        }

        public bool ContainsFolderPriority(string name)
        {
            foreach (FolderPriorityModel priority in FolderPriorities)
            {
                if (name == priority.Name)
                {
                    return true;
                }
            }

            return false;
        }

        public void CloseFolderPriority()
        {
            FolderPriorityToggle = false;
        }

        #endregion

        #region RankGraph

        public void UpdateRankGraph()
        {
            List<int> allValues = new List<int>();
            List<int> staticValues = new List<int>();
            List<int> gifValues = new List<int>();
            List<int> videoValues = new List<int>();

            // we need a buffer value for rank 0 since we aren't actually displaying the un-ranked images
            // without this buffer, the graph will always place the bars 1 value off
            allValues.Add(0);
            staticValues.Add(0);
            gifValues.Add(0);
            videoValues.Add(0);

            for (int i = 1; i <= ThemeUtil.ThemeSettings.MaxRank; i++) //? not including un-ranked, those will take over the majority of the graph
            {
                allValues.Add(ThemeUtil.RankController.GetRankCountOfTag(i, SelectedTag));
                staticValues.Add(ThemeUtil.RankController.GetImagesOfTypeRankCountOfTag(ImageType.Static, i, SelectedTag));
                gifValues.Add(ThemeUtil.RankController.GetImagesOfTypeRankCountOfTag(ImageType.GIF, i, SelectedTag));
                videoValues.Add(ThemeUtil.RankController.GetImagesOfTypeRankCountOfTag(ImageType.Video, i, SelectedTag));
            }

            AllColumnSeries.Values = allValues;
            StaticColumnSeries.Values = staticValues;
            GifColumnSeries.Values = gifValues;
            VideoColumnSeries.Values = videoValues;

            //xRankColumnSeries.Fill = new SolidColorPaint(SKColors.Blue);
            StaticColumnSeries.Fill = new SolidColorPaint(SKColors.SlateBlue);
            GifColumnSeries.Fill = new SolidColorPaint(SKColors.LimeGreen);
            VideoColumnSeries.Fill = new SolidColorPaint(SKColors.OrangeRed);
        }

        public void ToggleRankGraph() => RankGraphToggle = !RankGraphToggle;

        public void CloseRankGraph() => RankGraphToggle = false;

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

            if (ContainsFolderPriority(priorityName))
            {
                MessageBoxUtil.ShowError("The priority [" + priorityName + "] already exists");
                return;
            }

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