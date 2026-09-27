using System;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using OpenCW.Core;
using OpenCW.Hardware.Common;

namespace OpenCW.Hardware.Casper
{
    public class ExcaliburBridge : IHardwareProvider
    {
        private const string WmiNamespace = @"root\wmi";
        private const string WmiClassName = "RW_GMWMI";
        
        private readonly object _syncLock = new();
        private ManagementObject? _wmiInstance;
        private bool _isDisposed;

        public string VendorName => "Casper";
        public string DeviceModel { get; private set; }
        public bool IsHardwareConnected { get; private set; }

        public HardwareCapabilities Capabilities { get; } = new(
            HasRgbKeyboard: true,
            HasBrightnessControl: true,
            MaxBrightnessLevel: 2,
            HasManualFanControl: true,
            FanCount: 2,
            HasGpuModeSwitch: false
        );

        public ExcaliburBridge(string detectedModel)
        {
            DeviceModel = detectedModel;
            InitializeWmi();
        }

        private void InitializeWmi()
        {
            for (int attempt = 1; attempt <= 3; attempt++)
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
                    if (IsHardwareConnected)
                    {
                        Debug.WriteLine($"[ExcaliburBridge] WMI connection established on attempt {attempt}");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ExcaliburBridge] WMI attempt {attempt} failed: {ex.Message}");
                    IsHardwareConnected = false;
                }

                if (attempt < 3)
                {
                    System.Threading.Thread.Sleep(300);
                }
            }
        }

        private bool PerformSmi(ref SMI_STRUCT_S smi)
        {
            if (_wmiInstance == null)
            {
                InitializeWmi();
                if (_wmiInstance == null)
                {
                    return false;
                }
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
            smi.a0 = ExcaliburSmiHelper.CMD_READ;
            smi.a1 = ExcaliburSmiHelper.SUB_CPU_GPU_TELEMETRY;

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

            // Fallback for CPU temperature if 0 or outside realistic thermal range
            if (cpuTemp is < 15 or > 115)
            {
                byte fallbackCpu = GetThermalZoneTemperature();
                if (fallbackCpu > 0) cpuTemp = fallbackCpu;
            }

            // GPU Temperature & Fan Metrics:
            // Dedicated NVIDIA GPUs report authoritative, real-time die temperatures via NVML.
            // On Casper Excalibur hardware, the EC register (smi.a3) is typically uncalibrated or
            // hardcoded to a static 30°C (0x1E) dummy value because the GPU die is not wired to the EC ADC.
            var nvGpu = NvmlHelper.GetGpuMetrics();
            if (nvGpu.Temp > 0)
            {
                gpuTemp = (byte)nvGpu.Temp;
            }
            else if (gpuTemp == 30)
            {
                // If NVML returned 0 (e.g. GPU is asleep in D3Cold) and EC returned the known 30°C dummy value,
                // clear it to 0 so the UI displays "--°C" instead of a false static 30°C.
                gpuTemp = 0;
            }

            if (gpuRpm == 0 && nvGpu.FanPercent > 0)
            {
                gpuRpm = (ushort)(nvGpu.FanPercent * 45); 
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

        public bool SetPowerMode(PowerMode mode)
        {
            var smi = new SMI_STRUCT_S();
            smi.Clear();
            smi.a0 = ExcaliburSmiHelper.CMD_WRITE;
            smi.a1 = ExcaliburSmiHelper.SUB_POWER_MODE;
            smi.a2 = (uint)mode;
            return PerformSmi(ref smi);
        }

        public bool SetManualFanControl(bool enabled)
        {
            var smi = new SMI_STRUCT_S();
            smi.Clear();
            smi.a0 = ExcaliburSmiHelper.CMD_WRITE;
            smi.a1 = ExcaliburSmiHelper.SUB_FAN_OVERRIDE;
            smi.a2 = enabled ? 1u : 0u;
            return PerformSmi(ref smi);
        }

        public bool SetFanSpeed(byte cpuPercent, byte gpuPercent, byte sysPercent = 255)
        {
            var smi = new SMI_STRUCT_S();
            smi.Clear();
            smi.a0 = ExcaliburSmiHelper.CMD_WRITE;
            smi.a1 = ExcaliburSmiHelper.SUB_FAN_SPEED;
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

        public bool SetLed(LedZone zone, LedMode mode, byte brightness, byte r, byte g, byte b)
        {
            uint packed = ExcaliburSmiHelper.PackLedData(mode, brightness, r, g, b);
            var smi = new SMI_STRUCT_S();
            smi.Clear();
            smi.a0 = ExcaliburSmiHelper.CMD_WRITE;
            smi.a1 = ExcaliburSmiHelper.SUB_LED;
            smi.a2 = (uint)zone;
            smi.a3 = packed;
            return PerformSmi(ref smi);
        }

        public bool SetAllKeyboardLed(LedMode mode, byte brightness, byte r, byte g, byte b)
        {
            if (mode == LedMode.Off || brightness == 0)
            {
                return TurnOffAllLights();
            }

            // Prime the EC controller PWM if switching into breathing or dynamic modes
            if (mode == LedMode.Breathing || mode == LedMode.ColorfulCycle)
            {
                SetLed(LedZone.All, LedMode.Static, brightness, r, g, b);
                SetLed(LedZone.AllKeyboard, LedMode.Static, brightness, r, g, b);
            }

            // Set Zone 0 (Master), Zone 6 (ALLKBLED), and individual zones (3, 4, 5) to ensure EC wakes up
            bool res0 = SetLed(LedZone.All, mode, brightness, r, g, b);
            bool res6 = SetLed(LedZone.AllKeyboard, mode, brightness, r, g, b);
            bool res3 = SetLed(LedZone.KeyboardLeft, mode, brightness, r, g, b);
            bool res4 = SetLed(LedZone.KeyboardCenter, mode, brightness, r, g, b);
            bool res5 = SetLed(LedZone.KeyboardRight, mode, brightness, r, g, b);

            return res0 || res6 || res3;
        }

        public bool TurnOffAllLights()
        {
            bool res0 = SetLed(LedZone.All, LedMode.Off, 0, 0, 0, 0);
            bool res6 = SetLed(LedZone.AllKeyboard, LedMode.Off, 0, 0, 0, 0);
            bool res3 = SetLed(LedZone.KeyboardLeft, LedMode.Off, 0, 0, 0, 0);
            bool res4 = SetLed(LedZone.KeyboardCenter, LedMode.Off, 0, 0, 0, 0);
            bool res5 = SetLed(LedZone.KeyboardRight, LedMode.Off, 0, 0, 0, 0);
            return res0 || res6 || res3;
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
    }
}
