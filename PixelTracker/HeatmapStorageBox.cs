using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace PixelTracker
{
    // Stores every pixel as a uint - 32 times the size of the BitStorageBox
    public class HeatmapStorageBox : StorageBox
    {
        int mainDelay = 60000; // Number of milliseconds between each save
        int waitDelay = 1000; // If we have passed mainDelay without saving, poll every waitDelay milliseconds

        uint[][] data;
        ConcurrentBag<int> dirtyrows;
        object dataLock;

        int width;    // Elements in a row
        int numBytes; // Number of bytes on a row
        FileStream fs;

        bool running;
        Thread saver;

        public HeatmapStorageBox(string file, int width, int height)
        {
            data = new uint[height][];

            dataLock = new object();
            dirtyrows = new ConcurrentBag<int>();
            fs = new FileStream(file, FileMode.OpenOrCreate, FileAccess.ReadWrite);

            this.width = width;
            this.numBytes = width * 4;  // Number of bytes used for this row

            LoadData();
            StartSaver();
        }


        public override bool IsSet(int x, int y)
        {
            return GetCount(x, y) > 0;
        }

        public override void Set(int x, int y, bool set)
        {
            if (set)
                IncCount(x, y);
        }

        public override uint GetCount(int x, int y)
        {
            return data[y][x];
        }

        public override void IncCount(int x, int y)
        {
            if (y >= data.Length || x >= width || y < 0 || x < 0)
                return;
            lock (dataLock)
            {
                data[y][x]++;
                dirtyrows.Add(y);
            }
        }

        public override byte[] GetBitBuffer(int y)
        {
            byte[] row = new byte[numBytes];
            for (int x = 0; x < width; x++) // Combine all the bools to bytes
            {
                int i = x * 4;
                uint val = data[y][x];
                row[i + 0] = (byte)(val >> 24);
                row[i + 1] = (byte)(val >> 16);
                row[i + 2] = (byte)(val >> 8);
                row[i + 3] = (byte)(val);
            }
            return row;
        }

        public override uint[] GetHitCountBuffer(int row)
        {
            return data[row];
        }


        private void LoadData()
        {
            byte[] row = new byte[numBytes];
            for (int y = 0; y < data.Length; y++)
            {
                fs.Read(row, 0, numBytes);
                data[y] = new uint[width];
                for (int i = 0; i < numBytes; i++)
                {
                    int j = i * 4;
                    data[y][i] = ((uint)row[0] << 24) | ((uint)row[1] << 16) | ((uint)row[2] << 8) | ((uint)row[3]);
                }
            }
            Log.Write("Loaded data from " + fs.Name);
        }

        private void StartSaver()
        {
            running = true;
            saver = new Thread(SaveThread);
            saver.Start();
        }

        private void SaveThread()
        {
            while (running)
            {
                if (dirtyrows.Count == 0)
                {
                    while (dirtyrows.Count == 0 && running) // Sleep while we have nothing to do
                        Thread.Sleep(waitDelay);
                }
                else
                {
                    int div = 100;
                    for (int q = 0; q < div && running; q++)
                        Thread.Sleep(mainDelay / div); // Sleep a little bit anyway (but in small doses so we can exit in time)
                }

                if (!running)
                    break;

                int[] dirty;
                lock (dataLock)
                {
                    dirty = dirtyrows.ToArray();
                    dirtyrows = new ConcurrentBag<int>();
                }

                int tot = 0;
                foreach (int y in dirty)
                {
                    byte[] row = GetBitBuffer(y);

                    fs.Seek(y * numBytes, SeekOrigin.Begin);
                    fs.Write(row, 0, numBytes);
                    tot += numBytes;
                }
                Log.Write(string.Format("Saved {0} bytes to {1}", tot, fs.Name));
            }
        }

        public override void Dispose()
        {
            running = false;
            if (saver != null && saver.IsAlive)
                saver.Join();

            fs.Flush();
            fs.Close();
            Log.Write("Closed " + fs.Name);
        }
    }
}