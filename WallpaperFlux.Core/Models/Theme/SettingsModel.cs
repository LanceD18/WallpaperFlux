using System.Diagnostics;
using MvvmCross.ViewModels;
using WallpaperFlux.Core.Util;

namespace WallpaperFlux.Core.Models.Theme
{
    public class SettingsModel : MvxNotifyPropertyChanged
    {
        public int MaxRank { get; set; }

        public bool WeightedRanks { get; set; }

        public bool WeightedFrequency { get; set; }

        public SettingsModel(int maxRank)
        {
            MaxRank = maxRank;
        }

        #region Commands
        public void UpdateMaxRank()
        {
            DataUtil.Theme.RankController.SetMaxRank(MaxRank);
        }
        #endregion
    }
}
