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

                _wmiClass = new ManagementClass(scope, new ManagementPath(WmiClassName), null);

                // Find active instance of RW_GMWMI
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

                    if (_wmiInstance != null)
                    {
                        _wmiInstance["BufferBytes"] = inBuffer;
                        _wmiInstance.Put(); // Commit to WMI

                        if (_wmiInstance["BufferBytes"] is byte[] outBuffer && outBuffer.Length >= size)
                        {
                            Marshal.Copy(outBuffer, 0, ptr, size);
                            smi = Marshal.PtrToStructure<SMI_STRUCT_S>(ptr);
                            return true;
                        }
                    }
                    else if (_wmiClass != null)
                    {
                        _wmiClass["BufferBytes"] = inBuffer;
                        if (_wmiClass["BufferBytes"] is byte[] outBuffer && outBuffer.Length >= size)
                        {
                            Marshal.Copy(outBuffer, 0, ptr, size);
                            smi = Marshal.PtrToStructure<SMI_STRUCT_S>(ptr);
                            return true;
                        }
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

            if (PerformSmi(ref smi))
            {
                byte cpuTemp = (byte)smi.a2;
                byte gpuTemp = (byte)smi.a3;
                ushort cpuRpm = (ushort)smi.a4;
                ushort gpuRpm = (ushort)smi.a5;

                // Query SYS / 3rd fan if available
                var sysSmi = new SMI_STRUCT_S();
                sysSmi.Clear();
                sysSmi.a0 = 0xFA00;
                sysSmi.a1 = 0x0207;

                byte sysTemp = 0;
                ushort sysRpm = 0;
                if (PerformSmi(ref sysSmi))
                {
                    sysTemp = (byte)sysSmi.a2;
                    sysRpm = (ushort)sysSmi.a3;
                }

                return new HardwareTelemetry(
                    CpuTemperature: cpuTemp,
                    GpuTemperature: gpuTemp,
                    CpuFanRpm: cpuRpm,
                    GpuFanRpm: gpuRpm,
                    SysTemperature: sysTemp,
                    SysFanRpm: sysRpm,
                    IsAvailable: true
                );
            }

            return new HardwareTelemetry(0, 0, 0, 0, 0, 0, false);
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
    }
}
