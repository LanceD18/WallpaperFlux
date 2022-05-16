using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
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
        [JsonIgnore] public HashSet<TagModel> ParentTags = new HashSet<TagModel>(); //? Will be converted to strings in TagModelJson.cs for saving purposes instead of saving the entire object
        [JsonIgnore] public HashSet<TagModel> ChildTags = new HashSet<TagModel>(); //? Will be converted to strings in TagModelJson.cs for saving purposes instead of saving the entire object
        //xpublic HashSet<Tuple<string, string>> ParentTags = new HashSet<Tuple<string, string>>();
        //xpublic HashSet<Tuple<string, string>> ChildTags = new HashSet<Tuple<string, string>>();

        private string parentCategoryName;
        
        public string ParentCategoryName
        {
            get => parentCategoryName;

            set
            {
                /*x
                if (parentCategoryName != "")
                {
                    UpdateLinkedTagsCategoryName(value);
                }
                */

                parentCategoryName = value;
            }
        }

        //? We are ignoring this since these should get implemented on loading in the images through their TagCollection
        [JsonIgnore] public HashSet<ImageModel> LinkedImages = new HashSet<ImageModel>(); //? Will be converted to strings in TagModelJson.cs for saving purposes instead of saving the entire object

        #region UI Control

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

        #endregion

        #region Commands

        [JsonIgnore] public IMvxCommand AddTagToSelectedImagesCommand { get; set; }

        [JsonIgnore] public IMvxCommand AddTagToEntireImageGroupCommand { get; set; }

        [JsonIgnore] public IMvxCommand RemoveTagFromSelectedImageCommand { get; set; } //? for use with the Inspector

        [JsonIgnore] public IMvxCommand RemoveTagFromSelectedImagesCommand { get; set; }

        [JsonIgnore] public IMvxCommand RemoveTagFromEntireImageGroupCommand { get; set; }

        [JsonIgnore] public IMvxCommand TagInteractCommand { get; set; }  //? Handles functions such as Tag-Adding, Removing, & Linking

        [JsonIgnore] public IMvxCommand RemoveTagFromTagBoardCommand { get; set; }

        #endregion

        public TagModel(string name)
        {
            Name = name;

            AddTagToSelectedImagesCommand = new MvxCommand(() => AddTagToSelectedImages(WallpaperFluxViewModel.Instance.SelectedImageSelectorTab.GetSelectedItems()));
            AddTagToEntireImageGroupCommand = new MvxCommand(() => AddTagToSelectedImages(WallpaperFluxViewModel.Instance.SelectedImageSelectorTab.GetAllItems()));
            RemoveTagFromSelectedImageCommand = new MvxCommand(() => RemoveTagFromSelectedImages(new[] { WallpaperFluxViewModel.Instance.SelectedImageSelectorTab.SelectedImage } ));
            RemoveTagFromSelectedImagesCommand = new MvxCommand(() => RemoveTagFromSelectedImages(WallpaperFluxViewModel.Instance.SelectedImageSelectorTab.GetSelectedItems()));
            RemoveTagFromEntireImageGroupCommand = new MvxCommand(() => RemoveTagFromSelectedImages(WallpaperFluxViewModel.Instance.SelectedImageSelectorTab.GetAllItems()));
            TagInteractCommand = new MvxCommand( () =>
            {
                ImageModel[] selectedImages = null;

                if (TaggingUtil.GetTagAdderToggle() || TaggingUtil.GetTagRemoverToggle()) // no need to grab the images if we aren't adding/removing tags from one
                {
                    selectedImages = WallpaperFluxViewModel.Instance.SelectedImageSelectorTab.GetSelectedItems();
                }

                InteractWithTag(selectedImages);
            });
            RemoveTagFromTagBoardCommand = new MvxCommand(() => TagViewModel.Instance.RemoveTagFromTagBoard(this));
        }

        public void LinkImage(TagCollection tagLinker)
        {
            LinkedImages.Add(tagLinker.ParentImage);
        }

        public void UnlinkImage(TagCollection tagLinker)
        {
            LinkedImages.Remove(tagLinker.ParentImage);
        }

        public int GetLinkedImageCount()
        {
            Debug.WriteLine("Find an efficient way to get the number of images linked to a tag without having multiple references like in the previous TagData vs ImageData");
            return 0;
        }

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
                
                TaggingUtil.HighlightTags(ParentChildTagsUnionWithSelf());
            }
        }

        public HashSet<TagModel> ParentChildTagsUnion()
        {
            HashSet<TagModel> parentChildTags = new HashSet<TagModel>(ParentTags);
            parentChildTags.UnionWith(ChildTags);
            return parentChildTags;
        }

        public HashSet<TagModel> ParentChildTagsUnionWithSelf()
        {
            HashSet<TagModel> parentChildTags = ParentChildTagsUnion();
            parentChildTags.Add(this);
            return parentChildTags;
        }

        #region Command Methods

        public void AddTagToSelectedImages(ImageModel[] images)
        {
            foreach (ImageModel image in images) image.AddTag(this);
        }

        public void RemoveTagFromSelectedImages(ImageModel[] images)
        {
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
