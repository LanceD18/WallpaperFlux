using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using AdonisUI.Controls;
using LanceTools.DiagnosticsUtil;
using Microsoft.WindowsAPICodePack.Dialogs;
using MvvmCross;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using Newtonsoft.Json;
using WallpaperFlux.Core.Collections;
using WallpaperFlux.Core.External;
using WallpaperFlux.Core.Models.Controls;
using WallpaperFlux.Core.Models.Tagging;
using WallpaperFlux.Core.Util;
using WallpaperFlux.Core.ViewModels;

namespace WallpaperFlux.Core.Models
{
    //TODO You should verify if the extension is valid (Look into the methods you used for this in WallpaperManager to determine said extensions)
    public class ImageModel : ListBoxItemModel
    {
        // Properties
        [DataMember(Name = "Path")]
        public string Path { get; set; }

        [JsonIgnore]
        public string PathFolder => new FileInfo(Path).DirectoryName;

        [DataMember(Name = "Rank")]
        private int _rank;
        public int Rank
        {
            get => _rank;
            set
            {
                /*
                if (ImageType == ImageType.None) // either this is an invalid file or the image has not been instantiated yet
                {

                }

                if (ImageType == ImageType.None) // this is an invalid file, end
                {
                    // TODO Include with the UI Logger
                    Debug.WriteLine("Invalid File Encountered: " + Path);
                    return;
                }
                */

                DataUtil.Theme.RankController.ModifyRank(this, _rank, ref value); //? this should be called first to allow the old rank to be identified
                _rank = value;
                RaisePropertyChanged(() => Rank);
            }
        }

        [JsonIgnore] public Action<int, int> OnRankChange; //! Don't think you'll be using this, remove it at some point

        // Note: A rank 0 image is still active if able, it just has a 0% chance of being selected
        [DataMember(Name = "Active")]
        public bool Active { get; set; }

        // TODO The original ImageData also had an independent enabled boolean for the image itself, I think it would be better to have these overriding enables merged into 1 boolean
        // TODO Folders can be checked as active elsewhere, same with tags, this could potentially just be the original "Enabled" from imageData
        // TODO Remember that this had to remove the image from RankData on disabling for calculation purposes, may need to do this again (As opposed to looping the RankData each time)
        /*x
        [DataMember(Name = "Enabled")]
        private bool _Enabled = true;
        public bool Enabled // this is the image's individual enabled state, if this is false then nothing else can make the image active
        {
            get => _Enabled;

            set
            {
                _Enabled = value;
                Active = value;
            }
        }
        */

        [DataMember(Name = "Tags")] public TagCollection Tags;

        [DataMember(Name = "Image Type")] public ImageType ImageType { get; set; }

        // Video Properties
        public double Volume { get; set; } = 0.5;

        public double Speed { get; set; }

        // Type Checkers
        // TODO Replace the external references to these values (The references in xaml) with the ImageType variable
        public bool IsStatic => !(new FileInfo(Path).Extension == ".gif" || WallpaperUtil.IsSupportedVideoType(Path));
        
        public bool IsGIF => new FileInfo(Path).Extension == ".gif";

        public bool IsVideo => WallpaperUtil.IsSupportedVideoType(Path);

        // Commands
        public IMvxCommand ViewFileCommand { get; set; }

        public IMvxCommand OpenFileCommand { get; set; }

        public IMvxCommand SetWallpaperCommand { get; set; }

        public IMvxCommand DecreaseRankCommand { get; set; }

        public IMvxCommand IncreaseRankCommand { get; set; }

        public IMvxCommand PasteTagBoardCommand { get; set; }

        /*x
        // IoC Property
        private IExternalImageSource _imageSource;

        public IExternalImageSource ImageSource
        {
            get
            {
                _imageSource.InitCompressedSource(Path, 200, 200);
                return _imageSource;
            }
            set { _imageSource = value; }
        }
        */

        //xpublic IExternalImageSource ImageSource { get; set; }

        // ----- XAML Values -----
        // TODO Replace this section with ResourceDictionaries at some point
        #region XAML Values
        [JsonIgnore] public int ImageSelectorSettingsHeight => 25;

        [JsonIgnore] public int ImageSelectorThumbnailHeight => 150;

        [JsonIgnore] public int ImageSelectorThumbnailWidth => 150;

        [JsonIgnore] public int ImageSelectorThumbnailWidthVideo => ImageSelectorThumbnailWidth - 20; // until the GroupBox is no longer needed this will account for it
        #endregion

        #region UI Control

        /*
        private bool _isSelected;
        [JsonIgnore]
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (IsSelected != value) // this should only update if there's actually a change
                {
                    WallpaperFluxViewModel.Instance.SelectedImageCount += value ? 1 : -1;
                }
                
                SetProperty(ref _isSelected, value);
            }
        }
        */

        #endregion

        public ImageModel(string path, int rank = 0, TagCollection tags = null)
        {
            Path = path;

            ImageType = IsStatic 
                ? ImageType.Static 
                : IsGIF 
                    ? ImageType.GIF 
                    : ImageType.Video;

            Rank = rank;

            Tags = tags ?? new TagCollection(this); // create a new tag collection if the given one is null

            OnIsSelectedChanged += (value) =>
            {
                if (IsSelected != value) // this should only update if there's actually a change
                {
                    WallpaperFluxViewModel.Instance.SelectedImageCount += value ? 1 : -1;
                }
            };

            InitCommands();
        }

        private void InitCommands()
        {
            ViewFileCommand = new MvxCommand(ViewFile);
            OpenFileCommand = new MvxCommand(OpenFile);
            SetWallpaperCommand = new MvxCommand(SetWallpaper);

            DecreaseRankCommand = new MvxCommand(() => Rank--);
            IncreaseRankCommand = new MvxCommand(() => Rank++);

            PasteTagBoardCommand = new MvxCommand(PasteTagBoard);
        }

        public Size GetSize()
        {
            if (!IsVideo)
            {
                IExternalImage imageSource = Mvx.IoCProvider.Resolve<IExternalImage>();
                imageSource.SetImage(Path);
                return imageSource.GetSize();
            }
            else // TODO Implement a process for getting the video size
            {
                return new Size(0, 0);
            }
        }

        #region Tags
        public void AddTag(TagModel tag) => Tags.Add(tag);

        public void RemoveTag(TagModel tag) => Tags.Remove(tag);

        public void PasteTagBoard()
        {
            foreach (TagModel tag in TagViewModel.Instance.TagBoardTags)
            {
                AddTag(tag);
            }
        }
        #endregion

        #region Command Methods
        // opens the file's folder in the explorer and selects it to navigate the scrollbar to the file
        public void ViewFile()
        {
            if (!ValidationUtil.FileExists(Path)) return;
            ProcessUtil.SelectFile(Path);
        }

        // opens the file
        public void OpenFile()
        {
            if (!ValidationUtil.FileExists(Path)) return;
            ProcessUtil.OpenFile(Path);
        }

        private const string DISPLAY_DEFAULT_ID = "display";
        public void SetWallpaper()
        {
            int displayIndex = 0;
            if (WallpaperUtil.DisplayUtil.GetDisplayCount() > 1) // this MessageBox will only appear if the user has more than one display
            {
                // Create [Choose Display] MessageBox
                IMessageBoxButtonModel[] buttons = new IMessageBoxButtonModel[WallpaperUtil.DisplayUtil.GetDisplayCount()];
                for (int i = 0; i < buttons.Length; i++)
                {
                    buttons[i] = MessageBoxButtons.Custom("Display " + (i + 1), DISPLAY_DEFAULT_ID + i);
                }

                MessageBoxModel messageBox = new MessageBoxModel
                {
                    Text = "Choose a display",
                    Caption = "Choose an option",
                    Icon = MessageBoxImage.Question,
                    Buttons = buttons
                };

                // Display [Choose Display] MessageBox
                MessageBox.Show(messageBox);

                // Evaluate [Choose Display] MessageBox
                for (int i = 0; i < buttons.Length; i++)
                {
                    if ((string)messageBox.ButtonPressed.Id == (DISPLAY_DEFAULT_ID + i))
                    {
                        displayIndex = i;
                        break;
                    }
                }
            }

            WallpaperUtil.SetWallpaper(displayIndex, Path, true); // no randomization required here
        }
        #endregion
    }
}