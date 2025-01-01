using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using MvvmCross.ViewModels;
using WallpaperFlux.Core.IoC;
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
        public bool NotSyncedToParent => parentSyncedModel == null;

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

                if (NotSyncedToParent)
                {
                    foreach (DisplayModel model in childSyncedModels)
                    {
                        if (model != this)
                        {
                            model.DisplayTimerCurrent = DisplayTimerCurrent;
                        }
                    }

                    if (DisplayTimerCurrent <= 0 && DisplayTimerMax != 0)
                    {
                        ResetTimer(false);
                    }
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
                WallpaperUtil.WallpaperHandler.OnWallpaperStyleChange(_displayIndex, _displayStyle);
            }
        }

        public readonly IExternalTimer Timer;

        private Action<int, bool, bool> OnTimerReset;

        private DisplayModel parentSyncedModel;
        private List<DisplayModel> childSyncedModels = new List<DisplayModel>();

        //?-----Constructor-----
        public DisplayModel(IExternalTimer timer, Action<int, bool, bool> onTimerReset)
        {
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += TimerOnTick;
            timer.Start();

            Timer = timer;

            this.OnTimerReset = onTimerReset;
        }

        //-----Methods-----
        private void TimerOnTick(object sender, EventArgs e)
        {
            if (JsonUtil.IsLoadingData) return; // ? don't want to accidentally swap wallpaper before the data is even loaded

            if (NotSyncedToParent)
            {
                if (DisplayIntervalType != IntervalType.None)
                {
                    DisplayTimerCurrent -= 1;
                }
            }
        }

        private void ResetMaxTime()
        {
            RemoveSync();
            DisplayTimerMax = DisplayTimerCurrent = DisplayInterval * (int) DisplayIntervalType;
        }

        public void ResetTimer(bool ignoreResetEvent)
        {
            if (NotSyncedToParent)
            {
                DisplayTimerCurrent = DisplayTimerMax;
                if (!ignoreResetEvent) //? this means that we just want to reset the timer but we do not want to change the wallpaper
                {
                    OnTimerReset?.Invoke(_displayIndex, false, false);

                    //? _displayIndex uses the value appropriate for arrays while DisplayIndex uses the value appropriate for WPF/visual information

                    foreach (DisplayModel model in childSyncedModels)
                    {
                        if (model != this)
                        {
                            model.OnTimerReset?.Invoke(model._displayIndex, false, false); //! force change will skip videos/animations
                        }
                    }
                }
            }
            else
            {
                Debug.WriteLine("Timer Reset Prevented Due to Parent: " + parentSyncedModel.DisplayIndex);
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
