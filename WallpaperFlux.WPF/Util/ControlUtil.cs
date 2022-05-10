using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows.Controls;
using System.Windows.Input;
using WallpaperFlux.Core.Models;
using WallpaperFlux.Core.Models.Controls;

namespace WallpaperFlux.WPF.Util
{
    public static class ControlUtil
    {
        #region Tab Control

        public static void EnsureSingularSelection<T, U>(IEnumerable<T> items, ITabModel<U> selectedItem)
        {
            //? Deselect items from other tabs if a normal selection is made
            // We are not looking to select multiple items if a singular selection is made, clear selections from other pages
            // Without this other pages will retain their selection as they have separate ListBoxes
            if (Keyboard.Modifiers != ModifierKeys.Control && Keyboard.Modifiers != ModifierKeys.Shift)
            {
                Debug.WriteLine("Singular Selection | Deselecting Other Tabs");
                foreach (ITabModel<U> tab in items)
                {
                    if (tab != selectedItem) // the WPF will handle deselecting the selected item itself (without deselecting the singular selection)
                    {
                        tab.DeselectAllItems();
                    }
                }
            }
        }

        #endregion
    }
}
