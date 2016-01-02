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

        Color trackColor = Color.Red;   // Color of trail overlays
        int updateInterval = 1000;      // Milliseconds between each refresh of trails

        public FormTrackOverlay(MouseTracker tracker, Screen screen)
        {
            bounds = screen;
            mouse = tracker;
            mouse.MouseMoved += MouseMoved;

            bitmap = new Bitmap(screen.Bounds.Width, screen.Bounds.Height, PixelFormat.Format1bppIndexed);
            ColorPalette palette = bitmap.Palette;
            palette.Entries[0] = Color.White;
            palette.Entries[1] = trackColor;
            bitmap.Palette = palette;

            BackgroundImage = bitmap;

            dirtyrows = new ConcurrentBag<int>();
            for (int y = 0; y < screen.Bounds.Height; y++)
                dirtyrows.Add(y);
            UpdateImage();

            this.Location = screen.Bounds.Location;
            this.Size = screen.Bounds.Size;

            InitializeComponent();
            tmrRedraw.Interval = updateInterval;

            if (screen.Primary)
            {
                SetupComponents();
            }
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
            IEnumerable<int> dirty = dirtyrows.ToArray().Distinct().OrderBy(i => i);
            dirtyrows = new ConcurrentBag<int>();

            BitStorageBox px = mouse.GetStorage(bounds);

            foreach (int y in dirty)
            {
                BitmapData bd = bitmap.LockBits(new Rectangle(0, y, bitmap.Width, 1), ImageLockMode.ReadWrite, PixelFormat.Format1bppIndexed);

                byte[] row = px.GetRow(y);
                Marshal.Copy(row, 0, bd.Scan0, row.Length);

                bitmap.UnlockBits(bd);
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
            if (redraw)
            {
                UpdateImage();
                Invalidate();
                redraw = false;
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
