using System;
using System.Collections.Generic;
using System.Text;
using MediaToolkit;
using MediaToolkit.Model;
using WallpaperFlux.Core.IoC;

namespace WallpaperFlux.WPF.IoC
{
    public class ExternalVideoUtil : IExternalVideoUtil
    {
        public bool HasAudio(string videoPath)
        {
            using (Engine engine = new Engine())
            {
                MediaFile video = new MediaFile(videoPath);
                engine.GetMetadata(video);

                return video.Metadata.AudioData != null;
            }
        }
    }
}
