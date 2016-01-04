using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixelTracker
{
    static class GlobalSettings
    {
        // StorageBox settings
        internal static int mainDelay = 60000; // Number of milliseconds between each save
        internal static int waitDelay = 10000;  // If we have passed mainDelay without saving, poll every waitDelay milliseconds

        // FormTrackOverlay settings
        internal static Color trackColor = Color.Red;   // Color of trail overlays on screen
        internal static int updateInterval = 250;      // Milliseconds between each redraw of image

        // Storage type
        internal static bool countPixelHits = false;    // True to keep a count for each pixel how many times the mouse has passed it (heatmap style)
                                                        // False to just set a boolean to true if the pixel has been hit sometime
    }
}
