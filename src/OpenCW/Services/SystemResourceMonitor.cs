using System;
using System.IO;
using System.Runtime.InteropServices;

namespace OpenCW.Services
{
    public record SystemResourceTelemetry(
        double RamUsedGb,
        double RamTotalGb,
        int RamPercent,
        double SsdUsedGb,
        double SsdTotalGb,
        int SsdPercent
    );

    public static class SystemResourceMonitor
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

        public static SystemResourceTelemetry GetMetrics()
        {
            // 1. RAM Metrics via Win32 API (Instantaneous, 0 CPU overhead)
            double ramUsedGb = 0;
            double ramTotalGb = 16.0;
            int ramPercent = 0;

            var memStatus = new MEMORYSTATUSEX();
            memStatus.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            if (GlobalMemoryStatusEx(ref memStatus))
            {
                double total = memStatus.ullTotalPhys / (1024.0 * 1024 * 1024);
                double avail = memStatus.ullAvailPhys / (1024.0 * 1024 * 1024);
                ramTotalGb = total;
                ramUsedGb = total - avail;
                ramPercent = (int)memStatus.dwMemoryLoad;
            }

            // 2. SSD Metrics (System Drive C:)
            double ssdUsedGb = 0;
            double ssdTotalGb = 512.0;
            int ssdPercent = 0;

            try
            {
                string sysDrive = Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\";
                var driveInfo = new DriveInfo(sysDrive);
                if (driveInfo.IsReady)
                {
                    double total = driveInfo.TotalSize / (1024.0 * 1024 * 1024);
                    double free = driveInfo.TotalFreeSpace / (1024.0 * 1024 * 1024);
                    ssdTotalGb = total;
                    ssdUsedGb = total - free;
                    ssdPercent = (int)((ssdUsedGb / total) * 100);
                }
            }
            catch
            {
                // Silently fallback if drive query fails
            }

            return new SystemResourceTelemetry(
                RamUsedGb: ramUsedGb,
                RamTotalGb: ramTotalGb,
                RamPercent: ramPercent,
                SsdUsedGb: ssdUsedGb,
                SsdTotalGb: ssdTotalGb,
                SsdPercent: ssdPercent
            );
        }
    }
}
