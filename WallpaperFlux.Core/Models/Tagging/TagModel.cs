﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LanceTools;
using LanceTools.IO;
using LanceTools.WPF.Adonis.Util;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using Newtonsoft.Json;
using WallpaperFlux.Core.Collections;
using WallpaperFlux.Core.Models.Controls;
using WallpaperFlux.Core.Util;
using WallpaperFlux.Core.ViewModels;

namespace WallpaperFlux.Core.Models.Tagging
{
    public class TagModel : ListBoxItemModel
    {
        #region Core
        public string Name { get; private set; }

        private bool _enabled = true;

        public bool Enabled
        {
            get => _enabled;

            set
            {
                SetProperty(ref _enabled, value);
                UpdateLinkedImagesEnabledState();

                if (!JsonUtil.IsLoadingData && TagViewModel.Instance.HideDisabledTags) TagViewModel.Instance.VerifyVisibleTags();
            }
        }

        private bool _useForNaming = true;

        public bool UseForNaming
        {
            get => _useForNaming;

            set
            {
                SetProperty(ref _useForNaming, value);
                RaisePropertyChanged(() => UseForNaming_IncludeCategory);
                RaisePropertyChanged(() => ExceptionColor); //? depends on the value of UseForNaming_IncludeCategory
            }
        }


        //? Just create a TagModelJSON.cs that converts all of these into strings on saving the theme
        //! Using a string will require us to update this on rename, and search for the tag when needed, so lets just save the string portion for when saving
        private HashSet<TagModel> ParentTags = new HashSet<TagModel>(); //? Will be converted to strings in TagModelJson.cs for saving purposes instead of saving the entire object
        private HashSet<TagModel> ChildTags = new HashSet<TagModel>(); //? Will be converted to strings in TagModelJson.cs for saving purposes instead of saving the entire object

        private string _renameFolderPath = string.Empty;
        public string RenameFolderPath
        {
            get => _renameFolderPath;
            set
            {
                if (Directory.Exists(value))
                {
                    SetProperty(ref _renameFolderPath, value);
                    RaisePropertyChanged(() => RenameFolderContextMenuString);
                }
                else
                {
                    SetProperty(ref _renameFolderPath, string.Empty);
                    RaisePropertyChanged(() => RenameFolderContextMenuString);
                }
            }
        } // the folder images of this tag will be assigned to when renamed (if the priority of this folder is high enough)

        //! Using a string will require us to update this on rename, and search for the category when needed, and this isn't even being saved so lets just avoid the hassle
        public CategoryModel ParentCategory;

        //? We are ignoring this since these should get implemented on loading in the images through their TagCollection
        private HashSet<ImageModel> LinkedImages = new HashSet<ImageModel>(); //? Will be converted to strings in TagModelJson.cs for saving purposes instead of saving the entire object

        #endregion

        #region View Variables

        public int LinkedImageCount { get; private set; }

        public string ImageCountStringTag => "(" + LinkedImageCount + ")";

        public string ImageCountStringContextMenu => "Found in " + LinkedImageCount + " image(s)";

        public string RenameFolderContextMenuString
        {
            get
            {
                if (_renameFolderPath != string.Empty)
                {
                    return "Rename Folder [" + new DirectoryInfo(_renameFolderPath).Name + "]";
                }

                return "No Rename Folder Assigned";
            }
        }

        public bool UseForNaming_IncludeCategory => UseForNaming && ParentCategory.UseForNaming;

        #region ----- Highlighting -----
        //? Used for determining which tag's font to highlight when an image is selected
        private bool _isHighlighted;

        [JsonIgnore]
        public bool IsHighlighted
        {
            get => _isHighlighted;
            set => SetProperty(ref _isHighlighted, value);
        }

        //? Similar to IsHighlighted but exists to choose a different color for Parent tags of a given tag
        private bool _isHighlightedParent;

        [JsonIgnore]
        public bool IsHighlightedParent
        {
            get => _isHighlightedParent;
            set => SetProperty(ref _isHighlightedParent, value);
        }

        //? Similar to IsHighlighted but exists to choose a different color for Child tags of a given tag
        private bool _isHighlightedChild;

        [JsonIgnore]
        public bool IsHighlightedChild
        {
            get => _isHighlightedChild;
            set => SetProperty(ref _isHighlightedChild, value);
        }

        //? Similar to IsHighlighted but exists to choose a different color for tags that exist in only *some* images of a multi-image selection
        private bool _isHighlightedInSomeImages;

        [JsonIgnore]
        public bool IsHighlightedInSomeImages
        {
            get => _isHighlightedInSomeImages;
            set => SetProperty(ref _isHighlightedInSomeImages, value);
        }
        #endregion

        #region ----- TagBoard -----

        [JsonIgnore] private TagSearchType _searchType = TaggingUtil.DEFAULT_TAG_SEARCH_TYPE;

        public TagSearchType SearchType
        {
            get => _searchType;
            set
            {
                SetProperty(ref _searchType, value);

                RaisePropertyChanged(() => SearchTypeString);
                RaisePropertyChanged(() => SearchTypeColor);
                RaisePropertyChanged(() => SearchTypeToolTip);
            }
        }

        [JsonIgnore]
        public string SearchTypeString
        {
            get
            {
                switch (SearchType)
                {
                    case TagSearchType.Mandatory: return "+";

                    case TagSearchType.Optional: return "~";

                    case TagSearchType.Excluded: return "-";
                }

                return "?";
            }
        }

        public Color SearchTypeColor
        {
            get
            {
                switch (SearchType)
                {
                    case TagSearchType.Mandatory: return Color.White;

                    case TagSearchType.Optional: return Color.LimeGreen;

                    case TagSearchType.Excluded: return Color.Red;
                }

                return Color.Purple;
            }
        }

        public string SearchTypeToolTip
        {
            get
            {
                switch (SearchType)
                {
                    case TagSearchType.Mandatory: return "Mandatory (Selection *must* have this tag)";

                    case TagSearchType.Optional: return "Optional (Selection *may* have this tag)";

                    case TagSearchType.Excluded: return "Excluded (Selection *cannot* have this tag)";
                }

                return "ERROR";
            }
        }
        #endregion
        
        public string ExceptionText => IsNamingExceptionOfSelectedImage ? "+" : "-";

        public Color ExceptionColor => IsNamingExceptionOfSelectedImage ? Color.LimeGreen : UseForNaming_IncludeCategory ? Color.White :  Color.Red;

        public bool IsNamingExceptionOfSelectedImage
        {
            get
            {
                if (WallpaperFluxViewModel.Instance.SelectedImage is ImageModel imageModel)
                {
                    return imageModel.Tags.GetTagNamingExceptions().Contains(this);
                }

                return false;
            }
        }

        private string _relativeFrequencyText;
        public string RelativeFrequencyText
        {
            get => _relativeFrequencyText;
            set => SetProperty(ref _relativeFrequencyText, value);
        }

        private string _exactFrequencyText;
        public string ExactFrequencyText
        {
            get => _exactFrequencyText;
            set => SetProperty(ref _exactFrequencyText, value);
        }

        #endregion

        #region Commands

        public IMvxCommand SelectImagesWithTag { get; set; } //! There should be a button for selecting all tags in the TagBoard and/or all selected tags

        public IMvxCommand RenameTagCommand { get; set; }

        public IMvxCommand RemoveTagCommand { get; set; }

        public IMvxCommand TagInteractCommand { get; set; }  //? Handles functions such as Tag-Adding, Removing, & Linking

        #region ----- Image Control -----
        [JsonIgnore] public IMvxCommand AddTagToSelectedImagesCommand { get; set; }

        [JsonIgnore] public IMvxCommand AddTagToEntireImageGroupCommand { get; set; }

        [JsonIgnore] public IMvxCommand RemoveTagFromSelectedImageCommand { get; set; } //? for use with the Inspector

        [JsonIgnore] public IMvxCommand RemoveTagFromSelectedImagesCommand { get; set; }

        [JsonIgnore] public IMvxCommand RemoveTagFromEntireImageGroupCommand { get; set; }
        #endregion

        #region ----- TagBoard -----
        [JsonIgnore] public IMvxCommand RemoveTagFromTagBoardCommand { get; set; }

        [JsonIgnore] public  IMvxCommand CycleSearchTypeCommand { get; set; }

        #endregion

        public IMvxCommand ToggleNamingExceptionCommand { get; set; } //? for the inspector

        public IMvxCommand AssignRenameFolderCommand { get; set; }

        public IMvxCommand RemoveRenameFolderCommand { get; set; }

        public IMvxCommand ToggleRankGraphCommand { get; set; }

        public IMvxCommand ChangeRelativeFrequencyCommand { get; set; }

        #endregion

        public TagModel(string name, CategoryModel parentCategory, bool useForNaming = true, bool enabled = true, string renameFolderPath = "")
        {
            Name = name;
            ParentCategory = parentCategory;
            if (parentCategory == null) return;

            UseForNaming = useForNaming;
            Enabled = enabled;
            RenameFolderPath = renameFolderPath;

            InitCommands();
        }
        
        private void InitCommands()
        {
            SelectImagesWithTag = new MvxCommand(() => TaggingUtil.RebuildImageSelectorWithTagFilter(GetLinkedImages().ToArray()));
            RenameTagCommand = new MvxCommand(PromptRename);
            RemoveTagCommand = new MvxCommand(PromptRemoveTag);
            TagInteractCommand = new MvxCommand(InteractTag);

            // Image Control
            AddTagToSelectedImagesCommand = new MvxCommand(() => AddTagToImages(WallpaperFluxViewModel.Instance.GetAllHighlightedImages(), false));
            AddTagToEntireImageGroupCommand = new MvxCommand(() => AddTagToImages(WallpaperFluxViewModel.Instance.GetAllImagesInTabsOrSet(), true));
            RemoveTagFromSelectedImageCommand = new MvxCommand(() => RemoveTagFromImages(ImageUtil.GetImageSet(WallpaperFluxViewModel.Instance.SelectedImage), false));
            RemoveTagFromSelectedImagesCommand = new MvxCommand(() => RemoveTagFromImages(WallpaperFluxViewModel.Instance.GetAllHighlightedImages(), false));
            RemoveTagFromEntireImageGroupCommand = new MvxCommand(() => RemoveTagFromImages(WallpaperFluxViewModel.Instance.GetAllImagesInTabsOrSet(), true));

            // TagBoard
            RemoveTagFromTagBoardCommand = new MvxCommand(() => TagViewModel.Instance.RemoveTagFromTagBoard(this));
            CycleSearchTypeCommand = new MvxCommand(CycleSearchType);

            // Naming Exceptions
            ToggleNamingExceptionCommand = new MvxCommand(ToggleNamingException);

            // Folder Rename Priority
            AssignRenameFolderCommand = new MvxCommand(() => RenameFolderPath = FolderUtil.PromptValidFolderPath());
            RemoveRenameFolderCommand = new MvxCommand(() => RenameFolderPath = string.Empty);

            // Rank Graph
            ToggleRankGraphCommand = new MvxCommand(TagViewModel.Instance.ToggleRankGraph);

            // Frequency
            ChangeRelativeFrequencyCommand = new MvxCommand(ChangeRelativeFrequency);
        }

        public bool IsEnabled()
        {
            if (!Enabled) return false;

            if (!ParentCategory.Enabled) return false;

            foreach (TagModel parentTag in ParentTags)
            {
                if (!parentTag.IsEnabled()) return false;
            }

            return true;
        }

        #region Image Addition / Removal
        public void AddTagToImages(ImageModel[] images, bool promptUser)
        {
            bool canAdd = true;
            if (promptUser) canAdd = MessageBoxUtil.PromptYesNo("Are you sure you want to add the tag [" + Name + "] to " + images.Length + " image(s)?");

            if (canAdd)
            {
                foreach (ImageModel image in images) image.AddTag(this, false); //! TAG HIGHLIGHT DONE BELOW
            }

            RaisePropertyChangedImageCount();
            TaggingUtil.HighlightTags();
        }

        public void RemoveTagFromImages(ImageModel[] images, bool promptUser)
        {
            bool canRemove = true;
            if (promptUser) canRemove = MessageBoxUtil.PromptYesNo("Are you sure you want to remove the tag [" + Name + "] from " + images.Length + " image(s)?");

            if (canRemove)
            {
                foreach(ImageModel image in images) image.RemoveTag(this, false); //! TAG HIGHLIGHT DONE BELOW
            }

            RaisePropertyChangedImageCount();
            TaggingUtil.HighlightTags();
        }

        public void ToggleTagWithImages(ImageModel[] images)
        {
            bool highlightedInSomeImages = IsHighlightedInSomeImages; //! this can change mid-process so we want to make sure that doesn't happen for this for-loop, otherwise only 1 image can be removed
            // toggles tags for the given group of images
            foreach (ImageModel image in images)
            {
                if (!image.ContainsTag(this))
                {
                    image.AddTag(this, false); //! TAG HIGHLIGHT DONE BELOW
                }
                else
                {
                    if (!highlightedInSomeImages) //? in this case we will only add the tag to ensure that it is added to all corresponding images before allowing removal
                    {
                        image.RemoveTag(this, false); //! TAG HIGHLIGHT DONE BELOW
                    }
                }
            }

            TaggingUtil.HighlightTags();
        }
        #endregion

        #region Image Linking
        public void LinkImage(ImageTagCollection imageTags)
        {
            LinkedImages.Add(imageTags.ParentImage);

            //! Calling property changed here risked a considerable performance drop when tagging multiple images at once, use interact tag instead
            //xRaisePropertyChangedImageCount();
        }

        public void UnlinkImage(ImageTagCollection imageTags)
        {
            LinkedImages.Remove(imageTags.ParentImage);

            //! Calling property changed here risked a considerable performance drop when tagging multiple images at once, use interact tag instead
            //xRaisePropertyChangedImageCount();
        }

        public void UnlinkAllImages(bool highlightTags) // for removing/resetting a tag
        {
            List<ImageModel> imagesToRemove = new List<ImageModel>();
            foreach(ImageModel image in LinkedImages) //? We have to redirect the collection type to prevent the collection modified error
            {
                imagesToRemove.Add(image);
            }

            foreach (ImageModel image in imagesToRemove) //! this will refer back to LinkedImages, which can cause a modified collection error
            {
                image.RemoveTag(this, highlightTags); // will call UnlinkImage()
            }

            RaisePropertyChangedImageCount(); //? Not really needed for THIS tag but would ideally still exist here and IS NEEDED for the parent tags
        }
        
        public void RaisePropertyChangedImageCount()
        {
            // this is going to be called an enormous number of times while loading since images & tag linking is being loaded
            if (JsonUtil.IsLoadingData) return;

            LinkedImageCount = GetLinkedImageCount(); // we want to call this as few times as possible
            RaisePropertyChanged(() => ImageCountStringTag);
            RaisePropertyChanged(() => ImageCountStringContextMenu);

            // we need to update the image count of parent tags too (and their parent tags)
            foreach (TagModel parentTag in ParentTags)
            {
                parentTag.RaisePropertyChangedImageCount();
            }
        }

        public int GetLinkedImageCount(bool accountForInvalid = false) => GetLinkedImages(accountForInvalid).Count;

        //! Remember that we need to check for child tags too as references to linked child tags are included in references to the parent tag 
        //! (Prevents saving parent tag to JSON)
        /// <summary>
        /// Gets all linked images of this tag and its child tags
        /// </summary>
        /// <returns></returns>
        public HashSet<BaseImageModel> GetLinkedImages(bool accountForInvalid = true, bool convertImagesToSets = false, bool ignoreChildTags = false)
        {
            //? Only include images that actually exist (helps to detect and remove deleted images)
            HashSet<BaseImageModel> images;
            if (accountForInvalid) // accounting for invalid can add significant processing time when this is accessed multiple times by a tag that has a large number of children
            {
                images = new HashSet<BaseImageModel>(LinkedImages.Where(f => FileUtil.Exists(f.Path)));
            }
            else
            {
                images = new HashSet<BaseImageModel>(LinkedImages);
            }

            if (!ignoreChildTags)
            {
                foreach (TagModel tag in ChildTags)
                {
                    images.UnionWith(tag.GetLinkedImages(accountForInvalid));
                }
            }

            if (convertImagesToSets) //? keeps the set itself but removes the images within the sets
            {
                List<ImageModel> imagesToRemove = new List<ImageModel>();
                HashSet<ImageSetModel> imageSetsToAdd = new HashSet<ImageSetModel>();
                foreach (BaseImageModel image in images)
                {
                    if (image is ImageModel imageModel)
                    {
                        if (imageModel.IsInImageSet)
                        {
                            imagesToRemove.Add(imageModel);
                            imageSetsToAdd.Add(imageModel.ParentImageSet);
                        }
                    }
                }

                foreach (ImageModel image in imagesToRemove)
                {
                    images.Remove(image);
                }

                foreach (ImageSetModel imageSet in imageSetsToAdd)
                {
                    images.Add(imageSet);
                }
            }

            return images;
        }

        //! Remember that we need to check for child tags too as references to linked child tags are included in references to the parent tag
        //! (Prevents saving parent tag to JSON)
        /// <summary>
        /// Checks if the given image is linked to this tag or any of its child tags
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        public bool ContainsLinkedImage(BaseImageModel image)
        {
            return ImageUtil.PerformImageCheck(image, ContainsLinkedImage, !ThemeUtil.Theme.Settings.ThemeSettings.EnableDetectionOfInactiveImages); // we only need one valid image to verify an image set
        }

        public bool ContainsLinkedImage(ImageModel image)
        {
            if (LinkedImages.Contains(image)) return true;

            foreach (TagModel tag in ChildTags)
            {
                //? using the LinkedImages variable instead of this method will cause this method to miss child tags of the child tag
                if (tag.ContainsLinkedImage(image)) return true;
            }

            return false;
        }

        public void UpdateLinkedImagesEnabledState()
        {
            foreach (ImageModel image in LinkedImages)
            {
                image.UpdateEnabledState();
            }

            foreach (TagModel childTag in ChildTags) // we also need to turn off child tags
            {
                childTag.UpdateLinkedImagesEnabledState();
            }
        }

        #endregion

        #region Parent / Child Tag Linking

        public void LinkTag(TagModel tag, bool highlightTags)
        {
            if (tag == this) return; // can't link a tag to itself

            if (!ChildTags.Contains(tag)) // can't make a child tag of this tag also its parent tag, would cause looping errors and just not make sense
            {
                if (!ParentTags.Contains(tag))
                {
                    ParentTags.Add(tag); // makes the given tag the parent tag of this tag
                    tag.ChildTags.Add(this); // makes this tag a child tag of the given tag
                }
                
                // the highlightTags bool is a nice way to avoid bulk highlights of tags which can bog down the system
                if (highlightTags) TaggingUtil.HighlightTags(/*xParentChildTagsUnion_IncludeSelf()*/);
            }
        }

        public void UnlinkTag(TagModel tag, bool highlightTags)
        {
            if (tag == this) return; // can't link a tag to itself

            if (ParentTags.Contains(tag)) // can't unlink a tag that isn't linked
            {
                ParentTags.Remove(tag); // stops the given tag from being a parent tag of this tag
                tag.ChildTags.Remove(this); // stops this tag from being the child tag of the given tag
            }

            // the highlightTags bool is a nice way to avoid bulk highlights of tags which can bog down the system
            if (highlightTags) TaggingUtil.HighlightTags(/*xParentChildTagsUnion_IncludeSelf()*/);
        }

        public void ToggleTagLink(TagModel tag)
        {
            // toggles the source tag's link status with this tag
            if (!tag.HasTagAsParent(this))
            {
                tag.LinkTag(this, true); // making this tag the parent of the given tag
            }
            else
            {
                tag.UnlinkTag(this, true); // removing this tag from being the parent of the given tag
            }
        }

        public void UnlinkAllParentAndChildTags(bool highlightTags)
        {
            while (ParentTags.Count > 0)
            {
                UnlinkTag(ParentTags.First(), false); // removes the given tag from being the parent of this tag
            }

            while (ChildTags.Count > 0)
            {
                ChildTags.First().UnlinkTag(this, false); // removing this tag from being the parent of the given tag
            }

            if (highlightTags) TaggingUtil.HighlightTags(/*xParentChildTagsUnion_IncludeSelf()*/);
        }

        public HashSet<TagModel> GetParentChildTagsUnion()
        {
            HashSet<TagModel> parentChildTags = new HashSet<TagModel>(ParentTags);
            parentChildTags.UnionWith(ChildTags);
            return parentChildTags;
        }

        public HashSet<TagModel> GetParentChildTagsUnion_IncludeSelf()
        {
            HashSet<TagModel> parentChildTags = GetParentChildTagsUnion();
            parentChildTags.Add(this);
            return parentChildTags;
        }

        public HashSet<TagModel> GetParentTags() => ParentTags;

        public HashSet<TagModel> GetChildTags() => ChildTags;

        public bool HasTagAsParent(TagModel tag) => ParentTags.Contains(tag);

        public bool HasTagAsChild(TagModel tag) => ChildTags.Contains(tag);

        public bool HasParent() => ParentTags.Count > 0;

        public bool HasChild() => ChildTags.Count > 0;

        #endregion

        #region Command Methods

        //? handles TagAdder, TagLinker, etc.
        public void InteractTag()
        {
            if (TaggingUtil.GetTagAdderToggle()) // no need to grab the images if we aren't adding/removing tags from one
            {
                if (WallpaperFluxViewModel.Instance.SelectedImageSelectorTab != null)
                {
                    ToggleTagWithImages(WallpaperFluxViewModel.Instance.GetAllHighlightedImages());
                    IsHighlightedInSomeImages = false; // after calling this there's no way for the tag to still be highlighted in only some images
                }

                RaisePropertyChangedImageCount();
            }
            else if (TaggingUtil.GetTagLinkerToggle()) // we should only be doing one of these at a time
            {
                ToggleTagLink(TagViewModel.Instance.TagLinkingSource);

                RaisePropertyChangedImageCount();
            }
        }

        public void PromptRename()
        {
            string name = MessageBoxUtil.GetString("Rename", "Give a new name for this tag", "Tag Name...");

            if (!string.IsNullOrWhiteSpace(name)) Name = name;

            RaisePropertyChanged(() => Name);
        }

        public void PromptRemoveTag() //? removes the tag from the theme
        {
            if (MessageBoxUtil.PromptYesNo("Are you sure you want to remove the tag: [" + Name + "] ?"))
            {
                if (!TaggingUtil.RemoveTag(this)) MessageBoxUtil.ShowError("The tag, " + Name + ", does not exist");
            }
        }
        public void CycleSearchType()
        {
            // cycles between the search types
            switch (SearchType)
            {
                case TagSearchType.Mandatory:
                    SearchType = TagSearchType.Optional;
                    break;

                case TagSearchType.Optional:
                    SearchType = TagSearchType.Excluded;
                    break;

                case TagSearchType.Excluded:
                    SearchType = TagSearchType.Mandatory;
                    break;
            }
        }

        public void ToggleNamingException()
        {
            if (WallpaperFluxViewModel.Instance.SelectedImage is ImageModel imageModel)
            {
                if (!IsNamingExceptionOfSelectedImage)
                {
                    imageModel.Tags.AddNamingException(this);
                }
                else
                {
                    imageModel.Tags.RemoveNamingException(this);
                }

                RaisePropertyChanged(() => ExceptionText);
                RaisePropertyChanged(() => ExceptionColor);
            }
        }

        public void UpdateFrequencies()
        {
            RelativeFrequencyText = "Relative Frequency: " +Math.Round(TaggingUtil.TagFrequencies.GetRelativeFrequency(this) * 100, 5) + "%";
            ExactFrequencyText = "Exact Frequency: " + Math.Round(TaggingUtil.TagFrequencies.GetExactFrequency(this) * 100, 5) + "%";
        }

        public void ChangeRelativeFrequency()
        {
            if (MessageBoxUtil.GetInteger("Relative Frequency", "Set Frequency", out int frequency, "Relative Frequency..."))
            {
                TaggingUtil.TagFrequencies.ModifyFrequency(this, (double)frequency / 100);

                UpdateFrequencies();
            }
        }

        #endregion
    }
}
