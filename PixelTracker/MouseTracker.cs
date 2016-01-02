using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace PixelTracker
{
    public class MouseTracker
    {
        bool enabled;
        MouseHooks hooks;
        Dictionary<Screen, BitStorageBox> storage;

        public event MouseHooks.MouseMovedEventHandler MouseMoved;
        
        public MouseTracker()
        {
            hooks = new MouseHooks();
            hooks.MouseMoved += OnMouseMoved;
        }

        public BitStorageBox GetStorage(Screen s)
        {
            return storage[s];
        }

        private void InitStorage()
        {
            storage = new Dictionary<Screen, BitStorageBox>();
            foreach (Screen s in Screen.AllScreens)
            {
                string filename = string.Format("{0}@({1},{2}).{3}x{4}.dat", s.DeviceName, s.Bounds.X, s.Bounds.Y, s.Bounds.Width, s.Bounds.Height);
                filename = filename.Replace(@"\\.\", @"AbsPath-");

                BitStorageBox box = new BitStorageBox(filename, s.Bounds.Width, s.Bounds.Height);
                storage.Add(s, box);

                Debug.WriteLine("Monitoring screen at {0}", (object)filename);
            }
        }

        private void OnMouseMoved(int x, int y)
        {
            if (enabled && x >= 0 && y >= 0)
            {
                Screen s = Screen.FromPoint(new Point(x, y));
                storage[s].Set(x, y, true);
                MouseMoved(x, y);
            }
        }

        internal void Start()
        {
            enabled = true;
            InitStorage();
            hooks.StartHook();
        }

        internal void Stop()
        {
            enabled = false;
            hooks.StopHook();
            DisposeStorage();
        }

        private void DisposeStorage()
        {
            foreach (BitStorageBox box in storage.Values)
            {
                box.Dispose();
            }
            storage = null;
        }
    }
}