using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using Newtonsoft.Json;

namespace WallpaperFlux.Core.JSON.Temp
{
    public struct TempVideoSettings
    {
        public int Volume;
        public double PlaybackSpeed;

        public TempVideoSettings(int Volume, double PlaybackSpeed)
        {
            this.Volume = Volume;
            this.PlaybackSpeed = PlaybackSpeed;
        }
    }

    public class TempImageData
    {
        [DataMember(Name = "Path")] public string Path { get; private set; } //? If you need to change this, FileData's field must be changed into List<ImageData>

        [JsonIgnore] public string PathFolder { get; private set; }

        [DataMember(Name = "Rank")] private int _Rank;

        public int Rank
        {
            get => _Rank;

            set
            {
                /*x
                if (value <= GetMaxRank() && value >= 0) // prevents stepping out of valid rank bounds
                {
                    if (Active) // Rank Data does not include inactive images
                    {
                        RankData[_Rank].Remove(Path);
                        RankData[value].Add(Path);

                        ImagesOfTypeRankData[imageType][_Rank].Remove(Path);
                        ImagesOfTypeRankData[imageType][value].Add(Path);
                    }

                    _Rank = value; // place this after the above if statement to ensure that the right image file path is found
                }
                */
            }
        }

        [DataMember(Name = "Active")] private bool _Active;

        public bool Active
        {
            get => _Active;

            private set
            {
                /*x
                if (_Active != value) // Prevents element duplication whenever active is set to the same value
                {
                    _Active = value;

                    if (value)
                    {
                        RankData[_Rank].Add(Path);
                        ImagesOfTypeRankData[imageType][_Rank].Add(Path);

                        ActiveImages.Add(Path);
                        ActiveImagesOfType[imageType].Add(Path);
                    }
                    else  // Note that Rank Data does not include inactive images
                    {
                        RankData[_Rank].Remove(Path);
                        ImagesOfTypeRankData[imageType][_Rank].Remove(Path);

                        ActiveImages.Remove(Path);
                        ActiveImagesOfType[imageType].Remove(Path);
                    }
                }
                */
            }
        }

        [DataMember(Name = "Enabled")] private bool _Enabled = true;

        public bool Enabled // this is the image's individual enabled state, if this is false then nothing else can make the image active
        {
            get => _Enabled;

            set
            {
                /*x
                _Enabled = value;
                Active = value;
                */
            }
        }

        [DataMember(Name = "Tags")] public Dictionary<string, HashSet<string>> Tags; // this should stay as a string for saving to JSON | Represents: Dictionary<CategoryName, HashSet<TagName>>

        [DataMember(Name = "Tag Naming Exceptions")]
        public HashSet<Tuple<string, string>> TagNamingExceptions; // these tags be used for naming regardless of constraints

        [DataMember(Name = "Image Type")] public ImageType imageType;

        [DataMember(Name = "Video Settings")] public TempVideoSettings VideoSettings = new TempVideoSettings(100, 1); // only applicable to images with the corresponding image type


        public TempImageData()
        {

        }
    }
}
