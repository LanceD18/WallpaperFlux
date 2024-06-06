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
using LanceTools.WPF.Adonis.Util;
using Microsoft.WindowsAPICodePack.Dialogs;
using MvvmCross;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using Newtonsoft.Json;
using WallpaperFlux.Core.Collections;
using WallpaperFlux.Core.IoC;
using WallpaperFlux.Core.Models.Tagging;
using WallpaperFlux.Core.Tools;
using WallpaperFlux.Core.Util;
using WallpaperFlux.Core.ViewModels;

namespace WallpaperFlux.Core.Models
{
    //TODO You should verify if the extension is valid (Look into the methods you used for this in WallpaperManager to determine said extensions)
    public class ImageModel : BaseImageModel
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
                RaisePropertyChanged(() => PathName);
                RaisePropertyChanged(() => PathFolder);
            }
        }

        [JsonIgnore] public string PathName => new FileInfo(Path).Name;

        [JsonIgnore] public string PathFolder => new FileInfo(Path).DirectoryName;

        public FolderModel ParentFolder;

        [DataMember(Name = "Tags")] public ImageTagCollection Tags;

        public sealed override bool Enabled // this is the image's individual enabled state, if this is false then nothing else can make the image active
        {
            get => _enabled;

            set
            {
                SetProperty(ref _enabled, value);

                if (ParentFolder == null) //! this should be updated elsewhere, 'hacky' move to reduce IsEnabled() calls
                {
                    Active = false;
                    return;
                }

                UpdateEnabledState();
            }
        }

        private ImageSetModel _parentImageSet;
        public ImageSetModel ParentImageSet
        {
            get => _parentImageSet;
            set
            {
                _parentImageSet = value;

                UpdateEnabledState();
            }
        }

        public bool IsInImageSet => ParentImageSet != null;

        public bool IsDependentOnImageSet => IsInImageSet && !ParentImageSet.RetainImageIndependence;

        public bool IsDependentOnAnimatedImageSet => IsDependentOnImageSet && ParentImageSet.IsAnimated;

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
                        if (ThemeUtil.Theme.WallpaperRandomizer.ActiveWallpapers[i] is ImageModel imageModel)
                        {
                            if (WallpaperUtil.WallpaperHandler.GetWallpaperPath(i) == imageModel.Path)
                            {
                                WallpaperUtil.WallpaperHandler.UpdateVolume(i);
                            }
                        }
                    }
                }
            }
        }

        [JsonIgnore] public double ActualVolume => Volume / 100; // for use with inspector & tooltip audio sources and other related occurrences

        // Type Checkers
        // TODO Replace the external references to these values (The references in xaml) with the ImageType variable
        public bool IsStatic => WallpaperUtil.IsStatic(Path);

        public bool IsGif => WallpaperUtil.IsGif(Path);

        public override bool IsVideo => WallpaperUtil.IsVideo(Path);

        public bool IsWebmOrGif
        {
            get
            {
                string extension = System.IO.Path.GetExtension(Path);
                return extension == ".gif" || extension == ".webm";
            }
        }
        
        public bool IsMp4OrAvi //? used by WallpaperWindow.xaml.cs
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

        public IMvxCommand InspectCommand { get; set; }

        public IMvxCommand SetWallpaperCommand { get; set; }

        public IMvxCommand RenameImageCommand { get; set; }

        public IMvxCommand MoveImageCommand { get; set; }

        public IMvxCommand DeleteImageCommand { get; set; }

        public IMvxCommand RankImageCommand { get; set; }

        public IMvxCommand PasteTagBoardCommand { get; set; }

        public IMvxCommand PasteTagsToTagBoardCommand { get; set; }

        public IMvxCommand SetTagsToTagBoardCommand { get; set; }
        #endregion

        #region UI Control

        private Size _imageSize = Size.Empty; //! Do NOT save this to JSON unless you devise an efficient way to detect size changes

        #endregion

        public ImageModel(string path, int rank = 0, bool enabled = true, ImageTagCollection tags = null, FolderModel parentFolder = null, double volume = 50,
            int minimumLoops = 0, bool overrideMinimumLoops = false, int maximumTime = 0, bool overrideMaximumTime = false)
        {
            Path = _hashPath = path;

            ImageType = IsStatic // ideally this won't be changing later
                ? ImageType.Static
                : IsGif 
                    ? ImageType.GIF
                    : ImageType.Video;

            //!ParentFolder must be set before Enabled is set and after Path is set
            if (parentFolder != null)
            {
                ParentFolder = parentFolder;
            }
            else
            {
                if (!JsonUtil.IsLoadingData)
                {
                    UpdateParentFolder();
                }
                else
                {
                    ParentFolder = null;
                }
            }

            Tags = tags ?? new ImageTagCollection(this); // create a new tag collection if the given one is null

            //! Must be set *AFTER* the ImageType is set !!!!!!!!!!
            //! Must be set *AFTER* the ImageType is set !!!!!!!!!!
            //! Must also be set *AFTER* tags are created to handle checking IsEnabled() !!!!!!
            //! Must also be set *AFTER* tags are created to handle checking IsEnabled() !!!!!!
            Rank = _hashRank = rank;

            //! Tags & ParentFolder & Rank need to be set before enabled is called
            Enabled = enabled;

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
            InspectCommand = new MvxCommand(Inspect);
            SetWallpaperCommand = new MvxCommand(() => ImageUtil.SetWallpaper(this));

            RenameImageCommand = new MvxCommand(() => ImageRenamer.AutoRenameImage(this));
            MoveImageCommand = new MvxCommand(() => ImageRenamer.AutoMoveImage(this));
            DeleteImageCommand = new MvxCommand(() => ImageUtil.DeleteImage(this));

            RankImageCommand = new MvxCommand(() => ImageUtil.PromptRankImage(this));

            PasteTagBoardCommand = new MvxCommand(PasteTagBoard);
            PasteTagsToTagBoardCommand = new MvxCommand(() => TaggingUtil.AddTagsToTagboard(Tags.GetTags().ToArray()));
            SetTagsToTagBoardCommand = new MvxCommand(() =>
            {
                TaggingUtil.ClearTagboard();
                TaggingUtil.AddTagsToTagboard(Tags.GetTags().ToArray());
            });
        }

        public void Inspect()
        {
            WallpaperFluxViewModel.Instance.DeselectAllImages();

            this.IsSelected = true;
            WallpaperFluxViewModel.Instance.SelectedImage = this;

            WallpaperFluxViewModel.Instance.OpenInspector();
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

        public override bool IsEnabled(bool ignoreSet = false) //! ignore set ensures that we can avoid disabling images that are in sets *when they are needed*
        {
            if (!base.IsEnabled(ignoreSet))
            {
                Active = false;
                return false;
            }

            //xif (!ignoreSet)
            //x{
                Active = false; //! we need to set this to false AGAIN because base.IsEnabled() will set Active to TRUE if successful
            //x}

            if (IsDependentOnImageSet && !ignoreSet) return false;

            if (ParentFolder == null)
            {
                if (!UpdateParentFolder())
                {
                    return false; //! this indicates that we weren't able to find a parent folder, ideally this means that the folder is currently being added, otherwise, this shouldn't happen
                }
            }

            if (ParentFolder == null) 
            {
                Debug.Write("Error: Parent Folder still null after updating on image: " + Path);
                return false; // if still null, return false
            }

            if (!ParentFolder.Enabled) return false;

            if (!Tags.AreTagsEnabled()) return false;

            if (!ignoreSet) // don't change the active state if we are ignoring the set
            {
                Active = true; // if we reach this point, then the image is in fact Active
            }

            return true;
        }

        public bool UpdateParentFolder()
        {
            ParentFolder = FolderUtil.GetFolderModel(PathFolder);

            return ParentFolder != null;
        }

        #region Tags
        public void AddTag(TagModel tag, bool highlightTags) => Tags.Add(tag, highlightTags);

        public void RemoveTag(TagModel tag, bool highlightTags) => Tags.Remove(tag, highlightTags);

        public void RemoveAllTags() => Tags.RemoveAllTags();

        public void PasteTagBoard()
        {
            foreach (TagModel tag in TagViewModel.Instance.TagBoardTags)
            {
                AddTag(tag, false); //! TAG HIGHLIGHT DONE BELOW
            }

            TaggingUtil.HighlightTags();
        }

        public string GetTaggedName() => Tags.GetTaggedName();

        public bool ContainsTag(TagModel tag) => Tags.Contains(tag);

        public bool ContainsChildTag(TagModel tag) //? this means that the image may not have this tag but *does* have a child tag of this tag
        {
            if (tag == null) return false;

            foreach (TagModel childTag in tag.GetChildTags())
            {
                if (ContainsTagOrChildTag(childTag))
                {
                    return true;
                }
            }

            return false;
        }

        public bool ContainsTagOrChildTag(TagModel tag)
        {
            return ContainsTag(tag) || ContainsChildTag(tag);
        }

        public void AddTagNamingException(TagModel tag) => Tags.AddNamingException(tag);
        #endregion

        #region Command Methods
        // opens the file's folder in the explorer and selects it to navigate the scrollbar to the file
        public void ViewFile()
        {
            if (!MessageBoxUtil.FileExists(Path)) return;
            ProcessUtil.SelectFile(Path);
        }

        // opens the file
        public void OpenFile()
        {
            if (!MessageBoxUtil.FileExists(Path)) return;
            ProcessUtil.OpenFile(Path);
        }
        #endregion

        protected bool Equals(ImageModel other)
        {
            return _path == other._path;
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
        //? for use with dictionary addition
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