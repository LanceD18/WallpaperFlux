﻿using System;
using System.Collections.Generic;
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

        #endregion

        #region Commands

        public IMvxCommand SelectImagesCommand { get; set; }
        
        public IMvxCommand SelectImagesOfTypeCommand { get; set; }
        
        public IMvxCommand SelectImagesInFolderCommand { get; set; }
        
        public IMvxCommand SelectActiveWallpapersCommand { get; set; }

        public IMvxCommand SelectDisabledImagesCommand { get; set; }

        #endregion

        public ImageSelectionViewModel()
        {
            SelectImagesCommand = new MvxCommand( () => RebuildImageSelectorWithOptions(FilterImages(ThemeUtil.Theme.Images.GetAllImages())));
            SelectImagesOfTypeCommand = new MvxCommand(PromptImageType);
            SelectImagesInFolderCommand = new MvxCommand(PromptFolder);
            SelectActiveWallpapersCommand = new MvxCommand(SelectActiveWallpapers);
            SelectDisabledImagesCommand = new MvxCommand(SelectDisabledImages);
        }

        public void RebuildImageSelectorWithOptions(BaseImageModel[] images, bool closeWindow = true)
        {
            WallpaperFluxViewModel.Instance.RebuildImageSelector(images, Randomize, Reverse, DateTime);

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

            Func<BaseImageModel, bool> rankFilter = null;

            if (RadioAllRanks && RadioAllTypes) // if we're specifying a rank then we're be 
            {
                // no changes needed
                filteredImagesArr = images;
            }
            else
            {
                if (RadioAllRanks && !RadioAllTypes)
                {
                    rankFilter = image => true;
                }
                else if (RadioUnranked) // filter down to all unranked images
                {
                    rankFilter = image => ImageUtil.GetRank(image) == 0;
                }
                else if (RadioRanked) // filter down to all ranked images
                {
                    rankFilter = image => ImageUtil.GetRank(image) != 0;
                }
                else if (RadioSpecificRank)
                {
                    rankFilter = image => ImageUtil.GetRank(image) == SpecifiedRank;
                }
                else if (RadioRankRange)
                {
                    rankFilter = image => ImageUtil.GetRank(image) >= MinSpecifiedRank && ImageUtil.GetRank(image) <= MaxSpecifiedRank;
                }

                if (rankFilter == null) return null;

                foreach (ImageModel image in images)
                {
                    if (rankFilter(image) && VerifyImageType(image))
                    {
                        filteredImages.Add(image);
                    }
                }

                filteredImagesArr = filteredImages.ToArray();
            }

            if (TagboardFilter)
            {
                return TagViewModel.Instance.SearchValidImagesWithTagBoard(filteredImagesArr);
            }

            return filteredImagesArr;
        }

        private bool VerifyImageType(ImageModel image)
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

        private const string STATIC_BUTTON_ID = "static";
        private const string GIF_BUTTON_ID = "gif";
        private const string VIDEO_BUTTON_ID = "video";
        private void PromptImageType()
        {
            // ----- Create Button -----
            MessageBoxModel messageBox = new MessageBoxModel
            {
                Text = "Choose an image type",
                Caption = "Choose an option",
                Icon = MessageBoxImage.Question,
                Buttons = new[] { MessageBoxButtons.Custom("Static", STATIC_BUTTON_ID),
                    MessageBoxButtons.Custom("GIF", GIF_BUTTON_ID),
                    MessageBoxButtons.Custom("Video", VIDEO_BUTTON_ID)
                }
            };

            MessageBox.Show(messageBox);

            // ----- Evaluate Button Result -----
            if ((string)messageBox.ButtonPressed.Id == STATIC_BUTTON_ID)
            {
                RebuildImageSelectorWithOptions(FilterImages(ThemeUtil.Theme.RankController.GetAllImagesOfType(ImageType.Static)));
            }
            else if ((string)messageBox.ButtonPressed.Id == GIF_BUTTON_ID)
            {
                RebuildImageSelectorWithOptions(FilterImages(ThemeUtil.Theme.RankController.GetAllImagesOfType(ImageType.GIF)));
            }
            else if ((string)messageBox.ButtonPressed.Id == VIDEO_BUTTON_ID)
            {
                RebuildImageSelectorWithOptions(FilterImages(ThemeUtil.Theme.RankController.GetAllImagesOfType(ImageType.Video)));
            }
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
            BaseImageModel[] disabledImages = ThemeUtil.Theme.Images.GetAllImages().Where(f => !f.Active).ToArray();

            RebuildImageSelectorWithOptions(disabledImages);
        }
    }
}
