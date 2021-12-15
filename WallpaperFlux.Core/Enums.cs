using System;
using System.Collections.Generic;
using System.Text;

namespace WallpaperFlux.Core
{
    // TODO Consider splitting this to namespaces more closely associated with each particular enum
    // TODO Consider splitting this to namespaces more closely associated with each particular enum
    // TODO Consider splitting this to namespaces more closely associated with each particular enum
    // TODO Consider splitting this to namespaces more closely associated with each particular enum
    // TODO Consider splitting this to namespaces more closely associated with each particular enum
    // TODO Consider splitting this to namespaces more closely associated with each particular enum

    // TODO Do the above *especially* for enums that are only used by 1 class
    
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
