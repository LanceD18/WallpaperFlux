using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using LanceTools;
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
        public string Name { get; private set; }

        private bool _enabled;

        public bool Enabled
        {
            get => _enabled;

            set
            {
                _enabled = value;
                /*x

                if (_enabled != value)  // prevents unnecessary calls
                {
                    _enabled = value;

                    if (LinkedImages != null)
                    {
                        if (!WallpaperData.IsLoadingData)
                        {
                            WallpaperData.EvaluateImageActiveStates(LinkedImages.ToArray(), !value); // will forceDisable if the value is set to false
                        }
                    }
                }
                */
            }
        }

        private bool _UseForNaming;

        public bool UseForNaming
        {
            get => _UseForNaming;

            set
            {
                _UseForNaming = value;
                /*x
                if (_UseForNaming != value)  // prevents unnecessary calls
                {
                    _UseForNaming = value;

                    if (LinkedImages != null)
                    {
                        HashSet<WallpaperData.ImageData> imagesToRename = new HashSet<WallpaperData.ImageData>();
                        foreach (string imagePath in GetLinkedImages())
                        {
                            imagesToRename.Add(WallpaperData.GetImageData(imagePath));
                        }
                    }
                }
                */
            }
        }

        //? Just create a TagModelJSON.cs that converts all of these into strings on saving the theme
        //! Using a string will require us to update this on rename, and search for the tag when needed, so lets just save the string portion for when saving
        private HashSet<TagModel> ParentTags = new HashSet<TagModel>(); //? Will be converted to strings in TagModelJson.cs for saving purposes instead of saving the entire object

        private HashSet<TagModel> ChildTags = new HashSet<TagModel>(); //? Will be converted to strings in TagModelJson.cs for saving purposes instead of saving the entire object
        //xpublic HashSet<Tuple<string, string>> ParentTags = new HashSet<Tuple<string, string>>();
        //xpublic HashSet<Tuple<string, string>> ChildTags = new HashSet<Tuple<string, string>>();

        //! Using a string will require us to update this on rename, and search for the category when needed, and this isn't even being saved so lets just avoid the hassle
        [JsonIgnore] public CategoryModel ParentCategory;

        //? We are ignoring this since these should get implemented on loading in the images through their TagCollection
        private HashSet<ImageModel> LinkedImages = new HashSet<ImageModel>(); //? Will be converted to strings in TagModelJson.cs for saving purposes instead of saving the entire object

        #region View Variables

        [JsonIgnore] public string ImageCountStringContext => "Found in " + LinkedImages.Count + " image(s)";

        [JsonIgnore] public string ImageCountStringTag => "(" + LinkedImages.Count + ")";

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

        #endregion

        #region Commands

        [JsonIgnore] public IMvxCommand SelectImagesWithTag { get; set; } //! There should be a button for selecting all tags in the TagBoard and/or all selected tags

        [JsonIgnore] public IMvxCommand RenameTagCommand { get; set; }

        [JsonIgnore] public IMvxCommand RemoveTagCommand { get; set; }

        [JsonIgnore] public IMvxCommand TagInteractCommand { get; set; }  //? Handles functions such as Tag-Adding, Removing, & Linking

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

        #endregion

        public TagModel(string name, CategoryModel parentCategory, bool useForNaming = true, bool enabled = true)
        {
            Name = name;
            ParentCategory = parentCategory;
            UseForNaming = true;
            Enabled = true;

            SelectImagesWithTag = new MvxCommand(() => WallpaperFluxViewModel.Instance.RebuildImageSelector(LinkedImages.ToArray()));
            RenameTagCommand = new MvxCommand(PromptRename);
            RemoveTagCommand = new MvxCommand(PromptRemoveTag);
            TagInteractCommand = new MvxCommand(InteractTag);

            // Image Control
            AddTagToSelectedImagesCommand = new MvxCommand(() => AddTagToImages(WallpaperFluxViewModel.Instance.SelectedImageSelectorTab.GetSelectedItems(), false));
            AddTagToEntireImageGroupCommand = new MvxCommand(() => AddTagToImages(WallpaperFluxViewModel.Instance.SelectedImageSelectorTab.GetAllItems(), true));
            RemoveTagFromSelectedImageCommand = new MvxCommand(() => RemoveTagFromImages(new[] { WallpaperFluxViewModel.Instance.SelectedImageSelectorTab.SelectedImage }, false));
            RemoveTagFromSelectedImagesCommand = new MvxCommand(() => RemoveTagFromImages(WallpaperFluxViewModel.Instance.SelectedImageSelectorTab.GetSelectedItems(), false));
            RemoveTagFromEntireImageGroupCommand = new MvxCommand(() => RemoveTagFromImages(WallpaperFluxViewModel.Instance.SelectedImageSelectorTab.GetAllItems(), true));

            // TagBoard
            RemoveTagFromTagBoardCommand = new MvxCommand(() => TagViewModel.Instance.RemoveTagFromTagBoard(this));
            CycleSearchTypeCommand = new MvxCommand(() =>
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
            });
        }

        #region Image Addition / Removal
        public void AddTagToImages(ImageModel[] images, bool promptUser)
        {
            if (promptUser) MessageBoxUtil.PromptYesNo("Are you sure you want to add the tag [" + Name + "] to " + images.Length + " image(s)?");

            foreach (ImageModel image in images) image.AddTag(this);
        }

        public void RemoveTagFromImages(ImageModel[] images, bool promptUser)
        {
            if (promptUser) MessageBoxUtil.PromptYesNo("Are you sure you want to remove the tag [" + Name + "] from " + images.Length + " image(s)?");

            foreach (ImageModel image in images) image.RemoveTag(this);
        }

        public void ToggleTagWithImages(ImageModel[] images)
        {
            // toggles tags for the given group of images
            foreach (ImageModel image in images)
            {
                if (!image.ContainsTag(this))
                {
                    image.AddTag(this);
                }
                else
                {
                    if (!IsHighlightedInSomeImages) //? in this case we will only add the tag to ensure that it is added to all corresponding images before allowing removal
                    {
                        image.RemoveTag(this);
                    }
                }
            }
        }
        #endregion

        #region Image Linking
        public void LinkImage(ImageTagCollection imageTags)
        {
            LinkedImages.Add(imageTags.ParentImage);
            RaisePropertyChanged(() => ImageCountStringTag);
            RaisePropertyChanged(() => ImageCountStringContext);
        }

        public void UnlinkImage(ImageTagCollection imageTags)
        {
            LinkedImages.Remove(imageTags.ParentImage);
            RaisePropertyChanged(() => ImageCountStringTag);
            RaisePropertyChanged(() => ImageCountStringContext);
        }

        public void UnlinkAllImages() // for removing/resetting a tag
        {
            foreach(ImageModel image in LinkedImages.ToArray()) //? We have to redirect the collection type to prevent the collection modified error
            {
                image.RemoveTag(this); // will call UnlinkImage()
            }

            RaisePropertyChanged(() => ImageCountStringTag); //? Not really needed in current use cases but would ideally still exist here
            RaisePropertyChanged(() => ImageCountStringContext); //? Not really needed in current use cases but would ideally still exist here
        }

        public int GetLinkedImageCount() => LinkedImages.Count;

        public ImageModel[] GetLinkedImages() => LinkedImages.ToArray();

        public bool ContainsLinkedImage(ImageModel image) => LinkedImages.Contains(image);

        #endregion

        #region Parent / Child Tag Linking

        public void LinkTag(TagModel tag)
        {
            if (tag == this) return; // can't link a tag to itself

            if (!ChildTags.Contains(tag)) // can't make a child tag of this tag also its parent tag, would cause looping errors and just not make sense
            {
                if (!ParentTags.Contains(tag))
                {
                    ParentTags.Add(tag);
                    tag.ChildTags.Add(this); // when becoming the parent tag of another tag we must also become its child tag
                }
                
                TaggingUtil.HighlightTags(/*xParentChildTagsUnion_IncludeSelf()*/);
            }
        }

        public void UnlinkTag(TagModel tag)
        {
            if (tag == this) return; // can't link a tag to itself

            if (ParentTags.Contains(tag)) // can't unlink a tag that isn't linked
            {
                ParentTags.Remove(tag);
                tag.ChildTags.Remove(this);
            }

            TaggingUtil.HighlightTags(/*xParentChildTagsUnion_IncludeSelf()*/);
        }

        public void ToggleTagLink(TagModel tag)
        {
            // toggles the source tag's link status with this tag
            if (!tag.HasParent(this))
            {
                tag.LinkTag(this);
            }
            else
            {
                tag.UnlinkTag(this);
            }
        }

        public HashSet<TagModel> ParentChildTagsUnion()
        {
            HashSet<TagModel> parentChildTags = new HashSet<TagModel>(ParentTags);
            parentChildTags.UnionWith(ChildTags);
            return parentChildTags;
        }

        public HashSet<TagModel> ParentChildTagsUnion_IncludeSelf()
        {
            HashSet<TagModel> parentChildTags = ParentChildTagsUnion();
            parentChildTags.Add(this);
            return parentChildTags;
        }

        public bool HasParent(TagModel tag) => ParentTags.Contains(tag);
        public bool HasChild(TagModel tag) => ChildTags.Contains(tag);

        #endregion

        #region Command Methods

        //? handles TagAdder, TagLinker, etc.
        public void InteractTag()
        {
            if (TaggingUtil.GetTagAdderToggle()) // no need to grab the images if we aren't adding/removing tags from one
            {
                ToggleTagWithImages(WallpaperFluxViewModel.Instance.SelectedImageSelectorTab.GetSelectedItems());
                IsHighlightedInSomeImages = false; // after calling this there's no way for the tag to still be highlighted in only some images
                return; // we should only be doing one of these at a time
            }

            if (TaggingUtil.GetTagLinkerToggle())
            {
                ToggleTagLink(TagViewModel.Instance.TagLinkingSource);
                return; // we should only be doing one of these at a time
            }
        }

        public void PromptRename()
        {
            string name = MessageBoxUtil.GetString("Rename", "Give a new name for this tag", "Tag Name...");

            if (!string.IsNullOrWhiteSpace(name)) Name = name;
        }

        public void PromptRemoveTag() //? removes the tag from the theme
        {
            if (MessageBoxUtil.PromptYesNo("Are you sure you want to remove the tag: [" + Name + "] ?"))
            {
                if (!TaggingUtil.RemoveTag(this)) MessageBoxUtil.ShowError("The tag, " + Name + ", does not exist");
            }
        }
        #endregion
    }
}
