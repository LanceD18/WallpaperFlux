﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using AdonisUI.Controls;
using LanceTools;
using Microsoft.WindowsAPICodePack.Dialogs;
using MvvmCross;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using WallpaperFlux.Core.IoC;
using WallpaperFlux.Core.Models;
using WallpaperFlux.Core.Util;

namespace WallpaperFlux.Core.ViewModels
{


    public class ImageSelectionViewModel : MvxViewModel
    {
        public static ImageSelectionViewModel Instance; // allows the data to remain persistent without having to reload everything once the view is closed

        private int _specifiedRank;
        public int SpecifiedRank
        {
            get => _specifiedRank;
            set => SetProperty(ref _specifiedRank, ThemeUtil.Theme.RankController.ClampValueToRankRange(value));
        }

        private int _minSpecifiedRank;
        public int MinSpecifiedRank
        {
            get => _minSpecifiedRank;
            set => SetProperty(ref _minSpecifiedRank, ThemeUtil.Theme.RankController.ClampValueToRankRange(value));
        }

        private int _maxSpecifiedRank;
        public int MaxSpecifiedRank
        {
            get => _maxSpecifiedRank;
            set => SetProperty(ref _maxSpecifiedRank, ThemeUtil.Theme.RankController.ClampValueToRankRange(value));
        }

        public bool ShowDisabledSelector => ThemeUtil.ThemeSettings.EnableDetectionOfInactiveImages;

        #region Checkboxes & Radio Buttons

        public bool Randomize { get; set; }

        public bool Reverse { get; set; }

        public bool DateTime { get; set; }

        public bool ImageSetRestriction { get; set; }

        public bool TagboardFilter { get; set; }

        public bool RadioAllRanks { get; set; } = true;
        
        public bool RadioUnranked { get; set; }
        
        public bool RadioRanked { get; set; }

        public bool RadioAllTypes { get; set; } = true;

        public bool RadioStatic { get; set; }

        public bool RadioGif { get; set; }

        public bool RadioVideo { get; set; }

        public bool RadioSpecificRank { get; set; }

        public bool RadioRankRange { get; set; }

        public bool IncludeDependentImages { get; set; }

        #endregion

        #region Commands

        public IMvxCommand SelectImagesCommand { get; set; }
        
        public IMvxCommand SelectImagesInFolderCommand { get; set; }
        
        public IMvxCommand SelectActiveWallpapersCommand { get; set; }

        public IMvxCommand SelectDisabledImagesCommand { get; set; }

        #endregion

        public ImageSelectionViewModel()
        {
            SelectImagesCommand = new MvxCommand( () =>
            {
                BaseImageModel[] images;

                // TODO With this set up, we will check for image sets twice (second time in RebuildImageSelector()) ; find a better way to do this
                if (!ImageSetRestriction)
                {
                    images = ThemeUtil.Theme.Images.GetAllImages();
                }
                else
                {
                    images = ThemeUtil.Theme.Images.GetAllImageSets();
                }

                RebuildImageSelectorWithOptions(FilterImages(images));
            });
            SelectImagesInFolderCommand = new MvxCommand(PromptFolder);
            SelectActiveWallpapersCommand = new MvxCommand(SelectActiveWallpapers);
            SelectDisabledImagesCommand = new MvxCommand(SelectDisabledImages);
        }

        public void RebuildImageSelectorWithOptions(BaseImageModel[] images, bool closeWindow = true)
        {
            WallpaperFluxViewModel.Instance.RebuildImageSelector(images, Randomize, Reverse, DateTime, ImageSetRestriction);

            if (closeWindow)
            {
                Mvx.IoCProvider.Resolve<IExternalViewPresenter>().CloseImageSelectionOptions();
            }
        }

        public BaseImageModel[] FilterImages(BaseImageModel[] images)
        {
            if (images == null) return new BaseImageModel[] { };

            List<BaseImageModel> filteredImages = new List<BaseImageModel>();

            BaseImageModel[] filteredImagesArr;

            if (RadioAllRanks && RadioAllTypes) // if we're specifying a rank then we're be 
            {
                // no changes needed
                filteredImagesArr = images;
            }
            else
            {
                if (RadioAllRanks && !RadioAllTypes)
                {
                    // do nothing ; keep default
                }
                else if (RadioUnranked) // filter down to all unranked images
                {
                    images = ThemeUtil.RankController.GetAllUnrankedImages();
                }
                else if (RadioRanked) // filter down to all ranked images
                {
                    images = ThemeUtil.RankController.GetAllRankedImages();
                }
                else if (RadioSpecificRank)
                {
                    images = ThemeUtil.RankController.GetImagesOfRank(SpecifiedRank);
                }
                else if (RadioRankRange)
                {
                    images = ThemeUtil.RankController.GetImagesOfRankRange(MinSpecifiedRank, MaxSpecifiedRank);
                }

                if (images == null) return null;

                foreach (BaseImageModel image in images)
                {
                    // check the set for filters instead if one exists
                    if (VerifyImageType(image))
                    {
                        switch (image)
                        {
                            case ImageModel imageModel:
                            {
                                if (!imageModel.IsInImageSet)
                                {
                                    filteredImages.Add(imageModel);
                                }
                                else
                                {
                                    //! if multiple images from one set are in the search, we could accidentally add the same set multiple times without this
                                    if (!filteredImages.Contains(imageModel.ParentImageSet))
                                    {
                                        filteredImages.Add(imageModel.ParentImageSet);
                                    }
                                }

                                break;
                            }
                            
                            case ImageSetModel _:
                                filteredImages.Add(image);
                                break;
                        }
                    }
                }

                filteredImagesArr = filteredImages.ToArray();
            }

            if (TagboardFilter)
                return TagViewModel.Instance.SearchValidImagesWithTagBoard(filteredImagesArr);

            return filteredImagesArr;
        }

        private bool VerifyImageType(BaseImageModel image)
        {
            if (!RadioAllTypes)
            {
                ImageType validImageType = ImageType.None;

                if (RadioStatic) validImageType = ImageType.Static;
                if (RadioGif) validImageType = ImageType.GIF;
                if (RadioVideo) validImageType = ImageType.Video;

                return image.ImageType == validImageType;
            }

            return true;
        }

        private void PromptFolder()
        {
            RebuildImageSelectorWithOptions(FilterImages(FolderUtil.PromptValidFolderModel()?.GetImageModels()));
        }

        private void SelectActiveWallpapers()
        {
            BaseImageModel[] activeImages = ThemeUtil.Theme.WallpaperRandomizer.ActiveWallpapers.ToArray();
            RebuildImageSelectorWithOptions(activeImages);
        }

        private void SelectDisabledImages()
        {
            //x//? if we were to include images in image sets they would all be counted as "disabled", cluttering the selector
            //xBaseImageModel[] disabledImages = ThemeUtil.Theme.Images.GetAllImages().Where(f => !f.Active && !f.IsInImageSet).ToArray();
            BaseImageModel[] disabledImages;
            ImageModel[] allImages = ThemeUtil.Theme.Images.GetAllImages();

            disabledImages = IncludeDependentImages ? 
                allImages.Where(f => !f.Active).ToArray() : 
                allImages.Where(f => !f.Active && !f.IsDependentOnImageSet).ToArray();

            RebuildImageSelectorWithOptions(FilterImages(disabledImages));
        }
    }
}
