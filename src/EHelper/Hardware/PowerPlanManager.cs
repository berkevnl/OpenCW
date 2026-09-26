using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace EHelper.Hardware
{
    public static class PowerPlanManager
    {
        [DllImport("Powrprof.dll", SetLastError = true)]
        private static extern uint PowerSetActiveScheme(IntPtr userRootPowerKey, ref Guid schemeGuid);

        [DllImport("Powrprof.dll", SetLastError = true)]
        private static extern uint PowerGetActiveScheme(IntPtr userRootPowerKey, out IntPtr activePolicyGuid);

        // Standard Windows Power Scheme GUIDs
        public static readonly Guid HighPerformanceGuid = new("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");
        public static readonly Guid BalancedGuid = new("381b4222-f694-41f0-9685-ff5bb260df2e");
        public static readonly Guid PowerSaverGuid = new("a1841308-3541-4fab-bc81-f71556f20b4a");

        public static bool ApplyPowerPlan(ExcaliburPowerMode mode)
        {
            try
            {
                Guid targetGuid = mode switch
                {
                    ExcaliburPowerMode.HighPerformance => HighPerformanceGuid,
                    ExcaliburPowerMode.Gaming => BalancedGuid,
                    ExcaliburPowerMode.Office => PowerSaverGuid,
                    _ => BalancedGuid
                };

                uint result = PowerSetActiveScheme(IntPtr.Zero, ref targetGuid);
                return result == 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PowerPlanManager] Failed to apply Windows power scheme: {ex.Message}");
                return false;
            }
        }
    }
}
