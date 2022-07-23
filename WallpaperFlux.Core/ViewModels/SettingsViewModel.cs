using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using WallpaperFlux.Core.Models.Theme;
using WallpaperFlux.Core.Util;

namespace WallpaperFlux.Core.ViewModels
{
    public class SettingsViewModel : MvxViewModel
    {
        public static SettingsViewModel Instance; // allows the data to remain persistent without having to reload everything once the view is closed

        public SettingsModel Settings { get; set; } = DataUtil.Theme.Settings; //? most of the data handling is managed through here

        #region Commands

        public IMvxCommand OpenRankDistributionGraphCommand;

        #endregion

        public SettingsViewModel()
        {
            OpenRankDistributionGraphCommand = new MvxCommand(OpenRankDistributionGraph);
        }

        public void OpenRankDistributionGraph()
        {
            Debug.WriteLine("Not yet implemented");
        }
    }
}
