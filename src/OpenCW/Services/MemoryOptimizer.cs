using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace OpenCW.Services
{
    public static class MemoryOptimizer
    {
        [DllImport("kernel32.dll", EntryPoint = "SetProcessWorkingSetSize")]
        private static extern bool SetProcessWorkingSetSize(IntPtr proc, IntPtr min, IntPtr max);

        [DllImport("psapi.dll")]
        private static extern int EmptyWorkingSet(IntPtr hwProc);

        public static void TrimMemory()
        {
            try
            {
                // Force immediate generation 0, 1 and 2 collection
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);
                GC.WaitForPendingFinalizers();
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);

                // Release physical pages back to Windows memory manager
                IntPtr handle = Process.GetCurrentProcess().Handle;
                EmptyWorkingSet(handle);
                SetProcessWorkingSetSize(handle, new IntPtr(-1), new IntPtr(-1));
            }
            catch
            {
                // Silently ignore memory trim exceptions
            }
        }
    }
}
