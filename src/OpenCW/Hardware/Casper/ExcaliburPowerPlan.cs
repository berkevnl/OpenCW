using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using OpenCW.Core;

namespace OpenCW.Hardware.Casper
{
    public static class ExcaliburPowerPlan
    {
        [DllImport("Powrprof.dll", SetLastError = true)]
        private static extern uint PowerSetActiveScheme(IntPtr userRootPowerKey, ref Guid schemeGuid);

        public static readonly Guid HighPerformanceGuid = new("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");
        public static readonly Guid BalancedGuid = new("381b4222-f694-41f0-9685-ff5bb260df2e");
        public static readonly Guid PowerSaverGuid = new("a1841308-3541-4fab-bc81-f71556f20b4a");

        public static bool ApplyPowerPlan(PowerMode mode)
        {
            try
            {
                Guid targetGuid = mode switch
                {
                    PowerMode.HighPerformance => HighPerformanceGuid,
                    PowerMode.Gaming => BalancedGuid,
                    PowerMode.Office => PowerSaverGuid,
                    _ => BalancedGuid
                };

                uint result = PowerSetActiveScheme(IntPtr.Zero, ref targetGuid);
                return result == 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ExcaliburPowerPlan] Failed to apply Windows power scheme: {ex.Message}");
                return false;
            }
        }
    }
}
