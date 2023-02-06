using System.Collections.Generic;
using System.Linq;
using WallpaperFlux.Core.IoC;
using WallpaperFlux.WPF.Util;

namespace WallpaperFlux.WPF.IoC
{
    // for use with parent assemblies
    public class ExternalDisplayUtil : IExternalDisplayUtil
    {
        public int GetDisplayCount() => DisplayUtil.Displays.Count();

        public void ResetLargestDisplayIndexOrder() => DisplayUtil.ResetLargestDisplayIndexOrder();

        public IEnumerable<int> GetLargestDisplayIndexOrder() => DisplayUtil.GetLargestDisplayIndexOrder();

        public int GetDisplayXAdjustment() => DisplayUtil.DisplayXAdjustment;

        public int GetMinDisplayY() => DisplayUtil.MinDisplayY;
    }
}
