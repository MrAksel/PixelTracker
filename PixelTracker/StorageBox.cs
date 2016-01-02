using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixelTracker
{
    public abstract class StorageBox : IDisposable
    {

        public abstract bool IsSet(int x, int y);
        public abstract void Set(int x, int y, bool set);

        public abstract uint GetCount(int x, int y);
        public abstract void IncCount(int x, int y);

        public abstract byte[] GetBitBuffer(int row);
        public abstract uint[] GetHitCountBuffer(int row);

        public abstract void Dispose();
    }
}
