using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        //? Used for determining which tag's font to highlight when an image is selected
        private bool _isHighlighted;
        [JsonIgnore]
        public bool IsHighlighted
        {
            get => _isHighlighted;
            set => SetProperty(ref _isHighlighted, value);
        }

        //? Similar to IsHighlighted but exists to choose a different color
        private bool _isHighlightedParent;
        [JsonIgnore]
        public bool IsHighlightedParent
        {
            get => _isHighlightedParent;
            set => SetProperty(ref _isHighlightedParent, value);
        }

        //? Similar to IsHighlighted but exists to choose a different color
        private bool _isHighlightedChild;
        [JsonIgnore]
        public bool IsHighlightedChild
        {
            get => _isHighlightedChild;
            set => SetProperty(ref _isHighlightedChild, value);
        }

        [JsonIgnore] public string ImageCountStringContext => "Found in " + LinkedImages.Count + " image(s)";

        [JsonIgnore] public string ImageCountStringTag => "(" + LinkedImages.Count + ")";

        #endregion

        #region Commands

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

        [JsonIgnore] public IMvxCommand RemoveTagFromTagBoardCommand { get; set; }

        #endregion

        public TagModel(string name, CategoryModel parentCategory, bool useForNaming = true, bool enabled = true)
        {
            Name = name;
            ParentCategory = parentCategory;
            UseForNaming = true;
            Enabled = true;

            AddTagToSelectedImagesCommand = new MvxCommand(() => AddTagToSelectedImages(WallpaperFluxViewModel.Instance.SelectedImageSelectorTab.GetSelectedItems(), false));
            AddTagToEntireImageGroupCommand = new MvxCommand(() => AddTagToSelectedImages(WallpaperFluxViewModel.Instance.SelectedImageSelectorTab.GetAllItems(), true));
            RemoveTagFromSelectedImageCommand = new MvxCommand(() => RemoveTagFromSelectedImages(new[] { WallpaperFluxViewModel.Instance.SelectedImageSelectorTab.SelectedImage }, false));
            RemoveTagFromSelectedImagesCommand = new MvxCommand(() => RemoveTagFromSelectedImages(WallpaperFluxViewModel.Instance.SelectedImageSelectorTab.GetSelectedItems(), false));
            RemoveTagFromEntireImageGroupCommand = new MvxCommand(() => RemoveTagFromSelectedImages(WallpaperFluxViewModel.Instance.SelectedImageSelectorTab.GetAllItems(), true));

            TagInteractCommand = new MvxCommand(() =>
            {
                ImageModel[] selectedImages = null;

                if (TaggingUtil.GetTagAdderToggle() || TaggingUtil.GetTagRemoverToggle()) // no need to grab the images if we aren't adding/removing tags from one
                {
                    selectedImages = WallpaperFluxViewModel.Instance.SelectedImageSelectorTab.GetSelectedItems();
                }

                InteractWithTag(selectedImages);
            });

            RemoveTagFromTagBoardCommand = new MvxCommand(() => TagViewModel.Instance.RemoveTagFromTagBoard(this));

            RenameTagCommand = new MvxCommand(PromptRename);
            RemoveTagCommand = new MvxCommand(PromptRemoveTag);
        }

        public void PromptRename()
        {
            string name = MessageBoxUtil.GetString("Rename", "Give a new name for this tag", "Tag Name...");

            if (!string.IsNullOrWhiteSpace(name)) Name = name;
        }

        public void PromptRemoveTag()
        {
            if (MessageBoxUtil.PromptYesNo("Are you sure you want to remove the tag: [" + Name + "] ?"))
            {
                if (!TaggingUtil.RemoveTag(this)) MessageBoxUtil.ShowError("The tag, " + Name + ", does not exist");
            }
        }

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
                else // a simple toggle to also remove the parent/child reference of this tag when attempting to link again
                {
                    ParentTags.Remove(tag);
                    tag.ChildTags.Remove(this);
                }
                
                TaggingUtil.HighlightTags(ParentChildTagsUnion_IncludeSelf());
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

        public void AddTagToSelectedImages(ImageModel[] images, bool promptUser)
        {
            if (promptUser) MessageBoxUtil.PromptYesNo("Are you sure you want to add the tag [" + Name + "] to " + images.Length + " image(s)?");

            foreach (ImageModel image in images) image.AddTag(this);
        }

        public void RemoveTagFromSelectedImages(ImageModel[] images, bool promptUser)
        {
            if (promptUser) MessageBoxUtil.PromptYesNo("Are you sure you want to remove the tag [" + Name + "] from " + images.Length + " image(s)?");

            foreach (ImageModel image in images) image.RemoveTag(this);
        }

        public void InteractWithTag(ImageModel[] images)
        {
            if (TaggingUtil.GetTagAdderToggle())
            {
                foreach (ImageModel image in images) image.AddTag(this);
            }

            if (TaggingUtil.GetTagRemoverToggle())
            {
                foreach (ImageModel image in images) image.RemoveTag(this);
            }

            if (TaggingUtil.GetTagLinkerToggle())
            {
                TagViewModel.Instance.TagLinkingSource.LinkTag(this);
            }
        }

        #endregion
    }
}
