using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PixelTracker
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            MouseTracker m = new MouseTracker();
            m.Start();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            foreach (Screen s in Screen.AllScreens)
                new FormTrackOverlay(m, s).Show();
            Application.Run();

            m.Stop();
        }
    }
}
