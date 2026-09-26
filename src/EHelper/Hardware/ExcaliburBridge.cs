using System;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;

namespace EHelper.Hardware
{
    public class ExcaliburBridge : IHardwareBridge
    {
        private const string WmiNamespace = @"root\wmi";
        private const string WmiClassName = "RW_GMWMI";
        
        private readonly object _syncLock = new();
        private ManagementObject? _wmiInstance;
        private ManagementClass? _wmiClass;
        private bool _isDisposed;

        public bool IsHardwareConnected { get; private set; }
        public string DeviceModel { get; private set; }

        public ExcaliburBridge()
        {
            var (model, _, isExcalibur) = ModelDetector.DetectSystem();
            DeviceModel = model;

            InitializeWmi();
        }

        private void InitializeWmi()
        {
            try
            {
                var scope = new ManagementScope(WmiNamespace);
                scope.Connect();

                // 1. Initialize class
                _wmiClass = new ManagementClass(scope, new ManagementPath(WmiClassName), null);

                // 2. Find active instance of RW_GMWMI
                using var instances = _wmiClass.GetInstances();
                foreach (ManagementObject inst in instances)
                {
                    _wmiInstance = inst;
                    break;
                }

                IsHardwareConnected = (_wmiInstance != null || _wmiClass != null);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ExcaliburBridge] WMI connection failed: {ex.Message}");
                IsHardwareConnected = false;
            }
        }

        private bool PerformSmi(ref SMI_STRUCT_S smi)
        {
            if (!IsHardwareConnected && _wmiInstance == null && _wmiClass == null)
            {
                return false;
            }

            lock (_syncLock)
            {
                int size = Marshal.SizeOf<SMI_STRUCT_S>();
                byte[] inBuffer = new byte[size];
                IntPtr ptr = Marshal.AllocHGlobal(size);

                try
                {
                    Marshal.StructureToPtr(smi, ptr, false);
                    Marshal.Copy(ptr, inBuffer, 0, size);

                    byte[]? outBuffer = null;

                    // Option A: Write and read directly on ManagementClass (matching original ControlCenter decompilation)
                    if (_wmiClass != null)
                    {
                        try
                        {
                            _wmiClass["BufferBytes"] = inBuffer;
                            outBuffer = _wmiClass["BufferBytes"] as byte[];
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[ExcaliburBridge] _wmiClass SMI write/read failed: {ex.Message}");
                        }
                    }

                    // Option B: Fallback to active instance without calling Put() (dynamic ACPI providers do not support Put)
                    if ((outBuffer == null || outBuffer.Length < size) && _wmiInstance != null)
                    {
                        try
                        {
                            _wmiInstance.SetPropertyValue("BufferBytes", inBuffer);
                            outBuffer = _wmiInstance.GetPropertyValue("BufferBytes") as byte[];
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[ExcaliburBridge] _wmiInstance SMI write/read failed: {ex.Message}");
                        }
                    }

                    if (outBuffer != null && outBuffer.Length >= size)
                    {
                        Marshal.Copy(outBuffer, 0, ptr, size);
                        smi = Marshal.PtrToStructure<SMI_STRUCT_S>(ptr);
                        return true;
                    }

                    return false;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ExcaliburBridge] SMI Execution failed: {ex.Message}");
                    return false;
                }
                finally
                {
                    Marshal.FreeHGlobal(ptr);
                }
            }
        }

        public HardwareTelemetry GetTelemetry()
        {
            var smi = new SMI_STRUCT_S();
            smi.Clear();
            smi.a0 = 0xFA00; // Read
            smi.a1 = 0x0200; // CPU & GPU

            bool success = PerformSmi(ref smi);
            byte cpuTemp = success ? (byte)smi.a2 : (byte)0;
            byte gpuTemp = success ? (byte)smi.a3 : (byte)0;
            ushort cpuRpm = success ? (ushort)smi.a4 : (ushort)0;
            ushort gpuRpm = success ? (ushort)smi.a5 : (ushort)0;

            // Fallback for CPU temperature if WMI SMI returned 0
            if (cpuTemp == 0)
            {
                cpuTemp = GetThermalZoneTemperature();
            }

            // Fallback for GPU temperature if dedicated NVIDIA GPU is present and SMI returned 0
            if (gpuTemp == 0)
            {
                gpuTemp = (byte)NvmlHelper.GetGpuTemperature();
            }

            if (success || cpuTemp > 0 || gpuTemp > 0)
            {
                return new HardwareTelemetry(
                    CpuTemperature: cpuTemp,
                    GpuTemperature: gpuTemp,
                    CpuFanRpm: cpuRpm,
                    GpuFanRpm: gpuRpm,
                    SysTemperature: 0,
                    SysFanRpm: 0,
                    IsAvailable: true
                );
            }

            return new HardwareTelemetry(0, 0, 0, 0, 0, 0, false);
        }

        private static byte GetThermalZoneTemperature()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(@"root\wmi", "SELECT CurrentTemperature FROM MSAcpi_ThermalZoneTemperature");
                foreach (ManagementObject obj in searcher.Get())
                {
                    if (obj["CurrentTemperature"] is uint k && k > 2732)
                    {
                        // In tenths of Kelvin: (k - 2732) / 10
                        int c = (int)((k - 2732) / 10);
                        if (c is > 10 and < 115) return (byte)c;
                    }
                }
            }
            catch { }
            return 0;
        }

        public bool SetPowerMode(ExcaliburPowerMode mode)
        {
            var smi = new SMI_STRUCT_S();
            smi.Clear();
            smi.a0 = 0xFB00; // Write
            smi.a1 = 0x0300; // Power Mode
            smi.a2 = (uint)mode;
            return PerformSmi(ref smi);
        }

        public bool SetManualFanControl(bool enabled)
        {
            var smi = new SMI_STRUCT_S();
            smi.Clear();
            smi.a0 = 0xFB00;
            smi.a1 = 0x0206; // Fan Override Mode
            smi.a2 = enabled ? 1u : 0u;
            return PerformSmi(ref smi);
        }

        public bool SetFanSpeed(byte cpuPercent, byte gpuPercent, byte sysPercent = 255)
        {
            var smi = new SMI_STRUCT_S();
            smi.Clear();
            smi.a0 = 0xFB00;
            smi.a1 = 0x0205; // Fan Speed Percentage
            smi.a2 = cpuPercent;
            smi.a3 = gpuPercent;
            smi.a4 = sysPercent;
            return PerformSmi(ref smi);
        }

        public void ResetFansToAuto()
        {
            SetManualFanControl(false);
            SetFanSpeed(255, 255, 255);
        }

        public static uint PackLedData(ExcaliburLedMode mode, byte brightness, byte r, byte g, byte b)
        {
            // Format: [Mode (4-bit)] [Brightness (4-bit)] [Red (8-bit)] [Green (8-bit)] [Blue (8-bit)]
            uint modeAndAlpha = (uint)(((byte)mode << 4) | (brightness & 0x0F));
            uint rgb = (uint)((r << 16) | (g << 8) | b);
            return (modeAndAlpha << 24) | rgb;
        }

        public bool SetLed(ExcaliburLedZone zone, ExcaliburLedMode mode, byte brightness, byte r, byte g, byte b)
        {
            uint packed = PackLedData(mode, brightness, r, g, b);
            var smi = new SMI_STRUCT_S();
            smi.Clear();
            smi.a0 = 0xFB00;
            smi.a1 = 0x0100; // LED Control
            smi.a2 = (uint)zone;
            smi.a3 = packed;
            return PerformSmi(ref smi);
        }

        public bool SetAllKeyboardLed(ExcaliburLedMode mode, byte brightness, byte r, byte g, byte b)
        {
            return SetLed(ExcaliburLedZone.AllKeyboard, mode, brightness, r, g, b);
        }

        public bool TurnOffAllLights()
        {
            return SetLed(ExcaliburLedZone.All, ExcaliburLedMode.Off, 0, 0, 0, 0);
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            try
            {
                _wmiInstance?.Dispose();
                _wmiClass?.Dispose();
            }
            catch
            {
                // Suppress disposal errors
            }
        }

        // Lightweight NVIDIA NVML Helper for fallback GPU temperature
        private static class NvmlHelper
        {
            [DllImport("nvml.dll", EntryPoint = "nvmlInit_v2")]
            private static extern int nvmlInit();

            [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetHandleByIndex_v2")]
            private static extern int nvmlDeviceGetHandleByIndex(uint index, out IntPtr device);

            [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetTemperature")]
            private static extern int nvmlDeviceGetTemperature(IntPtr device, int sensorType, out uint temp);

            private static bool _initialized;
            private static IntPtr _deviceHandle = IntPtr.Zero;
            private static bool _nvmlUnavailable;

            public static uint GetGpuTemperature()
            {
                if (_nvmlUnavailable) return 0;

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
                            return 0;
                        }
                    }

                    if (_initialized && _deviceHandle != IntPtr.Zero)
                    {
                        if (nvmlDeviceGetTemperature(_deviceHandle, 0, out uint temp) == 0)
                        {
                            return temp;
                        }
                    }
                }
                catch
                {
                    _nvmlUnavailable = true;
                }
                return 0;
            }
        }
    }
}
