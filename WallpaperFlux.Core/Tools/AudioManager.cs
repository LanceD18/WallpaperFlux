/*
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CSCore.CoreAudioAPI;
using LanceTools.WindowsUtil;

namespace WallpaperFlux.Core.Tools
{
    public static class AudioManager
    {
        public static bool IsWallpapersMuted { get; private set; }

        //? remember that the max volume is 1
        private static readonly float MIN_VOLUME = 0f; // might make this a really small decimal due to the potential of float errors

        //private static Guid guid = Guid.NewGuid();

        public static async void CheckForMuteConditions()
        {
            await Task.Run(() =>
            {
                int potentialAudioCount = 0;
                foreach (string wallpaper in WallpaperPathSetter.ActiveWallpapers)
                {
                    if (WallpaperManagerTools.IsSupportedVideoType(wallpaper))
                    {
                        potentialAudioCount++;
                    }
                }
                //?if (potentialAudioCount == 0) return; // there's no need to check for muting if no wallpapers that can be muted exist

                bool muted = false;

                void ProcessMute()
                {
                    MuteWallpapers();
                    muted = true;
                }

                if (OptionsData.ThemeOptions.VideoOptions.MuteIfApplicationFocused && !muted)
                {
                    Process activeWindow = Win32.GetActiveWindowProcess();
                    string windowName = activeWindow.ProcessName;
                    if (windowName != Process.GetCurrentProcess().ProcessName && windowName != "explorer") //? explorer includes the desktop
                    {
                        WindowPlacementStyle windowStyle = WindowInfo.GetWindowStyle(activeWindow);
                        if (windowStyle == WindowPlacementStyle.Normal || windowStyle == WindowPlacementStyle.Maximized)
                        {
                            ProcessMute();
                        }
                    }
                }

                if (OptionsData.ThemeOptions.VideoOptions.MuteIfApplicationMaximized && !muted) // every window needs to be checked for maximization
                {
                    //xStopwatch test = new Stopwatch();
                    //xtest.Start();
                    foreach (Process p in Process.GetProcesses()) //? has the potential to take up a decent CPU load, not noticeable on the thread but still impactful
                    {
                        WindowPlacementStyle windowStyle = WindowInfo.GetWindowStyle(p);

                        if (windowStyle == WindowPlacementStyle.Maximized)
                        {
                            ProcessMute();
                            break;
                        }
                    }
                    //xtest.Stop();
                    //xDebug.WriteLine("Ms taken to check for maximized app: " + test.ElapsedMilliseconds);
                }

                if ((OptionsData.ThemeOptions.VideoOptions.MuteIfAudioPlaying || WallpaperData.WallpaperManagerForm.IsViewingInspector) && !muted)
                {
                    if (CheckForExternalAudio()) //? CheckForExternalAudio cannot be done on the UI thread | async doesn't fix this
                    {
                        ProcessMute();
                    }
                }

                if (IsWallpapersMuted && !muted) UnmuteWallpapers();
            }).ConfigureAwait(false);

            //x while (thread.IsAlive) { ( do nothing | Thread.Join() will just freeze the application ) } << this is only needed if you're returning something
        }

        private static bool CheckForExternalAudio()
        {
            WallpaperManagerForm wallpaperManagerForm = WallpaperData.WallpaperManagerForm;

            using (var sessionManager = GetDefaultAudioSessionManager2(DataFlow.Render))
            {
                using (var sessionEnumerator = sessionManager.GetSessionEnumerator())
                {
                    if (wallpaperManagerForm.IsViewingInspector)
                    {
                        // if a video is playing in the inspector, mute the background
                        // Trying to figure out the source would be difficult as all audio links to the app
                        if (WallpaperManagerTools.IsSupportedVideoType(wallpaperManagerForm.InspectedImage))
                        {
                            return true;
                        }
                    }

                    List<string> potentialNames = new List<string>();
                    foreach (string wallpaper in WallpaperPathSetter.ActiveWallpapers)
                    {
                        if (File.Exists(wallpaper))
                        {
                            if (WallpaperManagerTools.IsSupportedVideoType(wallpaper)) // only videos should be checked
                            {
                                //xDebug.WriteLine("Active: " + new FileInfo(wallpaper).Name);
                                potentialNames.Add(new FileInfo(wallpaper).Name);
                            }
                        }
                    }
                    if (potentialNames.Count == 0) return false; //? there's no audio to play

                    //? The name of the video playing on the WallpaperForm will definitely be given, so check if
                    //? anything BUT those are playing and if so mute the wallpaper
                    foreach (var session in sessionEnumerator)
                    {
                        // format of session.DisplayName for videos: videoName.extension - extension | We only want videoName.extension, cut off the first space
                        string sessionName = session.DisplayName;
                        string sessionVideoName = !sessionName.Contains(' ') ? sessionName : sessionName.Substring(0, sessionName.IndexOf(' '));

                        if (session.IconPath.Contains(Path.GetDirectoryName(MediaTypeNames.Application.ExecutablePath)))
                        {
                            Debug.WriteLine("Encountered Audio of Application");
                            continue;
                        }

                        if (OptionsData.ThemeOptions.VideoOptions.MuteIfAudioPlaying) // this is checked again since in some cases this code was only called due to the inspector
                        {
                            if (!potentialNames.Contains(sessionVideoName)) // checking an audio source that doesn't match up with to the active wallpapers
                            {
                                using (var audioMeterInformation = session.QueryInterface<AudioMeterInformation>())
                                {
                                    if (audioMeterInformation.GetPeakValue() > MIN_VOLUME) // if the volume of this application is greater than MIN_VOLUME, mute all wallpapers
                                    {
                                        Debug.WriteLine("External Audio Playing: " + session.DisplayName);
                                        //xDebug.WriteLine(audioMeterInformation.GetPeakValue());
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }

        private static AudioSessionManager2 GetDefaultAudioSessionManager2(DataFlow dataFlow)
        {
            using (var enumerator = new MMDeviceEnumerator())
            {
                using (var device = enumerator.GetDefaultAudioEndpoint(dataFlow, Role.Multimedia))
                {
                    //xDebug.WriteLine("DefaultDevice: " + device.FriendlyName);
                    var sessionManager = AudioSessionManager2.FromMMDevice(device);
                    return sessionManager;
                }
            }
        }

        private static void MuteWallpapers()
        {
            Debug.WriteLine("Mute");
            foreach (var wallpaper in WallpaperData.WallpaperManagerForm.GetWallpapers()) wallpaper.Mute();
            IsWallpapersMuted = true;
        }

        private static void UnmuteWallpapers()
        {
            Debug.WriteLine("Unmute");
            foreach (var wallpaper in WallpaperData.WallpaperManagerForm.GetWallpapers()) wallpaper.Unmute();
            IsWallpapersMuted = false;
        }
    }

}
*/