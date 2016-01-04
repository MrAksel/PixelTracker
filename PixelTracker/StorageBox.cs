using System;

namespace PixelTracker
{
    public abstract class StorageBox : IDisposable
    {
        public abstract bool IsSet(int x, int y);
        public abstract void Set(int x, int y, bool set);

        public abstract uint GetCount(int x, int y);
        public abstract void IncCount(int x, int y);

        public abstract bool[] GetBoolBuffer(int row);      // Gets an array of bools where true=pixel covered.
        public abstract byte[] GetBitBuffer(int row);       // Returns the bool array of the row packed as single bits into bytes
        public abstract uint[] GetHitCountBuffer(int row);  // Count of number of hits on each pixel

        public abstract void Dispose();
    }
}
