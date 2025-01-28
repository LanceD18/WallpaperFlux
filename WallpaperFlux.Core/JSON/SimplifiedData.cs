using System;
using System.Collections.Generic;
using System.Text;
using WallpaperFlux.Core.Util;

namespace WallpaperFlux.Core.JSON
{
    //? Simplified version of models/data used to minimize the size of the JSON

    public struct SimplifiedFolder
    {
        public string Path;

        public bool Enabled;

        public string PriorityName;

        public SimplifiedFolder(string path, string priorityName, bool enabled = true)
        {
            Path = path;
            Enabled = enabled;
            PriorityName = priorityName;
        }
    }

    public struct SimplifiedCategory
    {
        public string Name;

        public bool Enabled;

        public bool UseForNaming;

        public SimplifiedTag[] Tags;

        public SimplifiedCategory(string name, SimplifiedTag[] tags, bool enabled = true, bool useForNaming = true)
        {
            Name = name;
            Enabled = enabled;
            UseForNaming = useForNaming;
            Tags = tags;
        }
    }

    public struct SimplifiedTag
    {
        public string Name;

        public bool Enabled;

        public bool UseForNaming;

        public string ParentCategoryName;

        public SimplifiedParentTag[] ParentTags;

        public string RenameFolderPath;

        public SimplifiedTag(string name, string parentCategoryName, SimplifiedParentTag[] parentTags, string renameFolder, bool enabled = true, bool useForNaming = true)
        {
            Name = name;
            Enabled = enabled;
            UseForNaming = useForNaming;
            ParentCategoryName = parentCategoryName;
            ParentTags = parentTags;
            RenameFolderPath = renameFolder;
        }
    }

    public struct SimplifiedParentTag
    {
        public string Name;
        public string ParentCategoryName; //? we still need to know the name of the category, as different categories can have tags with the same name

        public SimplifiedParentTag(string name, string parentCategoryName)
        {
            Name = name;
            ParentCategoryName = parentCategoryName;
        }
    }

    // TODO Combine SimplifiedImage and SimplifiedImageSet in some form to avoid duplicate variables (can't use inheritance)
    public struct SimplifiedImage
    {
        public string Path;

        public int Rank;

        public bool Enabled;

        // TODO Consider handling similarly to SimplifiedParentTag or the other way around for both, analyze the benefits / costs to the JSON size (The dictionary method results in less category naming)
        // TODO The reason why SimplifiedParentTag was handled differently in the first place was because of it pulling directly from the property in the older version
        public Dictionary<string, List<string>> Tags; //? similar to SimplifiedParentTag, we want to know the category name to prevent duplicate name issues without loading in too much information

        public Dictionary<string, List<string>> TagNamingExceptions;

        public double Volume;

        public int MinLoops;

        public int MaxTime;

        public bool OverrideMinLoops;

        public bool OverrideMaxTime;

        public SimplifiedImage(string path, int rank, Dictionary<string, List<string>> tags, Dictionary<string, List<string>> tagNamingExceptions,
            int minLoops, int maxTime, bool overrideMinLoops, bool overrideMaxTime, bool enabled = true, double volume = -1)
        {
            Path = path;
            Rank = rank;
            Enabled = enabled;
            Tags = tags;
            TagNamingExceptions = tagNamingExceptions;

            //? the -1 acts as a default null value since volume can't be set to -1, if the volume is at this value then use the global default
            Volume = volume == -1 ? ThemeUtil.VideoSettings.DefaultVideoVolume : volume;
            //? the -1 acts as a default null value since volume can't be set to -1, if the volume is at this value then use the global default

            Volume = volume;
            MinLoops = minLoops;
            MaxTime = maxTime;
            OverrideMinLoops = overrideMinLoops;
            OverrideMaxTime = overrideMaxTime;
        }
    }

    // TODO Combine SimplifiedImage and SimplifiedImageSet in some form to avoid duplicate variables (can't use inheritance)
    public struct SimplifiedImageSet
    {
        public string[] ImagePaths;

        public int OverrideRank;

        public bool UsingAverageRank;

        public bool UsingWeightedAverage;

        public bool UsingOverrideRank;

        public bool UsingWeightedRank;

        public int OverrideRankWeight;

        public bool Enabled;

        public double Speed;

        public int MinLoops;

        public int MaxTime;

        public bool OverrideMinLoops;

        public bool OverrideMaxTime;

        public bool FractionIntervals;

        public bool StaticIntervals;

        public bool WeightedIntervals;

        public bool RetainImageIndependence;

        public ImageSetType SetType;

        public SimplifiedImageSet(string[] imagePaths, int overrideRank, bool usingAverageRank, bool usingWeightedAverage, 
            bool usingOverrideRank, bool usingWeightedRank, int overrideRankWeight, bool enabled, double speed, ImageSetType setType,
            int minLoops, int maxTime, bool overrideMinLoops, bool overrideMaxTime, bool fractionIntervals, bool staticIntervals, bool weightedIntervals, 
            bool retainImageIndependence)
        {
            ImagePaths = imagePaths;
            OverrideRank = overrideRank;
            UsingAverageRank = usingAverageRank;
            UsingWeightedAverage = usingWeightedAverage;
            UsingOverrideRank = usingOverrideRank;
            UsingWeightedRank = usingWeightedRank;
            OverrideRankWeight = overrideRankWeight;
            Enabled = enabled;
            Speed = speed;
            SetType = setType;
            MinLoops = minLoops;
            MaxTime = maxTime;
            OverrideMinLoops = overrideMinLoops;
            OverrideMaxTime = overrideMaxTime;
            FractionIntervals = fractionIntervals;
            StaticIntervals = staticIntervals;
            WeightedIntervals = weightedIntervals;
            RetainImageIndependence = retainImageIndependence;
        }
    }

    public struct SimplifiedDisplaySettings
    {
        public SimplifiedDisplaySetting[] DisplaySettings;

        public bool IsSynced;

        public SimplifiedDisplaySettings(SimplifiedDisplaySetting[] displaySettings, bool synced)
        {
            DisplaySettings = displaySettings;
            IsSynced = synced;
        }
    }

    public struct SimplifiedDisplaySetting
    {
        public int DisplayInterval;

        public IntervalType DisplayIntervalType;

        public WallpaperStyle DisplayStyle;

        public SimplifiedDisplaySetting(int displayInterval, IntervalType displayIntervalType, WallpaperStyle displayStyle)
        {
            DisplayInterval = displayInterval;
            DisplayIntervalType = displayIntervalType;
            DisplayStyle = displayStyle;
        }
    }

    public struct SimplifiedFrequencyModel
    {
        public double RelativeFrequencyStatic;

        public double RelativeFrequencyGif;

        public double RelativeFrequencyVideo;

        public bool WeightedFrequency;

        public SimplifiedFrequencyModel(double relativeFrequencyStatic, double relativeFrequencyGif, double relativeFrequencyVideo, bool weightedFrequency)
        {
            RelativeFrequencyStatic = relativeFrequencyStatic;
            RelativeFrequencyGif = relativeFrequencyGif;
            RelativeFrequencyVideo = relativeFrequencyVideo;
            WeightedFrequency = weightedFrequency;
        }
    }

    public struct SimplifiedFolderPriority
    {
        public string Name;

        public string ConflictResolutionFolder;

        public int PriorityOverride;

        public SimplifiedFolderPriority(string name, string conflictResolutionFolder, int priorityOverride = -1)
        {
            Name = name;
            ConflictResolutionFolder = conflictResolutionFolder;
            PriorityOverride = priorityOverride;
        }
    }
}
