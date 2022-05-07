using System;
using System.Collections.Generic;
using System.Text;

namespace WallpaperFlux.Core.Models.Controls
{
    public interface ITabModel<out T>
    {
        T[] GetSelectedItems();

        T[] GetAllItems();

        void SelectAllItems();

        void DeselectAllItems();
    }
}
