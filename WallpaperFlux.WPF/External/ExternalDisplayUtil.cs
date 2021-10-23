using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WallpaperFlux.Core.External;
using WallpaperFlux.WPF.Util;

namespace WallpaperFlux.WPF.External
{
    // for use with parent assemblies
    public class ExternalDisplayUtil : IExternalDisplayUtil
    {
        public int GetDisplayCount() => DisplayUtil.Displays.Count();

        public void ResetLargestDisplayIndexOrder() => DisplayUtil.ResetLargestDisplayIndexOrder();

        public IEnumerable<int> GetLargestDisplayIndexOrder() => DisplayUtil.GetLargestDisplayIndexOrder();
    }
}
