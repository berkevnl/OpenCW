using System;
using System.Runtime.InteropServices;

namespace OpenCW.Hardware.Common
{
    public static class NvmlHelper
    {
        [DllImport("nvml.dll", EntryPoint = "nvmlInit_v2")]
        private static extern int nvmlInit();

        [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetHandleByIndex_v2")]
        private static extern int nvmlDeviceGetHandleByIndex(uint index, out IntPtr device);

        [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetTemperature")]
        private static extern int nvmlDeviceGetTemperature(IntPtr device, int sensorType, out uint temp);

        [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetFanSpeed")]
        private static extern int nvmlDeviceGetFanSpeed(IntPtr device, out uint fanSpeed);

        private static bool _initialized;
        private static IntPtr _deviceHandle = IntPtr.Zero;
        private static bool _nvmlUnavailable;

        public static (uint Temp, uint FanPercent) GetGpuMetrics()
        {
            if (_nvmlUnavailable) return (0, 0);

            try
            {
                if (!_initialized)
                {
                    if (nvmlInit() == 0 && nvmlDeviceGetHandleByIndex(0, out _deviceHandle) == 0)
                    {
                        _initialized = true;
                    }
                    else
                    {
                        _nvmlUnavailable = true;
                        return (0, 0);
                    }
                }

                if (_initialized && _deviceHandle != IntPtr.Zero)
                {
                    uint temp = 0;
                    uint fan = 0;

                    if (nvmlDeviceGetTemperature(_deviceHandle, 0, out uint t) == 0)
                        temp = t;

                    if (nvmlDeviceGetFanSpeed(_deviceHandle, out uint f) == 0)
                        fan = f;

                    return (temp, fan);
                }
            }
            catch
            {
                _nvmlUnavailable = true;
            }
            return (0, 0);
        }
    }
}
