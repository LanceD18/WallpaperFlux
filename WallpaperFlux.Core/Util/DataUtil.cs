using System;
using System.Collections.Generic;
using System.Text;
using WallpaperFlux.Core.Models.Theme;

namespace WallpaperFlux.Core.Util
{
    // TODO Consider minimizing the use of this or remove it entirely, might be better to move all this information onto a different assembly
    // TODO Consider using event calls to handle accessing the data within models. Keep in mind that this could result in an excessive amount of events being written
    public static class DataUtil
    {
        public static ThemeModel Theme = new ThemeModel(10);
    }
}
