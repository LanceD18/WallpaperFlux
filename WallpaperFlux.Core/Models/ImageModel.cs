using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using AdonisUI.Controls;
using LanceTools;
using LanceTools.DiagnosticsUtil;
using Microsoft.WindowsAPICodePack.Dialogs;
using MvvmCross;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using Newtonsoft.Json;
using WallpaperFlux.Core.Collections;
using WallpaperFlux.Core.IoC;
using WallpaperFlux.Core.Models.Controls;
using WallpaperFlux.Core.Models.Tagging;
using WallpaperFlux.Core.Tools;
using WallpaperFlux.Core.Util;
using WallpaperFlux.Core.ViewModels;

namespace WallpaperFlux.Core.Models
{
    //TODO You should verify if the extension is valid (Look into the methods you used for this in WallpaperManager to determine said extensions)
    public class ImageModel : ListBoxItemModel
    {
        //? without making these readonly the hashcode could possibly be modified and the image's reference in various Dictionaries or HashSets would be lost
        private readonly string _hashPath;
        private readonly int _hashRank;

        // Properties
        private string _path;
        [DataMember(Name = "Path")]
        public string Path
        {
            get => _path;
            set
            {
                //? this should be called before setting the value so that the proper old path is obtainable, and also so that this isn't called while first setting up the image
                if (_path != null)
                {
                    ThemeUtil.Theme.Images.UpdateImageCollectionPath(this, _path, value);
                    //? RankController uses the image file itself so it shouldn't need updating
                } 

                _path = value;
            }
        }

        [JsonIgnore] public string PathName => new FileInfo(Path).Name;

        [JsonIgnore] public string PathFolder => new FileInfo(Path).DirectoryName;

        private int _rank;
        [DataMember(Name = "Rank")]
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

                ThemeUtil.Theme.RankController.ModifyRank(this, _rank, ref value); //? this should be called first to allow the old rank to be identified
                SetProperty(ref _rank, value);
            }
        }

        [JsonIgnore] public Action<int, int> OnRankChange; //! Don't think you'll be using this, remove it at some point
        
        // TODO This should become a non-saved variable, the saved variable should be 'Enabled' while this Active variables just determines if the image can be used as a wallpaper
        // TODO Active would factor in more than just the image's Enabled state, but the Enabled state of every tag it uses, the categories of those tags, and the folder it's in
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

        [DataMember(Name = "Tags")] public ImageTagCollection Tags;

        [DataMember(Name = "Image Type")] public ImageType ImageType { get; set; }

        // Video Properties
        private double _volume = 50;
        public double Volume
        {
            get => _volume;
            set
            {
                SetProperty(ref _volume, MathE.Clamp(value, 0, 100));
                RaisePropertyChanged(() => ActualVolume);

                if (!JsonUtil.IsLoadingData)
                {
                    for (int i = 0; i < WallpaperUtil.DisplayUtil.GetDisplayCount(); i++) 
                    {
                        if (WallpaperUtil.WallpaperHandler.GetWallpaperPath(i) == ThemeUtil.Theme.WallpaperRandomizer.ActiveWallpapers[i])
                        {
                            WallpaperUtil.WallpaperHandler.UpdateVolume(i);
                        }
                    }
                   
                }
            }
        }

        [JsonIgnore] public double ActualVolume => Volume / 100; // for use with inspector & tooltip audio sources and other related occurrences

        // Video & Gif Properties
        public double Speed { get; set; } = 1;

        public int MinimumLoops { get; set; }

        private bool _overrideMinimumLoops;
        public bool OverrideMinimumLoops
        {
            get => _overrideMinimumLoops;
            set => SetProperty(ref _overrideMinimumLoops, value);
        }

        public int MaximumTime { get; set; }

        private bool _overrideMaximumTime;
        public bool OverrideMaximumTime
        {
            get => _overrideMaximumTime;
            set => SetProperty(ref _overrideMaximumTime, value);
        }

        // Type Checkers
        // TODO Replace the external references to these values (The references in xaml) with the ImageType variable
        public bool IsStatic => WallpaperUtil.IsStatic(Path);

        public bool IsGif => WallpaperUtil.IsGif(Path);

        public bool IsVideo => WallpaperUtil.IsVideo(Path);

        public bool IsWebmOrGif
        {
            get
            {
                string extension = System.IO.Path.GetExtension(Path);
                return extension == ".gif" || extension == ".webm";
            }
        }

        public bool IsMp4OrAvi
        {
            get
            {
                string extension = System.IO.Path.GetExtension(Path);
                return extension == ".mp4" || extension == ".avi";
            }
        }

        public bool IsVideoOrGif => IsVideo || IsGif;

        // Commands
        #region Commands
        public IMvxCommand ViewFileCommand { get; set; }

        public IMvxCommand OpenFileCommand { get; set; }

        public IMvxCommand SetWallpaperCommand { get; set; }

        public IMvxCommand RenameImageCommand { get; set; }

        public IMvxCommand MoveImageCommand { get; set; }

        public IMvxCommand DeleteImageCommand { get; set; }

        public IMvxCommand RankImageCommand { get; set; }

        public IMvxCommand PasteTagBoardCommand { get; set; }

        public IMvxCommand DecreaseRankCommand { get; set; }

        public IMvxCommand IncreaseRankCommand { get; set; }
        #endregion

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

        //? Replaced by ListBoxItemModel
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

        private Size _imageSize = Size.Empty; //! Do NOT save this to JSON unless you devise an efficient way to detect size changes

        #endregion

        public ImageModel(string path, int rank = 0, ImageTagCollection tags = null, double volume = 50,
            int minimumLoops = 0, bool overrideMinimumLoops = false, int maximumTime = 0, bool overrideMaximumTime = false)
        {
            Path = _hashPath = path;

            ImageType = IsStatic 
                ? ImageType.Static 
                : IsGif 
                    ? ImageType.GIF 
                    : ImageType.Video;

            //! Must be set *AFTER* the ImageType is set !!!!!!!!!!
            //! Must be set *AFTER* the ImageType is set !!!!!!!!!!
            //! Must be set *AFTER* the ImageType is set !!!!!!!!!!
            Rank = _hashRank = rank;

            Tags = tags ?? new ImageTagCollection(this); // create a new tag collection if the given one is null

            Volume = volume;

            MinimumLoops = minimumLoops;
            OverrideMinimumLoops = overrideMinimumLoops;
            MaximumTime = maximumTime;
            OverrideMaximumTime = overrideMaximumTime;

            //? this is only called if there is actually a change, if the same value was sent in then nothing will happen
            // increments or decrements based on the IsSelected state
            OnIsSelectedChanged += (value) => WallpaperFluxViewModel.Instance.SelectedImageCount += value ? 1 : -1;

            InitCommands();
        }

        private void InitCommands()
        {
            ViewFileCommand = new MvxCommand(ViewFile);
            OpenFileCommand = new MvxCommand(OpenFile);
            SetWallpaperCommand = new MvxCommand(SetWallpaper);

            RenameImageCommand = new MvxCommand(() => ImageRenamer.AutoRenameImage(this));
            MoveImageCommand = new MvxCommand(() => ImageRenamer.AutoMoveImage(this));
            DeleteImageCommand = new MvxCommand(() => ImageUtil.DeleteImage(this));

            RankImageCommand = new MvxCommand(() => ImageUtil.PromptRankImage(this));
            DecreaseRankCommand = new MvxCommand(() => Rank--);
            IncreaseRankCommand = new MvxCommand(() => Rank++);

            PasteTagBoardCommand = new MvxCommand(PasteTagBoard);
        }

        public Size GetSize()
        {
            if (_imageSize == Size.Empty) //! initializing the size multiple times would bog down resources, so just set it after the first call and be done with it, it won't change in 99% of cases
            {
                if (!IsVideo)
                {
                    using (IExternalImage imageSource = Mvx.IoCProvider.Resolve<IExternalImage>())
                    {
                        imageSource.SetImage(Path);
                        _imageSize = imageSource.GetSize();
                    }

                    return _imageSize;
                }
                else
                {
                    //! bugged, just returns 0x0
                    /* TODO Find a different solution, double check that the video you think is loading is actually there
                    using (IExternalMediaElement mediaSource = Mvx.IoCProvider.Resolve<IExternalMediaElement>())
                    {
                        mediaSource.SetMediaElement(Path);
                        _imageSize = new Size(mediaSource.GetWidth(), mediaSource.GetHeight());
                    }
                    */

                    return _imageSize;
                }
            }
            else
            {
                return _imageSize;
            }
        }

        public void UpdatePath(string newPath) => Path = newPath; //? UPDATES TO OTHER PROPERTIES DONE IN THE SETTER OF PATH


        #region Tags
        public void AddTag(TagModel tag, bool highlightTags) => Tags.Add(tag, highlightTags);

        public void RemoveTag(TagModel tag, bool highlightTags) => Tags.Remove(tag, highlightTags);

        public void RemoveAllTags() => Tags.RemoveAllTags();

        public void PasteTagBoard()
        {
            foreach (TagModel tag in TagViewModel.Instance.TagBoardTags)
            {
                AddTag(tag, false);
            }

            TaggingUtil.HighlightTags();
        }

        public string GetTaggedName() => Tags.GetTaggedName();

        public bool ContainsTag(TagModel tag) => Tags.Contains(tag);

        public void AddTagNamingException(TagModel tag) => Tags.AddNamingException(tag);
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

            WallpaperUtil.SetWallpaper(displayIndex, Path, true, true); // no randomization required here
        }
        #endregion

        protected bool Equals(ImageModel other)
        {
            return _path == other._path && _rank == other._rank;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ImageModel)obj);
        }

        //? remember that these can be generated under Resharper with Resharper -> Edit -> Generate Code -> Equality Members
        //? https://stackoverflow.com/questions/14652567/is-there-a-way-to-auto-generate-gethashcode-and-equals-with-resharper
        // for use with dictionary addition
        public override int GetHashCode()
        {
            //? we need to use readonly variables here to ensure that the hashcode is not lost, hence the hashPath & hashRank
            unchecked
            {
                return ((_hashPath != null ? _hashPath.GetHashCode() : 0) * 397) ^ _hashRank;
            }
        }
    }
}