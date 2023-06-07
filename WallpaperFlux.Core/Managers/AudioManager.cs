using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using CSCore.CoreAudioAPI;
using LanceTools.IO;
using LanceTools.WindowsUtil;
using MvvmCross;
using WallpaperFlux.Core.IoC;
using WallpaperFlux.Core.Util;

namespace WallpaperFlux.Core.Managers
{
    public static class AudioManager
    {
        public static bool IsWallpapersMuted { get; private set; }

        public delegate void MuteEvent();

        public static event MuteEvent OnMute;
        public static event MuteEvent OnUnmute;

        //? remember that the max volume is 1
        private static readonly double MIN_VOLUME = 0; //? not using a float to minimize errors on low volume

        private static IExternalTimer _audioTimer;

        private static Thread _audioThread = new Thread(() => {});
        // used to check if the thread is alive so that we don't have multiple checks running at the same time

        public static void StartAudioManagerTimer()
        {
            _audioTimer = Mvx.IoCProvider.Resolve<IExternalTimer>();
            _audioTimer.Interval = TimeSpan.FromMilliseconds(100);
            _audioTimer.Tick += AudioManagerOnTick;
            _audioTimer.Start();
        }

        private static void AudioManagerOnTick(object sender, EventArgs e)
        {
            if (!_audioThread.IsAlive) // we don't want a lagged thread to overwrite and/or conflict with an upcoming thread, nor do we want too many running at the same time
            {
                CheckForMuteConditions();
            }
        }

        public static void CheckForMuteConditions()
        {
            //! await Task.Run(() => Using audioThread instead
            _audioThread = new Thread(() =>
            {
                //x Debug.WriteLine("Checking for mute conditions");
                int potentialAudioCount = 0;
                foreach (string wallpaper in ThemeUtil.Theme.WallpaperRandomizer.ActiveWallpapers)
                {
                    if (WallpaperUtil.IsSupportedVideoType(wallpaper))
                    {
                        potentialAudioCount++;
                    }
                }

                //x Debug.WriteLine("Potential Audio Count: " + potentialAudioCount);

                if (potentialAudioCount == 0) return; // there's no need to check for muting if no wallpapers that can be muted exist

                bool muted = false;

                void Mute()
                {
                    MuteWallpapers();
                    muted = true;
                }

                if (ThemeUtil.Theme.Settings.ThemeSettings.VideoSettings.MuteIfApplicationFocused && !muted)
                {
                    Process activeWindow = Win32.GetActiveWindowProcess();
                    string windowName = activeWindow.ProcessName;
                    if (windowName != Process.GetCurrentProcess().ProcessName && windowName != "explorer") //? explorer includes the desktop
                    {
                        WindowPlacementStyle windowStyle = WindowInfo.GetWindowStyle(activeWindow);
                        if (windowStyle == WindowPlacementStyle.Normal || windowStyle == WindowPlacementStyle.Maximized)
                        {
                            Mute();
                        }
                    }
                }

                if (ThemeUtil.Theme.Settings.ThemeSettings.VideoSettings.MuteIfApplicationMaximized && !muted) // every window needs to be checked for maximization
                {
                    //xStopwatch test = new Stopwatch();
                    //xtest.Start();
                    foreach (Process p in Process.GetProcesses()) //? has the potential to take up a decent CPU load, not noticeable on the thread but still impactful
                    {
                        WindowPlacementStyle windowStyle = WindowInfo.GetWindowStyle(p);

                        if (windowStyle == WindowPlacementStyle.Maximized)
                        {
                            Mute();
                            break;
                        }
                    }
                    //xtest.Stop();
                    //xDebug.WriteLine("Ms taken to check for maximized app: " + test.ElapsedMilliseconds);
                }

                if (ThemeUtil.Theme.Settings.ThemeSettings.VideoSettings.MuteIfAudioPlaying && !muted)
                {
                    if (CheckForExternalAudio()) //? CheckForExternalAudio cannot be done on the UI thread | async doesn't fix this
                    {
                        Mute();
                    }
                }

                if (IsWallpapersMuted && !muted) UnmuteWallpapers();
            }); //x.ConfigureAwait(false);

            _audioThread.Start();

            //x while (thread.IsAlive) { ( do nothing | Thread.Join() will just freeze the application ) } << this is only needed if you're returning something
        }

        private static bool CheckForExternalAudio()
        {
            using (AudioSessionManager2 sessionManager = GetDefaultAudioSessionManager2(DataFlow.Render))
            {
                using (AudioSessionEnumerator sessionEnumerator = sessionManager.GetSessionEnumerator())
                {
                    /*x
                    if (wallpaperManagerForm.IsViewingInspector)
                    {
                        // if a video is playing in the inspector, mute the background
                        // Trying to figure out the source would be difficult as all audio links to the app
                        if (WallpaperUtil.IsSupportedVideoType(wallpaperManagerForm.InspectedImage))
                        {
                            return true;
                        }
                    }
                    */

                    List<string> potentialNames = new List<string>();
                    foreach (string wallpaper in ThemeUtil.Theme.WallpaperRandomizer.ActiveWallpapers)
                    {
                        if (FileUtil.Exists(wallpaper))
                        {
                            if (WallpaperUtil.IsSupportedVideoType(wallpaper)) // only videos should be checked
                            {
                                //xDebug.WriteLine("Enabled: " + new FileInfo(wallpaper).Name);
                                potentialNames.Add(new FileInfo(wallpaper).Name);
                            }
                        }
                    }
                    if (potentialNames.Count == 0) return false; //? there's no audio to play

                    //? The name of the video playing on the WallpaperForm will definitely be given, so check if
                    //? anything BUT those are playing and if so mute the wallpaper
                    foreach (AudioSessionControl session in sessionEnumerator)
                    {
                        // format of session.DisplayName for videos: videoName.extension - extension | We only want videoName.extension, cut off the first space
                        string sessionName = session.DisplayName;
                        string sessionVideoName = !sessionName.Contains(' ') ? sessionName : sessionName.Substring(0, sessionName.IndexOf(' '));

                        //xif (session.IconPath.Contains(Path.GetDirectoryName(Application.ExecutablePath)))
                        if (session.IconPath.Contains(AppDomain.CurrentDomain.BaseDirectory)) // if the detected audio if the application itself, skip
                        {
                            Debug.WriteLine("Encountered Audio of Application");
                            continue;
                        }

                        if (ThemeUtil.Theme.Settings.ThemeSettings.VideoSettings.MuteIfAudioPlaying) // this is checked again since in some cases this code was only called due to the inspector
                        {
                            if (!potentialNames.Contains(sessionVideoName)) // checking an audio source that doesn't match up with to the active wallpapers
                            {
                                using (AudioMeterInformation audioMeterInformation = session.QueryInterface<AudioMeterInformation>())
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
            IsWallpapersMuted = true;
            OnMute?.Invoke();
        }

        private static void UnmuteWallpapers()
        {
            Debug.WriteLine("Unmute");
            IsWallpapersMuted = false;
            OnUnmute?.Invoke();
        }
    }
}