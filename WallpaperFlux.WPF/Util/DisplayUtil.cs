using System;
using System.Collections.Generic;
using System.Linq;
using LanceTools;
using WallpaperFlux.Core.Util;
using WpfScreenHelper;

namespace WallpaperFlux.WPF.Util
{
    public static class DisplayUtil
    {
        //! IMPORTANT: Using Winform's screen property may cause issues since WPF's measurement units are not in pixels, which is why we are using WpfScreenHelper
        public static readonly IEnumerable<Screen> Displays = Screen.AllScreens;

        //? Screen.AllScreens seems to do this by default due to the way it functions, I wouldn't rely on that though as there's no documentation for it
        private static IEnumerable<int> LargestDisplayOrderIndex =
        (
            from s in Screen.AllScreens
            orderby s.Bounds.Width + s.Bounds.Height descending
            select Screen.AllScreens.IndexOf(s)
        );

        // Used to help set up monitor bounds & adjustments
        public static int TotalDisplayWidth { get; } // total width of all monitors combined
        public static int MaxDisplayHeight { get; } // height of the tallest monitor, maximum possible height
        public static int DisplayXAdjustment { get; }
        public static int MinDisplayY { get; } = int.MaxValue;

        static DisplayUtil() // automatic static initialization
        {
            //xWallpaperUtil.SetDisplayCount(Displays.Count());

            // Set up monitor bounds & adjustments
            foreach (Screen display in Displays)
            {
                if (display.Bounds.X < 0) // used to prevent wallpapers from being drawn off the screen
                {
                    DisplayXAdjustment += (int)Math.Abs(display.Bounds.X);
                }

                if (display.Bounds.Y < MinDisplayY)
                {
                    MinDisplayY = (int)display.Bounds.Y;
                }

                TotalDisplayWidth += (int)display.Bounds.Width;
                MaxDisplayHeight = (int)Math.Max(MaxDisplayHeight, display.Bounds.Height);
            }
        }

        public static void ResetLargestDisplayIndexOrder()
        {
            LargestDisplayOrderIndex =
            (
                from s in Displays
                orderby s.Bounds.Width + s.Bounds.Height descending
                select Displays.IndexOf(s)
            );
        }

        public static IEnumerable<int> GetLargestDisplayIndexOrder() => LargestDisplayOrderIndex;

        /*x
        private static int displayNum;
        private static List<int> displayNumbering = new List<int>();

        public static void ReorderDisplays()
        {
            Button[] displayButtons = new Button[Displays.Length];
            for (int i = 0; i < displayButtons.Length; i++)
            {
                displayButtons[i] = new Button();
                displayButtons[i].Text = (i + 1).ToString();
                displayButtons[i].Click += OnMonitorButtonsClick;
            }

            // **Reorders MonitorData to a user selected order**
            Screen[] TempDisplayData = new Screen[Displays.Length];
            displayNumbering.Clear();
            for (int i = 0; i < Displays.Length; i++)
            {
                Screen display = Displays[i];
                MessageBoxDynamic.Show("Identify Display", "Identify the following display:\n" + display.DeviceName + "\n" + display.Bounds, displayButtons, true);
                TempDisplayData[displayNum] = Displays[i];
                displayNumbering.Add(displayNum);
            }
            TempDisplayData.CopyTo(Displays, 0);

            ResetLargestDisplayIndexOrder();
        }

        private static void OnMonitorButtonsClick(object sender, EventArgs e)
        {
            Button button = sender as Button;
            displayNum = int.Parse(button.Text) - 1;
            button.Visible = false;
        }
        */
    }

}
