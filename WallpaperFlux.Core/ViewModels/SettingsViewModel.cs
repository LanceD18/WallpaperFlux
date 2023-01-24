using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using LiveChartsCore;
using LiveChartsCore.Kernel.Drawing;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Drawing;
using LiveChartsCore.SkiaSharpView.Painting;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using SkiaSharp;
using WallpaperFlux.Core.Models.Theme;
using WallpaperFlux.Core.Util;

namespace WallpaperFlux.Core.ViewModels
{
    public class SettingsViewModel : MvxViewModel
    {
        public static SettingsViewModel Instance; // allows the data to remain persistent without having to reload everything once the view is closed

        public SettingsModel Settings { get; set; } = ThemeUtil.Theme.Settings; //? most of the data handling is managed through here

        public ColumnSeries<int> RankColumnSeries = new ColumnSeries<int>();
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
                    List<int> rankValues = new List<int>();
                    List<int> staticValues = new List<int>();
                    List<int> gifValues = new List<int>();
                    List<int> videoValues = new List<int>();

                    for (int i = 1; i <= ThemeUtil.ThemeSettings.MaxRank; i++) //? not including un-ranked, those will take over the majority of the graph
                    {
                        rankValues.Add(ThemeUtil.RankController.GetRankCount(i));
                        staticValues.Add(ThemeUtil.RankController.GetImagesOfTypeRankCount(ImageType.Static, i));
                        gifValues.Add(ThemeUtil.RankController.GetImagesOfTypeRankCount(ImageType.GIF, i));
                        videoValues.Add(ThemeUtil.RankController.GetImagesOfTypeRankCount(ImageType.Video, i));
                    }

                    RankColumnSeries.Values = rankValues;
                    StaticColumnSeries.Values = staticValues;
                    GifColumnSeries.Values = gifValues;
                    VideoColumnSeries.Values = videoValues;
                }
            }
        }

        public string RankedText => "Ranked: " + ThemeUtil.RankController.GetRankCountTotal();

        public string UnrankedText => "Unranked: " + ThemeUtil.RankController.GetRankCount(0);

        private bool _rankColumnToggle = true;
        public bool RankColumnToggle
        {
            get => _rankColumnToggle;
            set
            {
                SetProperty(ref _rankColumnToggle, value);
                RankColumnSeries.IsVisible = value;
                RankColumnSeries.IsVisible = value;
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
            RankColumnSeries.Name = "All";
            StaticColumnSeries.Name = "Static";
            GifColumnSeries.Name = "GIF";
            VideoColumnSeries.Name = "Video";

            RankColumnSeries.IsVisible = RankColumnToggle;
            StaticColumnSeries.IsVisible = StaticColumnToggle;
            GifColumnSeries.IsVisible = GifColumnToggle;
            VideoColumnSeries.IsVisible = VideoColumnToggle;

            RankSeries = new ISeries[]
            {
                RankColumnSeries,
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
