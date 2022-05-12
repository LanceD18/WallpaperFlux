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

        public HashSet<Tuple<string, string>> ParentTags = new HashSet<Tuple<string, string>>();
        public HashSet<Tuple<string, string>> ChildTags = new HashSet<Tuple<string, string>>();

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
        [JsonIgnore] public HashSet<ImageModel> LinkedImages = new HashSet<ImageModel>();

        #region UI Control

        //? Used for determining which tag's font to highlight when an image is selected
        private bool _isHighlighted;
        [JsonIgnore]
        public bool IsHighlighted
        {
            get => _isHighlighted;
            set => SetProperty(ref _isHighlighted, value);
        }

        #endregion

        #region Commands

        [JsonIgnore] public IMvxCommand AddTagToSelectedImagesCommand { get; set; }

        [JsonIgnore] public IMvxCommand AddTagToEntireImageGroupCommand { get; set; }

        [JsonIgnore] public IMvxCommand RemoveTagFromSelectedImageCommand { get; set; } //? for use with the Inspector

        [JsonIgnore] public IMvxCommand RemoveTagFromSelectedImagesCommand { get; set; }

        [JsonIgnore] public IMvxCommand RemoveTagFromEntireImageGroupCommand { get; set; }

        [JsonIgnore] public IMvxCommand TagInteractCommand { get; set; }

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
            TagInteractCommand = new MvxCommand( () => InteractWithTag(WallpaperFluxViewModel.Instance.SelectedImageSelectorTab.GetSelectedItems()));
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
        }

        #endregion
    }
}
