using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace PixelTracker
{
    // Stores every pixel as a uint - 32 times the size of the BitStorageBox
    public class HeatmapStorageBox : StorageBox
    {
        uint[][] data;
        ConcurrentBag<int> dirtyrows;
        object dataLock;

        uint pxLowCount = uint.MaxValue;
        uint pxMaxCount = 0;

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
                uint val = data[y][x]++;
                dirtyrows.Add(y);

                if (val > pxMaxCount) // Update highest pixel
                    pxMaxCount = val;
                if ((val - 1) == pxLowCount) // If this might have been the lowest value in the array force a cache update on pxLowCount
                    pxLowCount = uint.MaxValue;
            }
        }


        internal uint GetLowestCount()
        {
            if (pxLowCount == uint.MaxValue) // Invalidated, update cache
            {
                return pxLowCount = data.SelectMany(row => row).Min();
            }
            else // Use cached copy
            {
                return pxLowCount;
            }
        }

        public uint GetHighestCount()
        {
            return pxMaxCount;
        }

        public override bool[] GetBoolBuffer(int row)
        {
            return Array.ConvertAll(data[row], c => c > 0 ? true : false);
        }

        public override byte[] GetBitBuffer(int y)
        {
            byte[] row = new byte[numBytes];
            for (int i = 0; i < numBytes; i++) // Combine all the bools to bytes
            {
                byte val = 0;
                int j = i * 8;
                for (int k = 0; k < 8; k++)
                {
                    if (data[y][j + k] > 0U)
                        val |= (byte)(0x80 >> k);
                }
                row[i] = val;
            }
            return row;
        }

        public override uint[] GetHitCountBuffer(int row)
        {
            return data[row];
        }


        // Just converts the ints into bytes for storage
        private byte[] GetRowData(int y)
        {
            byte[] row = new byte[numBytes];
            for (int x = 0; x < width; x++)
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

        private void LoadData()
        {
            byte[] row = new byte[numBytes];
            for (int y = 0; y < data.Length; y++)
            {
                fs.Read(row, 0, numBytes);
                data[y] = new uint[width];
                for (int i = 0; i < width; i++)
                {
                    int j = i * 4;
                    uint val = ((uint)row[j + 0] << 24) | ((uint)row[j + 1] << 16) | ((uint)row[j + 2] << 8) | ((uint)row[j + 3]);
                    data[y][i] = val;

                    if (val > pxMaxCount)
                        pxMaxCount = val;
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
                int div = 100;
                for (int q = 0; q < div && running; q++)
                    Thread.Sleep(GlobalSettings.mainDelay / div); // Sleep a little bit (but in small doses so we can exit in time)

                if (dirtyrows.Count == 0)
                {
                    while (dirtyrows.Count == 0 && running) // Sleep while we have nothing to do
                        Thread.Sleep(GlobalSettings.waitDelay);
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
                    byte[] row = GetRowData(y);

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