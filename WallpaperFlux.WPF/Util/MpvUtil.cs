using System;
using System.Collections.Generic;
using System.Text;
using HanumanInstitute.MediaPlayer.Wpf.Mpv;
using Mpv.NET.Player;

namespace WallpaperFlux.WPF.Util
{
    public static class MpvUtil
    {
        public static string MpvPath;

        public static Action<string>[] Open;
    }
}
