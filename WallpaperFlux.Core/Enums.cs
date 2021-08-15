using System;
using System.Collections.Generic;
using System.Text;

namespace WallpaperFlux.Core
{
    public enum ImageType
    {
        None,
        Static,
        GIF,
        Video
    }

    public enum SelectionType
    {
        None,
        Active,
        All
    }

    public enum FrequencyType
    {
        Relative,
        Exact
    }

    public enum WallpaperStyle
    {
        Fill,
        Stretch,
        Fit,
        Center
    }

    public enum IntervalType
    {
        None = 0,
        Seconds = 1,
        Minutes = 60,
        Hours = 3600
    }

    /*x
    
    //!temp
    public enum WallpaperStyle
    {
        [Description("Fill")]
        Fill,
        [Description("Stretch")]
        Stretch,
        [Description("Zoom")]
        Zoom,
        [Description("Center")]
        Center
    }
    //!temp

    //!temp
    public enum IntervalType
    {
        [Description("None")]
        None,
        [Description("Seconds")]
        Seconds,
        [Description("Minutes")]
        Minutes,
        [Description("Hours")]
        Hours
    }
    //!temp

    */
}
