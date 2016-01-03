using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
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
            Log.Init();
            
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Application.ThreadException += Application_ThreadException;

            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Log.Write("Initialized exception mode");

            MouseTracker m = new MouseTracker();
            m.Start();
            Log.Write("Started mouse tracker");

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            foreach (Screen s in Screen.AllScreens)
                new FormTrackOverlay(m, s).Show();

            Log.Write("Starting message loop");
            Application.Run();

            Log.Write("Exiting\n\n");
            m.Stop();

            Log.Close();

            Application.ThreadException -= Application_ThreadException;
            AppDomain.CurrentDomain.UnhandledException -= CurrentDomain_UnhandledException;
        }


        [HandleProcessCorruptedStateExceptions]
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try // We really don't want to create a stack overflow if writing to log throws an unhandled exception
            {
                Log.Write("Caught unhandled exception in application domain. Trying to gather additional info");

                Exception ex = e.ExceptionObject as Exception;
                if (ex != null)
                {
                    LogException(ex);
                }
                else if (e.ExceptionObject != null)
                {
                    Log.Write(string.Format("ExceptionObject was: {0}: {1}", e.ExceptionObject.GetType().FullName, e.ExceptionObject));
                }
                else
                {
                    Log.Write("No additional info");
                }

                if (e.IsTerminating)
                    Log.Write("Nothing to do - terminating");
            }
            catch (Exception)
            {
                // Nothing to do - we failed
            }
        }

        private static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            try // We really don't want to create a stack overflow if writing to log throws an unhandled exception
            {
                Log.Write("Caught unhandled exception in application UI thread. Trying to gather additional info");
                LogException(e.Exception);
            }
            catch (Exception)
            {
                // Nothing to do - we failed
            }
        }

        private static void LogException(Exception ex)
        {
            Log.Write(string.Format("{0}: {1}", ex.GetType().FullName, ex.Message));
            int indent = 4;

            ex = ex.InnerException;
            while (ex != null)
            {
                Log.Write(new string(' ', indent) + string.Format("- Inner {0}: {1}", ex.GetType().FullName, ex.Message));
                ex = ex.InnerException;
                indent += 4;
            }

            Log.Write(ex.StackTrace);
        }
    }
}
