using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PixelTracker
{
    public partial class FormTrackOverlay : Form
    {
        bool redraw;
        Screen bounds;
        MouseTracker mouse;

        Bitmap bitmap;
        ConcurrentBag<int> dirtyrows;

        public FormTrackOverlay(MouseTracker tracker, Screen screen)
        {
            bounds = screen;
            mouse = tracker;
            mouse.MouseMoved += MouseMoved;

            if (GlobalSettings.countPixelHits)
            {
                bitmap = new Bitmap(screen.Bounds.Width, screen.Bounds.Height, PixelFormat.Format24bppRgb); // Needs color to display heatmap
            }
            else
            {
                bitmap = new Bitmap(screen.Bounds.Width, screen.Bounds.Height, PixelFormat.Format1bppIndexed);

                ColorPalette palette = bitmap.Palette;
                palette.Entries[0] = Color.White;
                palette.Entries[1] = GlobalSettings.trackColor;
                bitmap.Palette = palette;
            }

            BackgroundImage = bitmap;

            dirtyrows = new ConcurrentBag<int>();
            for (int y = 0; y < screen.Bounds.Height; y++)
                dirtyrows.Add(y);
            UpdateImage();

            this.Location = screen.Bounds.Location;
            this.Size = screen.Bounds.Size;

            InitializeComponent();
            tmrRedraw.Interval = GlobalSettings.updateInterval;

            if (screen.Primary)
            {
                SetupComponents();
            }
            Log.Write("Initialized overlay for screen " + screen.DeviceName);
        }

        private void SetupComponents()
        {
            ContextMenu cms = new ContextMenu();
            cms.MenuItems.Add("E&xit").Click += exitToolStripMenuItem_Click;

            NotifyIcon ni = new NotifyIcon(components);
            ni.Text = "PixelTracker";
            ni.Icon = Icon;
            ni.ContextMenu = cms;
            ni.Visible = true;
            ni.DoubleClick += nico_DoubleClick;

            Log.Write("This is primary screen");
        }

        private void MouseMoved(int x, int y)
        {
            if (bounds.Bounds.Contains(x, y))
            {
                redraw = true;
                dirtyrows.Add(y);
            }
        }

        private void UpdateImage()
        {
            List<int> dirty = dirtyrows.ToArray().Distinct().OrderBy(i => i).ToList();
            dirtyrows = new ConcurrentBag<int>();

            StorageBox px = mouse.GetStorage(bounds);

            foreach (int y in dirty)
            {
                if (GlobalSettings.countPixelHits)
                {
                    UpdateRowHeatmap(bitmap, px as HeatmapStorageBox, y);
                }
                else
                {
                    UpdateRowBits(bitmap, px as BitStorageBox, y);
                }
            }
            if (dirty.Count > 0)
                Log.Write(string.Format("Updated {0} rows in overlay image", dirty.Count));
        }

        private void UpdateRowHeatmap(Bitmap bitmap, HeatmapStorageBox heatmapStorageBox, int y)
        {
            BitmapData bd = bitmap.LockBits(new Rectangle(0, y, bitmap.Width, 1), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            uint[] row = heatmapStorageBox.GetHitCountBuffer(y);
            uint minHit = heatmapStorageBox.GetLowestCount();
            uint maxHit = heatmapStorageBox.GetHighestCount();

            byte[] stride = new byte[bd.Width * 3];
            for (int x = 0; x < bd.Width; x++)
            {
                int off = x * 3;
                uint cnt = row[x];

                double hue, sat, val;
                Colorize(cnt, minHit, maxHit, out hue, out sat, out val);
                byte r, g, b;
                HsvToRgb(hue, sat, val, out r, out g, out b);

                
                stride[off + 0] = b;
                stride[off + 1] = g;
                stride[off + 2] = r;
                
            }
            Marshal.Copy(stride, 0, bd.Scan0, bd.Stride);
            bitmap.UnlockBits(bd);
        }

        private void UpdateRowBits(Bitmap bitmap, BitStorageBox bitStorageBox, int y)
        {
            BitmapData bd = bitmap.LockBits(new Rectangle(0, y, bitmap.Width, 1), ImageLockMode.ReadWrite, PixelFormat.Format1bppIndexed);

            byte[] row = bitStorageBox.GetBitBuffer(y);
            Marshal.Copy(row, 0, bd.Scan0, row.Length);

            bitmap.UnlockBits(bd);
        }

        private static void Colorize(uint hits, uint minHits, uint maxHits, out double hue, out double sat, out double val)
        {
            if (hits == 0)  // No hits for this pixel, make white (shows as transparent)
            {
                hue = 0.0;
                sat = 0.0;
                val = 1.0;
                return;
            }

            uint diff = maxHits - minHits;
            if (diff == 0)
            {
                // All pixels hit equally many times!!
                hue = 120.0;
                sat = 1.0;
                val = 1.0;
                return;
            }

            // We use hues between 0 (red) and 250 (blue)
            double hotness = (maxHits - hits) / (double)diff;   // How warm this is from 1 to 0 (0 is warmest)
            hue = hotness * 250.0;

            double brightness = hits / (hits + 1.0);      // Sigmoid shaped function. Low counts give darker colors
            val = brightness;

            sat = 1.0;
        }

        // http://stackoverflow.com/questions/359612/how-to-change-rgb-color-to-hsv
        private static void HsvToRgb(double hue, double saturation, double value, out byte r, out byte g, out byte b)
        {
            int hi = Convert.ToInt32(Math.Floor(hue / 60.0)) % 6;
            double f = hue / 60.0 - Math.Floor(hue / 60.0);

            value = value * 255;
            int v = Convert.ToInt32(value);
            int p = Convert.ToInt32(value * (1 - saturation));
            int q = Convert.ToInt32(value * (1 - f * saturation));
            int t = Convert.ToInt32(value * (1 - (1 - f) * saturation));

            if (hi == 0)
            {
                r = (byte)v;
                g = (byte)t;
                b = (byte)p;
            }
            else if (hi == 1)
            {
                r = (byte)q;
                g = (byte)v;
                b = (byte)p;
            }
            else if (hi == 2)
            {
                r = (byte)p;
                g = (byte)v;
                b = (byte)t;
            }
            else if (hi == 3)
            {
                r = (byte)p;
                g = (byte)q;
                b = (byte)v;
            }
            else if (hi == 4)
            {
                r = (byte)t;
                g = (byte)p;
                b = (byte)v;
            }
            else
            {
                r = (byte)v;
                g = (byte)p;
                b = (byte)q;
            }
        }

        private void nico_DoubleClick(object sender, EventArgs e)
        {
            if (Visible)
                Hide();
            else
                Show();
            WindowState = FormWindowState.Normal;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Log.Write("User clicked exit - byebye");
            Application.Exit();
        }

        private void FormTrackOverlay_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
            }
        }

        private void FormTrackOverlay_SizeChanged(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
                Hide();
        }

        private void tmrRedraw_Tick(object sender, EventArgs e)
        {
            if (redraw && Visible)
            {
                redraw = false;
                UpdateImage();
                Invalidate();
            }
        }


        // http://stackoverflow.com/questions/1524035/topmost-form-clicking-through-possible
        #region Click through

        public enum GWL
        {
            ExStyle = -20
        }

        public enum WS_EX
        {
            Transparent = 0x20,
            Layered = 0x80000
        }

        public enum LWA
        {
            ColorKey = 0x1,
            Alpha = 0x2
        }

        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        public static extern int GetWindowLong(IntPtr hWnd, GWL nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        public static extern int SetWindowLong(IntPtr hWnd, GWL nIndex, int dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetLayeredWindowAttributes")]
        public static extern bool SetLayeredWindowAttributes(IntPtr hWnd, int crKey, byte alpha, LWA dwFlags);

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            int wl = GetWindowLong(this.Handle, GWL.ExStyle);
            wl = wl | 0x80000 | 0x20;
            SetWindowLong(this.Handle, GWL.ExStyle, wl);
        }

        #endregion
    }
}
