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

                using var searcher = new ManagementObjectSearcher(scope, new ObjectQuery($"SELECT * FROM {WmiClassName}"));
                foreach (ManagementObject inst in searcher.Get())
                {
                    _wmiInstance = inst;
                    break;
                }

                IsHardwareConnected = (_wmiInstance != null);
                Debug.WriteLine($"[ExcaliburBridge] WMI connection established: {IsHardwareConnected}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ExcaliburBridge] WMI connection failed: {ex.Message}");
                IsHardwareConnected = false;
            }
        }

        private bool PerformSmi(ref SMI_STRUCT_S smi)
        {
            if (_wmiInstance == null)
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

                    // 1. Write inBuffer to WMI ACPI SMI
                    _wmiInstance["BufferBytes"] = inBuffer;
                    _wmiInstance.Put(); // Executes SMI interrupt on Quanta EC

                    // 2. Fetch fresh output buffer from ACPI WMI
                    byte[]? outBuffer = null;

                    try
                    {
                        using var searcher = new ManagementObjectSearcher(WmiNamespace, "SELECT BufferBytes FROM RW_GMWMI");
                        foreach (ManagementObject obj in searcher.Get())
                        {
                            if (obj["BufferBytes"] is byte[] fresh && fresh.Length >= size)
                            {
                                outBuffer = fresh;
                                break;
                            }
                        }
                    }
                    catch { }

                    if (outBuffer == null || outBuffer.Length < size)
                    {
                        try
                        {
                            _wmiInstance.Get();
                            outBuffer = _wmiInstance["BufferBytes"] as byte[];
                        }
                        catch { }
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
                    Debug.WriteLine($"[ExcaliburBridge] SMI Execution error: {ex.Message}");
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
            smi.a0 = 0xFA00; // Read Mode
            smi.a1 = 0x0200; // CPU & GPU Telemetry Subcode

            bool smiSuccess = PerformSmi(ref smi);

            byte cpuTemp = 0;
            byte gpuTemp = 0;
            ushort cpuRpm = 0;
            ushort gpuRpm = 0;

            if (smiSuccess)
            {
                cpuTemp = (byte)(smi.a2 & 0xFF);
                gpuTemp = (byte)(smi.a3 & 0xFF);
                cpuRpm = (ushort)(smi.a4 & 0xFFFF);
                gpuRpm = (ushort)(smi.a5 & 0xFFFF);
            }

            // Fallback for CPU temperature if 0
            if (cpuTemp == 0)
            {
                cpuTemp = GetThermalZoneTemperature();
            }

            // Fallback for GPU temperature & fan if 0
            if (gpuTemp == 0)
            {
                var nvGpu = NvmlHelper.GetGpuMetrics();
                gpuTemp = (byte)nvGpu.Temp;
                if (gpuRpm == 0 && nvGpu.FanPercent > 0)
                {
                    // Estimate RPM from percentage if tachometer is 0
                    gpuRpm = (ushort)(nvGpu.FanPercent * 45); 
                }
            }

            if (smiSuccess || cpuTemp > 0 || gpuTemp > 0 || cpuRpm > 0 || gpuRpm > 0)
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
            smi.a0 = 0xFB00; // Write Mode
            smi.a1 = 0x0300; // Power Mode Subcode
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
            smi.a1 = 0x0205; // Fan Speed
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
            smi.a1 = 0x0100; // LED Control Subcode
            smi.a2 = (uint)zone;
            smi.a3 = packed;
            return PerformSmi(ref smi);
        }

        public bool SetAllKeyboardLed(ExcaliburLedMode mode, byte brightness, byte r, byte g, byte b)
        {
            if (mode == ExcaliburLedMode.Off || brightness == 0)
            {
                return TurnOffAllLights();
            }

            // Prime the EC controller PWM if switching into breathing or dynamic modes
            if (mode == ExcaliburLedMode.Breathing || mode == ExcaliburLedMode.ColorfulCycle)
            {
                SetLed(ExcaliburLedZone.All, ExcaliburLedMode.Static, brightness, r, g, b);
                SetLed(ExcaliburLedZone.AllKeyboard, ExcaliburLedMode.Static, brightness, r, g, b);
            }

            // Set both Zone 0 (Master), Zone 6 (ALLKBLED), and individual zones (3, 4, 5) to ensure EC wakes up
            bool res0 = SetLed(ExcaliburLedZone.All, mode, brightness, r, g, b);
            bool res6 = SetLed(ExcaliburLedZone.AllKeyboard, mode, brightness, r, g, b);
            bool res3 = SetLed(ExcaliburLedZone.KeyboardLeft, mode, brightness, r, g, b);
            bool res4 = SetLed(ExcaliburLedZone.KeyboardCenter, mode, brightness, r, g, b);
            bool res5 = SetLed(ExcaliburLedZone.KeyboardRight, mode, brightness, r, g, b);

            return res0 || res6 || res3;
        }

        public bool TurnOffAllLights()
        {
            bool res1 = SetLed(ExcaliburLedZone.AllKeyboard, ExcaliburLedMode.Off, 0, 0, 0, 0);
            bool res2 = SetLed(ExcaliburLedZone.All, ExcaliburLedMode.Off, 0, 0, 0, 0);
            return res1 || res2;
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            try
            {
                _wmiInstance?.Dispose();
            }
            catch
            {
                // Suppress disposal errors
            }
        }

        private static class NvmlHelper
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
}
