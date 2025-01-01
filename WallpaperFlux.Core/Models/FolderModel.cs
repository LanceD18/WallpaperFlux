using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using LanceTools.DiagnosticsUtil;
using LanceTools.IO;
using LanceTools.WPF.Adonis.Util;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using WallpaperFlux.Core.Util;
using WallpaperFlux.Core.ViewModels;

namespace WallpaperFlux.Core.Models
{
    // the FolderModel will pick up every file within it and attempt to enable/disable it based on the model's enabled state & whether or not the file is within the theme
    public class FolderModel : MvxNotifyPropertyChanged
    {
        private string _path;

        public string Path
        {
            get => _path;

            private set => _path = value;
        }

        // TODO Rename this to Enabled to match with similar functions across other aspects of the program
        private bool _enabled;

        public bool Enabled
        {
            get => _enabled;

            set
            {
                SetProperty(ref _enabled, value);
                ValidateImages(true);
            }
        }

        public string PriorityName { get; set; }

        private List<string> _images
        {
            get
            {
                if (Directory.Exists(Path))
                {
                    return new DirectoryInfo(Path).GetFiles().Select((s) => s.FullName).ToList();
                }

                return new List<string>();
            }
        }

        public IMvxCommand ViewFolderCommand { get; set; }

        public IMvxCommand SelectFolderImagesCommand { get; set; }

        public IMvxCommand SelectFolderImagesSelectionFilterCommand { get; set; }

        public FolderModel(string path, bool enabled, string priorityName = "")
        {
            if (!Directory.Exists(path))
            {
                path = "INVALID PATH ERROR: " + path;
                Path = path;
                return; //! doing anything else could cause errors as this folder is invalid
            }

            //? internally adds the images to the model (see setter)
            Path = path;

            //! this will internally validate the image folder so this must be placed after the images are added
            Enabled = enabled;

            PriorityName = priorityName;

            ViewFolderCommand = new MvxCommand(ViewFolder);
            SelectFolderImagesCommand = new MvxCommand(() => WallpaperFluxViewModel.Instance.RebuildImageSelector(GetImageModels()));
            SelectFolderImagesSelectionFilterCommand = new MvxCommand(() => 
                ImageSelectionViewModel.Instance.RebuildImageSelectorWithOptions(ImageSelectionViewModel.Instance.FilterImages(GetImageModels()), false));
        }

        public void ViewFolder()
        {
            if (!MessageBoxUtil.DirectoryExists(Path)) return;
            ProcessUtil.OpenFile(Path);
        }

        // updated the enabled state of each image based on the folder's enabled state
        // if the image does not exist, add it to the theme
        public void ValidateImages(bool updateEnabledState)
        {
            //! This currently doesn't check for non-image files

            if (JsonUtil.IsLoadingData) return; //? we will call all folders once loading is finished, this processes tackles both the Image Collection AND Rank Controller

            foreach (string imagePath in _images)
            {
                if (FileUtil.Exists(imagePath)) //? no need to delete from _images if the image does not exist considering how the { get; } function of _images works
                {
                    ImageModel image = ThemeUtil.Theme.Images.GetImage(imagePath);

                    if (image == null) // new image found
                    {
                        image = ThemeUtil.Theme.Images.AddImage(imagePath, this);
                        if (image == null) continue; // ? invalid image type gathered

                        image.UpdateEnabledState(); //? we will always update the enabled state of new images to ensure that they are made active
                    }
                    else // check if existing image is enabled
                    {
                        if (image.ImageType == ImageType.None) continue; // ? previously valid image type is now invalid, do not add

                        //xif (!JsonUtil.IsLoadingData) ThemeUtil.Theme.Images.AddImage(image, this); //? the image will add itself while loading
                        image.ParentFolder = this;

                        if (updateEnabledState) image.UpdateEnabledState(); //? so that we don't have to loop twice to process changes to Enabled
                    }
                }
            }
        }

        public void RemoveAllImagesOfFolder()
        {
            foreach (string image in _images)
            {
                ThemeUtil.Theme.Images.RemoveImage(image);
            }
        }

        public string[] GetImagePaths()
        {
            ValidateImages(false); // check for new images

            return _images.ToArray();
        }

        public ImageModel[] GetImageModels() => ThemeUtil.Theme.Images.GetImageRange(GetImagePaths());
    }
}
