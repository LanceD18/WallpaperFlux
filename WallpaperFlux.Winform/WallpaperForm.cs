using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Emgu.CV;
using LanceTools.Mpv;
using LanceTools.WindowsUtil;
using Mpv.NET.Player;
using WallpaperFlux.Core;
using WallpaperFlux.Core.Managers;
using WallpaperFlux.Core.Models;
using WallpaperFlux.Core.Tools;
using WallpaperFlux.Core.Util;
using WallpaperFlux.Core.ViewModels;
using WallpaperFlux.Winform.Util;
using WpfScreenHelper;
using Color = System.Drawing.Color;

namespace WallpaperFlux.Winform
{
    public partial class WallpaperForm : Form
    {
        private Rectangle pictureBoxBounds;

        private const double FRAME_LENGTH = (double)1 / 60;

        public MpvPlayer Player;

        public int Loops { get; private set; }

        public Stopwatch WallpaperUptime { get; private set; } = new Stopwatch();
        public bool IsPlayingVideo { get; private set; }

        private ImageModel _activeImage;
        public ImageModel ActiveImage
        {
            get => _activeImage;
            set
            {
                _activeImage = value;
                SetWallpaperStyle(WallpaperFluxViewModel.Instance.DisplaySettings[DisplayIndex].DisplayStyle);
                //xUpdateVolume(_activeImage); // without this the first video won't play audio
            }
        }

        public int DisplayIndex;

        protected override bool ShowWithoutActivation => true; // ! should stop the form from gaining focus when a wallpaper is set

        public WallpaperForm(WpfScreenHelper.Screen display, IntPtr workerw, int displayIndex, Action onVideoEnd)
        {
            this.SetStyle(ControlStyles.SupportsTransparentBackColor, true);

            InitializeComponent();

            DisplayIndex = displayIndex;

            this.TabStop = false; // minimizes likelihood of mpv stealing focus

            Load += (s, e) =>
            {
                this.Invoke((MethodInvoker)delegate
                {
                    BackColor = Color.Transparent;

                    // Sets bounds of the form
                    Width = (int)display.Bounds.Width + ThemeUtil.Theme.Settings.WindowWidthOffset;
                    Height = (int)display.Bounds.Height + ThemeUtil.Theme.Settings.WindowHeightOffset;
                    Left = (int)display.Bounds.X + WallpaperUtil.DisplayUtil.GetDisplayXAdjustment();
                    Top = (int)display.Bounds.Y + WallpaperUtil.DisplayUtil.GetMinDisplayY();
                    pictureBoxBounds = new Rectangle(0, 0, Width, Height);

                    // Sets bounds of the wallpaper
                    pictureBoxWallpaper.Bounds = pictureBoxBounds;
                    panelWallpaper.Bounds = pictureBoxBounds;

                    // Initializes Player
                    Player = new MpvPlayer(panelWallpaper.Handle, MpvUtil.MpvPath) // handle tells the Player to draw itself onto the panelWallpaper
                    {
                        AutoPlay = true,
                        Loop = true
                    };
                    //xPlayer.MediaEndedSeeking += (sender, args) => Loops++;
                    Player.MediaEndedSeeking += (sender, args) => onVideoEnd?.Invoke();

                    // This line makes the form a child of the WorkerW window, thus putting it behind the desktop icons and out of reach 
                    // for any user input. The form will just be rendered, no keyboard or mouse input will reach it.
                    // (Would have to use WH_KEYBOARD_LL and WH_MOUSE_LL hooks to capture mouse and keyboard input)
                    Win32.SetParent(Handle, workerw);
                });
            };

            Closing += (s, e) =>
            {
                Controls.Remove(pictureBoxWallpaper);

                Player?.Stop();

                Controls.Remove(panelWallpaper);
            };
        }
        
        /*
        // https://www.codeproject.com/Questions/400115/How-to-prevent-a-form-from-receiving-focus-on-mous
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == (int)0x84)
                m.Result = (IntPtr)(-1);
            else
                base.WndProc(ref m);
        }
        */

        //? Using this will cause old wallpapers to remain visible if you aren't filling the entire screen
        /*
        // makes the background transparent to prevent flickering (Only stops the 1 frame flicker when closing, not the one that occurs when loading)
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            var sb = new SolidBrush(Color.FromArgb(0, 0, 0, 0));
            e.Graphics.FillRectangle(sb, this.DisplayRectangle);
        }
        */

        // TODO Create a queue that stores pictureBoxes/axWindowMediaPlayers for each wallpaper. This will be used to allow transitions & prevent flickering from
        // TODO style readjustment when changing wallpapers by *locking* the previous wallpaper in place
        public async void SetWallpaper(ImageModel image)
        {
            if (IsDisposed) return; // for uses of this.<>

            Loops = 0;
            WallpaperUptime.Stop();

            if (!IsHandleCreated) return;

            bool setWallpaperResult = false;

            if (this.InvokeRequired)
            {
                this.BeginInvoke((MethodInvoker)delegate
                {
                    SetWallpaperInternal(image);
                    setWallpaperResult = true;
                });
            }
            else
            {
                await Task.Run(() => SetWallpaperInternal(image)).ConfigureAwait(false);
                setWallpaperResult = true;
            }

            async void SetWallpaperInternal(ImageModel image)
            {
                // --- Verify Wallpaper ---
                FileInfo wallpaperInfo;
                string wallpaperPath = image.Path;

                if (!string.IsNullOrEmpty(wallpaperPath))
                {
                    wallpaperInfo = new FileInfo(wallpaperPath);
                }
                else
                {
                    Debug.WriteLine("Null Wallpaper Path found when calling OnWallpaperChange");
                    return; //xfalse;
                }

                if (WallpaperUtil.IsSupportedVideoType_GivenExtension(wallpaperInfo.Extension))
                {
                    IsPlayingVideo = true;
                    WallpaperUptime.Restart();

                    pictureBoxWallpaper.Visible = false; // TODO Redundant, only needs to be called once
                    pictureBoxWallpaper.Enabled = false; // TODO Redundant, only needs to be called once

                    panelWallpaper.Enabled = true;
                    panelWallpaper.Visible = true;

                    await Task.Run(() =>
                    {
                        Player.Reload(image.Path);

                        UpdateVolume(image);

                        Player.Speed = image.Speed;
                    }).ConfigureAwait(false);
                }
                /*x
                else
                {
                    IsPlayingVideo = false;

                    Player.Stop();
                    panelWallpaper.Visible = false;
                    panelWallpaper.Enabled = false;

                    pictureBoxWallpaper.ImageLocation = image.Path;
                    pictureBoxWallpaper.Enabled = true;
                    pictureBoxWallpaper.Visible = true;
                }
                */

                ActiveImage = image; //? this change implies that the wallpaper was SUCCESSFULLY changed | Errors, video loop control, etc. can stop this
                //xreturn true;
            }

            //xreturn setWallpaperResult;
        }

        public void SetWallpaperStyle(WallpaperStyle wallpaperStyle)
        {
            if (!IsHandleCreated) return;
            if (IsDisposed) return; // for uses of this.<>

            Debug.WriteLine("Setting Style");
            if (InvokeRequired)
            {
                this?.Invoke((MethodInvoker)SetWallpaperStyleInternal);
            }
            else
            {
                SetWallpaperStyleInternal();
            }

            void SetWallpaperStyleInternal()
            {
                if (ActiveImage == null) return;

                if (pictureBoxWallpaper.Visible)
                {
                    if (pictureBoxWallpaper.ImageLocation == null) return;

                    pictureBoxWallpaper.SuspendLayout();
                    pictureBoxWallpaper.Bounds = pictureBoxBounds; // it's generally a good idea to reset this to prevent unwanted changes from previous states
                    switch (wallpaperStyle)
                    {
                        case WallpaperStyle.Fill:
                            using (Image image = Image.FromFile(pictureBoxWallpaper.ImageLocation))
                            {
                                int heightDiff = GetFillHeightDiff(image.Width, image.Height);
                                pictureBoxWallpaper.Width = Width; // scales the image to its width
                                pictureBoxWallpaper.Height = Height + heightDiff; // any additional height will be pushed offscreen
                                pictureBoxWallpaper.Top = -heightDiff / 2; // centers the height pushed offscreen
                                pictureBoxWallpaper.SizeMode = PictureBoxSizeMode.StretchImage;
                            }

                            break;

                        case WallpaperStyle.Stretch:
                            pictureBoxWallpaper.SizeMode = PictureBoxSizeMode.StretchImage;
                            break;

                        case WallpaperStyle.Fit:
                            pictureBoxWallpaper.Height -= ThemeUtil.Theme.Settings.WindowHeightOffset;
                            pictureBoxWallpaper.SizeMode = PictureBoxSizeMode.Zoom;
                            break;
                    }

                    pictureBoxWallpaper.ResumeLayout();
                }

                if (panelWallpaper.Visible)
                {
                    /* TODO Not properly working
                    panelWallpaper.SuspendLayout();
                    using (VideoCapture video = new VideoCapture(ActiveImage.PathName))
                    {
                        using (Mat m = new Mat()) video.Read(m);
                        //video.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.PosAviRatio, 0);

                        switch (wallpaperStyle)
                        {
                            case WallpaperStyle.Fill:
                                panelWallpaper.Bounds = pictureBoxBounds;
                                int heightDiff = GetFillHeightDiff(video.Width, video.Height);
                                panelWallpaper.Width = Width; // scales the image to its width
                                panelWallpaper.Height = Height + heightDiff; // any additional height will be pushed offscreen
                                panelWallpaper.Top = -heightDiff / 2; // centers the height pushed offscreen
                                break;

                            case WallpaperStyle.Stretch:
                                panelWallpaper.Bounds = pictureBoxBounds;
                                break;

                            case WallpaperStyle.Fit:
                                panelWallpaper.Bounds = GetZoomBounds(video.Width, video.Height);
                                break;
                        }

                        panelWallpaper.ResumeLayout();
                        
                    }
                    */
                }
            }
        }

        private int GetFillHeightDiff(int imageWidth, int imageHeight)
        {
            float imageRatio = (float)imageWidth / imageHeight;
            float monitorRatio = (float)Width / Height;

            float combinedRatio = monitorRatio / imageRatio;
            float rescaledImageHeight = Height * combinedRatio;

            return (int)Math.Abs(Height - rescaledImageHeight);
        }

        private Rectangle GetZoomBounds(int videoWidth, int videoHeight) // images can do this automatically with the pictureBox
        {
            // it's best to check with ratios rather than the exact ImageHeight & ImageWidth in order to avoid scaling out of the monitor
            float widthRatio = (float)videoWidth / Width;
            float heightRatio = (float)videoHeight / Height;

            int TaskBarHeight = ThemeUtil.Theme.Settings.WindowHeightOffset;

            // if both are equal heightRatio should be preferred
            if (heightRatio >= widthRatio) // scale image to match the monitor height and let the width have gaps 
            {
                float adjustedHeight = Height - TaskBarHeight;
                float adjustedWidth = videoWidth * ((float)Height / videoHeight) * (adjustedHeight / Height);
                //float adjustedWidth = MonitorWidth * ((float) ImageWidth / ImageHeight);

                float widthDifference = Width - adjustedWidth;
                float leftGap = widthDifference / 2;
                float xPos = 0 + widthDifference - leftGap;

                return new Rectangle((int)xPos, 0, (int)adjustedWidth, (int)adjustedHeight);
            }
            else // scale image to match the monitor width and let the height have gaps
            {
                float adjustedHeight = videoHeight * ((float)Width / videoWidth);

                float heightDifference = Height - adjustedHeight;
                float bottomGap = heightDifference / 2;
                float yPos = heightDifference - bottomGap;

                return new Rectangle(0, (int)yPos, Width, (int)adjustedHeight);
            }
        }

        public void Mute()
        {
            if (Player != null)
            {
                Player.Volume = 0;
            }
        }

        public void Unmute()
        {
            UpdateVolume(ActiveImage);
        }

        public void UpdateVolume(ImageModel image)
        {
            if (Player == null) return;

            if (image != null && !AudioManager.IsWallpapersMuted)
            {
                Player.Volume = (int)image.Volume;
            }
            else
            {
                Player.Volume = 0;
            }

            Debug.WriteLine("Updating Mpv Volume: "  + Player.Volume);
        }

        public void StopMpv()
        {
            if (Player != null) Player.Stop();
            panelWallpaper.Visible = false;
            panelWallpaper.Enabled = false;
        }

        private void timerAudioFixer_Tick(object sender, EventArgs e)
        {
            /*
            if (IsPlayingVideo && !AudioManager.IsWallpapersMuted)
            {
                // TODO For whatever reason videos randomly use the audio of a previous video, find a more permanent solution to this fix
                // TODO The commented fix did not solve this
                // TODO The occurence is 'rare' enough for to not know if the below fix has actually done anything yet
                Player.Volume = WallpaperData.GetImageData(activeVideoImagePath).VideoSettings.Volume;
                /*
                WallpaperData.VideoSettings videoSettings = WallpaperData.GetImageData(activeVideoImagePath).VideoSettings;
                if (Player.Volume != videoSettings.Volume)
                {
                    Debug.WriteLine("Error: Had to fix the volume of a wallpaper with the timerAudioFix control");
                    Player.Volume = videoSettings.Volume;
                }
                /
            }
            */
        }
    }
}
