using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using MvvmCross.ViewModels;
using WallpaperFlux.Core.External;
using WallpaperFlux.Core.Util;

namespace WallpaperFlux.Core.Models
{
    public class DisplayModel : MvxNotifyPropertyChanged
    {
        //-----DisplayIndex-----
        private int _displayIndex;

        public int DisplayIndex // offsets used for visual purposes
        {
            get => _displayIndex + 1;
            set => _displayIndex = value;
        }

        //-----DisplayInterval-----
        // TODO This should reset the timer on changing the interval
        private int _displayInterval;

        public int DisplayInterval
        {
            get => _displayInterval;
            set
            {
                SetProperty(ref _displayInterval, value);
                ResetMaxTime();
            }
        }

        public bool CanSetInterval => DisplayIntervalType != IntervalType.None;

        //-----DisplayIntervalType-----
        // TODO This should reset the timer on changing the interval type
        private IntervalType _displayIntervalType;

        public IntervalType DisplayIntervalType
        {
            get => _displayIntervalType;
            set
            {
                SetProperty(ref _displayIntervalType, value);
                RaisePropertyChanged(() => CanSetInterval);
                ResetMaxTime();
            }
        }

        //-----DisplayTimerMax-----
        public int DisplayTimerMax { get; private set; }

        //-----DisplayTimerCurrent-----
        private int _displayTimerCurrent;

        public int DisplayTimerCurrent
        {
            get => _displayTimerCurrent;
            set
            {
                SetProperty(ref _displayTimerCurrent, Math.Max(value, 0));

                if (DisplayTimerCurrent <= 0 && DisplayTimerMax != 0)
                {
                    ResetTimer();
                }
            }
        }

        //-----DisplayStyle-----
        private WallpaperStyle _displayStyle;

        public WallpaperStyle DisplayStyle
        {
            get => _displayStyle;
            set
            {
                SetProperty(ref _displayStyle, value);
                WallpaperUtil.OnWallpaperStyleChange?.Invoke(_displayIndex, _displayStyle);
            }
        }

        private readonly ITimer timer;

        private Action<int, bool> OnTimerReset;

        private DisplayModel parentSyncedModel;
        private List<DisplayModel> childSyncedModels = new List<DisplayModel>();

        //?-----Constructor-----
        public DisplayModel(ITimer timer, Action<int, bool> OnTimerReset)
        {
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += TimerOnTick;
            timer.Start();

            this.OnTimerReset = OnTimerReset;
        }

        //-----Methods-----
        private void TimerOnTick(object sender, EventArgs e)
        {
            if (parentSyncedModel == null)
            {
                if (DisplayIntervalType != IntervalType.None)
                {
                    DisplayTimerCurrent -= 1;
                }

                foreach (DisplayModel model in childSyncedModels)
                {
                    model.DisplayTimerCurrent = DisplayTimerCurrent;
                }
            }
        }

        private void ResetMaxTime()
        {
            RemoveSync();
            DisplayTimerMax = DisplayTimerCurrent = DisplayInterval * (int) DisplayIntervalType;
        }

        private void ResetTimer()
        {
            if (parentSyncedModel == null)
            {
                DisplayTimerCurrent = DisplayTimerMax;
                OnTimerReset?.Invoke(_displayIndex, true);
                //_displayIndex uses the value appropriate for arrays while DisplayIndex uses the value appropriate for WPF/visual information

                foreach (DisplayModel model in childSyncedModels)
                {
                    model.OnTimerReset?.Invoke(model._displayIndex, true);
                }
            }
            else
            {
                Debug.WriteLine("Parent: " + parentSyncedModel.DisplayIndex);
            }
        }

        #region Sync
        // the copy use case for this class happens to be immutable, so we'll have to use this method instead of a copy constructor
        public void SyncModel(DisplayModel otherModel)
        {
            RemoveSync(); //? changes to the DisplayInterval & DisplayIntervalType variables will cause this to be called regardless

            DisplayInterval = otherModel.DisplayInterval;

            DisplayIntervalType = otherModel.DisplayIntervalType;

            DisplayStyle = otherModel.DisplayStyle;

            parentSyncedModel = otherModel;

            AddSync(otherModel);
        }

        private void AddSync(DisplayModel otherModel)
        {
            otherModel.childSyncedModels.Add(this);
        }

        private void RemoveSync()
        {
            if (parentSyncedModel == null) // removing sync from parent
            {
                while (childSyncedModels.Count > 0)
                {
                    childSyncedModels.Last().RemoveSync();
                }
            }
            else // removing sync from child
            {
                parentSyncedModel?.childSyncedModels.Remove(this);
                parentSyncedModel = null;
            }
        }
        #endregion
    }
}
