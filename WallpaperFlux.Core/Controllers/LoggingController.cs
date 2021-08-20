using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace WallpaperFlux.Core.Controllers
{
    public class LoggingController
    {
        // TODO This will serve as a way of logging warnings to the UI, such as using an invalid file in your theme
        // TODO This should be tied to the settings, allowing individual warnings, categories of warnings, or all warnings, to be disabled

        public void LogToUser(string message)
        {
            Debug.WriteLine(message);
        }
    }
}
