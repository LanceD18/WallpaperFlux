using System;
using System.Collections.Generic;
using System.Text;

namespace WallpaperFlux.Core.Tools
{
    // TODO
    // 1. Multiple Precision Levels Available (1 = 16x16, 2 = 32x32, 3 = 64x64, 4 = 128x128)
    // 2. Once a precision level is picked, reduce the image & any comparison image down to the given precision size
    // 3. Reduce the image to black & white, allowing it to be turned into a boolean table
    // 4. Compare from there, the user can pick what range of similarity they'll allow to be visible, from 0% to 100% matching pixels

    // Fast Check: Compare the 4 corner pixels, the 4 mid-corner pixels, and the middle pixel. If more pixels match than don't, scan
    //     - Fast check will scan the images before applying the original precision
    //     - It will use it's own form of precision however,each increment in precision reduce the RGB range required for pixel match:
    //          1 = 10%, 2 = 5%, 3 = 2.5%, 4 = Exact Match

    public class SimilarityDetector
    {
        
    }
}
