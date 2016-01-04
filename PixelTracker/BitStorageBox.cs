using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;

namespace PixelTracker
{
    // Uses a single bit for each pixel
    public class BitStorageBox : StorageBox
    {
        int mainDelay = 60000; // Number of milliseconds between each save
        int waitDelay = 1000; // If we have passed mainDelay without saving, poll every waitDelay milliseconds

        bool[][] data;
        ConcurrentBag<int> dirtyrows;
        object dataLock;

        int width;    // Elements in a row
        int numBytes; // Number of bytes on a row
        FileStream fs;

        bool running;
        Thread saver;

        public BitStorageBox(string file, int width, int height)
        {
            data = new bool[height][];

            dataLock = new object();
            dirtyrows = new ConcurrentBag<int>();
            fs = new FileStream(file, FileMode.OpenOrCreate, FileAccess.ReadWrite);

            this.width = width + 8 - width % 8;   // Pad to a multiple of 8
            this.numBytes = width / 8;            // Number of bytes used for this row
            LoadData();
            StartSaver();
        }


        public override bool IsSet(int x, int y)
        {
            return data[y][x];
        }

        public override uint GetCount(int x, int y)
        {
            return IsSet(x, y) ? 1U : 0U;
        }

        public override void Set(int x, int y, bool set)
        {
            if (y >= data.Length || x >= width || y < 0 || x < 0)
                return;
            lock (dataLock)
            {
                if (data[y][x] != set)
                {
                    data[y][x] = set;
                    dirtyrows.Add(y);
                }
            }
        }

        public override void IncCount(int x, int y)
        {
            Set(x, y, true);
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
                    if (data[y][j + k])
                        val |= (byte)(0x80 >> k);
                }
                row[i] = val;
            }
            return row;
        }

        public override uint[] GetHitCountBuffer(int row)
        {
            return Array.ConvertAll(data[row], b => b ? 1U : 0U);
        }


        private void LoadData()
        {
            byte[] row = new byte[numBytes];
            for (int y = 0; y < data.Length; y++)
            {
                fs.Read(row, 0, numBytes);
                data[y] = new bool[width];
                for (int i = 0; i < numBytes; i++) // Since the array is padded we won't have to worry about bounds
                {
                    int j = i * 8;
                    data[y][j + 0] = (row[i] & 0x80) != 0;
                    data[y][j + 1] = (row[i] & 0x40) != 0;
                    data[y][j + 2] = (row[i] & 0x20) != 0;
                    data[y][j + 3] = (row[i] & 0x10) != 0;
                    data[y][j + 4] = (row[i] & 0x08) != 0;
                    data[y][j + 5] = (row[i] & 0x04) != 0;
                    data[y][j + 6] = (row[i] & 0x02) != 0;
                    data[y][j + 7] = (row[i] & 0x01) != 0;
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