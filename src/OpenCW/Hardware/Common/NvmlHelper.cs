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
        private static bool _dllMissing;
        private static int _initRetryCount;

        public static (uint Temp, uint FanPercent) GetGpuMetrics()
        {
            if (_dllMissing) return (0, 0);

            try
            {
                if (!_initialized)
                {
                    // Allow retry on subsequent polls without overwhelming if persistently failing
                    if (_initRetryCount > 10 && (_initRetryCount % 5 != 0))
                    {
                        _initRetryCount++;
                        return (0, 0);
                    }

                    if (nvmlInit() == 0 && nvmlDeviceGetHandleByIndex(0, out _deviceHandle) == 0 && _deviceHandle != IntPtr.Zero)
                    {
                        _initialized = true;
                        _initRetryCount = 0;
                    }
                    else
                    {
                        _initRetryCount++;
                        return (0, 0);
                    }
                }

                if (_initialized && _deviceHandle != IntPtr.Zero)
                {
                    uint temp = 0;
                    uint fan = 0;

                    int tempRes = nvmlDeviceGetTemperature(_deviceHandle, 0, out uint t);
                    if (tempRes == 0)
                    {
                        temp = t;
                    }
                    else if (tempRes is 15 or 999) // NVML_ERROR_GPU_IS_LOST / GPU asleep
                    {
                        // Reset handle so it re-acquires once GPU wakes up from D3Cold
                        _initialized = false;
                        _deviceHandle = IntPtr.Zero;
                    }

                    if (nvmlDeviceGetFanSpeed(_deviceHandle, out uint f) == 0)
                        fan = f;

                    return (temp, fan);
                }
            }
            catch (DllNotFoundException)
            {
                _dllMissing = true;
            }
            catch (EntryPointNotFoundException)
            {
                _dllMissing = true;
            }
            catch
            {
                _initialized = false;
                _deviceHandle = IntPtr.Zero;
            }
            return (0, 0);
        }
    }
}
