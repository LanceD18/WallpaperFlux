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

        public int PriorityIndex;

        public SimplifiedFolder(string path, bool enabled, int priorityIndex)
        {
            Path = path;
            Enabled = enabled;
            PriorityIndex = priorityIndex;
        }
    }

    public struct SimplifiedCategory
    {
        public string Name;

        public bool Enabled;

        public bool UseForNaming;

        public SimplifiedTag[] Tags;

        public SimplifiedCategory(string name, bool enabled, bool useForNaming, SimplifiedTag[] tags)
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

        public SimplifiedTag(string name, bool enabled, bool useForNaming, string parentCategoryName, SimplifiedParentTag[] parentTags, string renameFolder)
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

    public struct SimplifiedImage
    {
        public string Path;

        public int Rank;

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
            int minLoops, int maxTime, bool overrideMinLoops, bool overrideMaxTime, double volume = -1)
        {
            Path = path;
            Rank = rank;
            Tags = tags;
            TagNamingExceptions = tagNamingExceptions;
            //? the -1 acts as a default null value since volume can't be set to -1, if the volume is at this value then use the global default
            Volume = volume == -1 ? ThemeUtil.VideoSettings.DefaultVideoVolume : volume;
            Volume = volume;
            MinLoops = minLoops;
            MaxTime = maxTime;
            OverrideMinLoops = overrideMinLoops;
            OverrideMaxTime = overrideMaxTime;
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

        public SimplifiedFolderPriority(string name, string conflictResolutionFolder)
        {
            Name = name;
            ConflictResolutionFolder = conflictResolutionFolder;
        }
    }
}
