using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using LiveChartsCore;
using LiveChartsCore.Drawing;
using LiveChartsCore.Kernel.Drawing;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Drawing;
using LiveChartsCore.SkiaSharpView.Painting;
using MvvmCross;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using SkiaSharp;
using WallpaperFlux.Core.IoC;
using WallpaperFlux.Core.Models;
using WallpaperFlux.Core.Models.Theme;
using WallpaperFlux.Core.Util;

namespace WallpaperFlux.Core.ViewModels
{
    public class SettingsViewModel : MvxViewModel
    {
        public static SettingsViewModel Instance; // allows the data to remain persistent without having to reload everything once the view is closed

        public SettingsModel Settings { get; set; } = ThemeUtil.Theme.Settings; //? most of the data handling is managed through here

        public ColumnSeries<int> AllColumnSeries = new ColumnSeries<int>();
        public ColumnSeries<int> StaticColumnSeries = new ColumnSeries<int>();
        public ColumnSeries<int> GifColumnSeries = new ColumnSeries<int>();
        public ColumnSeries<int> VideoColumnSeries = new ColumnSeries<int>();

        public ISeries[] RankSeries { get; set; }

        private bool _rankGraphToggle;
        public bool RankGraphToggle
        {
            get => _rankGraphToggle;
            set
            {
                SetProperty(ref _rankGraphToggle, value);

                if (value) // update graph
                {
                    List<int> allValues = new List<int>();
                    List<int> staticValues = new List<int>();
                    List<int> gifValues = new List<int>();
                    List<int> videoValues = new List<int>();

                    // we need a buffer value for rank 0 since we aren't actually displaying the un-ranked images
                    // without this buffer, the graph will always place the bars 1 value off
                    allValues.Add(0);
                    staticValues.Add(0);
                    gifValues.Add(0);
                    videoValues.Add(0);

                    for (int i = 1; i <= ThemeUtil.ThemeSettings.MaxRank; i++) //? not including un-ranked, those will take over the majority of the graph
                    {
                        allValues.Add(ThemeUtil.RankController.GetCountOfRank(i));
                        staticValues.Add(ThemeUtil.RankController.GetCountOfRankOfType(ImageType.Static, i));
                        gifValues.Add(ThemeUtil.RankController.GetCountOfRankOfType(ImageType.GIF, i));
                        videoValues.Add(ThemeUtil.RankController.GetCountOfRankOfType(ImageType.Video, i));
                    }

                    AllColumnSeries.Values = allValues;
                    StaticColumnSeries.Values = staticValues;
                    GifColumnSeries.Values = gifValues;
                    VideoColumnSeries.Values = videoValues;

                    //xRankColumnSeries.Fill = new SolidColorPaint(SKColors.Blue);
                    StaticColumnSeries.Fill = new SolidColorPaint(SKColors.SlateBlue);
                    GifColumnSeries.Fill = new SolidColorPaint(SKColors.LimeGreen);
                    VideoColumnSeries.Fill = new SolidColorPaint(SKColors.OrangeRed);
                }
            }
        }

        public string RankedText => "Ranked: " + ThemeUtil.RankController.GetCountOfAllRankedImages();

        public string UnrankedText => "Unranked: " + ThemeUtil.RankController.GetCountOfRank(0);

        public string DisabledText
        {
            get
            {
                int diff = ThemeUtil.Theme.Images.GetAllImages().Length - (ThemeUtil.RankController.GetCountOfAllRankedImages() + ThemeUtil.RankController.GetCountOfRank(0));
                //xDebug.WriteLine("Diff: All: " + ThemeUtil.Theme.Images.GetAllImages().Length + " | 99: " + ThemeUtil.RankController.GetCountOfAllRankedImages() + " | 0: " + ThemeUtil.RankController.GetCountOfRank(0));

                if (diff > 0)
                {
                    //? enabled images in sets count as disabled but for the sake of these number they'll be counted as enabled
                    return "Disabled: " + (diff - ThemeUtil.Theme.Images.GetEnabledImagesInSetsCount());
                }

                return "";
            }
        }

        public string ImagesInSetText
        {
            get
            {
                int count = ThemeUtil.Theme.Images.GetEnabledImagesInSetsCount();

                if (count > 0)
                {
                    return "In Sets: " + count;
                }

                return "";
            }
        }

        private bool _allColumnToggle = true;
        public bool AllColumnToggle
        {
            get => _allColumnToggle;
            set
            {
                SetProperty(ref _allColumnToggle, value);
                AllColumnSeries.IsVisible = value;
            }
        }

        private bool _staticColumnToggle;
        public bool StaticColumnToggle
        {
            get => _staticColumnToggle;
            set
            {
                SetProperty(ref _staticColumnToggle, value);
                StaticColumnSeries.IsVisible = value;
            }
        }

        private bool _gifColumnToggle;
        public bool GifColumnToggle
        {
            get => _gifColumnToggle;
            set
            {
                SetProperty(ref _gifColumnToggle, value);
                GifColumnSeries.IsVisible = value;
            }
        }

        private bool _videoColumnToggle;
        public bool VideoColumnToggle
        {
            get => _videoColumnToggle;
            set
            {
                SetProperty(ref _videoColumnToggle, value);
                VideoColumnSeries.IsVisible = value;
            }
        }

        #region Commands

        public IMvxCommand ToggleRankGraphCommand { get; set; }

        public IMvxCommand CloseRankGraphCommand { get; set; }

        #endregion

        //? LiveChart2 Docs: https://lvcharts.com/docs/WPF/2.0.0-beta.330/CartesianChart.Cartesian%20chart%20control (Don't accidentally use LiveChart1 docs)
        public SettingsViewModel()
        {
            AllColumnSeries.Name = "All";
            StaticColumnSeries.Name = "Static";
            GifColumnSeries.Name = "GIF";
            VideoColumnSeries.Name = "Video";

            AllColumnSeries.IsVisible = AllColumnToggle;
            StaticColumnSeries.IsVisible = StaticColumnToggle;
            GifColumnSeries.IsVisible = GifColumnToggle;
            VideoColumnSeries.IsVisible = VideoColumnToggle;

            RankSeries = new ISeries[]
            {
                AllColumnSeries,
                StaticColumnSeries,
                GifColumnSeries,
                VideoColumnSeries
            };

            ToggleRankGraphCommand = new MvxCommand(ToggleRankGraph);
            CloseRankGraphCommand = new MvxCommand(CloseRankGraph);
        }

        public void ToggleRankGraph() => RankGraphToggle = !RankGraphToggle;

        public void CloseRankGraph() => RankGraphToggle = false;
    }
}
