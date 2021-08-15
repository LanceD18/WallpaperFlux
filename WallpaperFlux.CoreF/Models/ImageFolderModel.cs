using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using LanceTools.DiagnosticsUtil;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using WallpaperFlux.Core.Util;

namespace WallpaperFlux.Core.Models
{
    public class ImageFolderModel : MvxNotifyPropertyChanged
    {
        public string Path { get; set; }

        private bool _active;

        public bool Active
        {
            get => _active;

            set
            {
                _active = value;
                RaisePropertyChanged(() => Active);
                ImageFolderUtil.ThemeImageFolders.ValidateImageFolders();
            }
        }

        public ImageFolderModel()
        {
            ViewFolderCommand = new MvxCommand(ViewFolder);
        }

        public IMvxCommand ViewFolderCommand { get; set; }

        public void ViewFolder()
        {
            if (!ValidationUtil.DirectoryExists(Path)) return;
            ProcessUtil.OpenFile(Path);
        }
    }
}
