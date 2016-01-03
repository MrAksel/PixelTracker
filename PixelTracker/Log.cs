using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PixelTracker
{
    static class Log
    {
        static volatile bool do_flush;

        static Timer flushTimer;
        static FileStream logOutStream;
        static StreamWriter logOutWriter;

        public static void Init()
        {
            logOutStream = new FileStream("program.log", FileMode.Append, FileAccess.Write);
            logOutWriter = new StreamWriter(logOutStream);
            flushTimer = new Timer(flushCb, null, 0, 1000);
        }

        private static void flushCb(object state)
        {
            if (do_flush && logOutWriter != null)
            {
                do_flush = false;
                logOutWriter.Flush();
                logOutStream.Flush();
            }
        }

        public static void Close()
        {
            logOutWriter.Close();
        }

        public static void Write(string logMessage, [CallerMemberName] string caller = "")
        {
            string output = string.Format("[{0}] {1}: {2}", DateTime.Now, caller, logMessage);
            logOutWriter.WriteLine(output);
            Debug.WriteLine(output);
            do_flush = true;
        }
    }
}
