using System;
using OpenCW.Core;

namespace OpenCW.Hardware.Simulation
{
    public class MockHardwareProvider : IHardwareProvider
    {
        private readonly Random _rand = new();
        private PowerMode _currentPowerMode = PowerMode.Gaming;
        private LedMode _currentLedMode = LedMode.Static;
        private byte _brightness = 2;
        private (byte R, byte G, byte B) _color = (0, 150, 255);

        public string VendorName { get; }
        public string DeviceModel { get; }
        public bool IsHardwareConnected => true;

        public MockHardwareProvider(string? vendor = null, string? deviceModel = null)
        {
            VendorName = vendor ?? "Simülasyon";
            DeviceModel = !string.IsNullOrWhiteSpace(deviceModel)
                ? $"{deviceModel} (Simülasyon Modu)"
                : "Evrensel Model (Simülasyon Modu)";
        }

        public HardwareCapabilities Capabilities { get; } = new(
            HasRgbKeyboard: true,
            HasBrightnessControl: true,
            MaxBrightnessLevel: 2,
            HasManualFanControl: true,
            FanCount: 2,
            HasGpuModeSwitch: false
        );

        public (byte R, byte G, byte B) CurrentColor => _color;
        public byte CurrentBrightness => _brightness;
        public LedMode CurrentLedMode => _currentLedMode;
        public PowerMode CurrentPowerMode => _currentPowerMode;

        public HardwareTelemetry GetTelemetry()
        {
            byte baseCpuTemp = _currentPowerMode switch
            {
                PowerMode.Office => (byte)45,
                PowerMode.Gaming => (byte)68,
                PowerMode.HighPerformance => (byte)78,
                _ => (byte)50
            };

            byte baseGpuTemp = _currentPowerMode switch
            {
                PowerMode.Office => (byte)40,
                PowerMode.Gaming => (byte)62,
                PowerMode.HighPerformance => (byte)74,
                _ => (byte)45
            };

            ushort baseCpuRpm = _currentPowerMode switch
            {
                PowerMode.Office => (ushort)1800,
                PowerMode.Gaming => (ushort)3400,
                PowerMode.HighPerformance => (ushort)4900,
                _ => (ushort)2500
            };

            byte cpuTemp = (byte)(baseCpuTemp + _rand.Next(-2, 3));
            byte gpuTemp = (byte)(baseGpuTemp + _rand.Next(-1, 3));
            ushort cpuRpm = (ushort)(baseCpuRpm + _rand.Next(-80, 80));
            ushort gpuRpm = (ushort)(baseCpuRpm - 150 + _rand.Next(-60, 60));

            return new HardwareTelemetry(
                CpuTemperature: cpuTemp,
                GpuTemperature: gpuTemp,
                CpuFanRpm: cpuRpm,
                GpuFanRpm: gpuRpm,
                SysTemperature: 38,
                SysFanRpm: 0,
                IsAvailable: true
            );
        }

        public bool SetPowerMode(PowerMode mode)
        {
            _currentPowerMode = mode;
            return true;
        }

        public bool SetManualFanControl(bool enabled) => true;

        public bool SetFanSpeed(byte cpuPercent, byte gpuPercent, byte sysPercent = 255) => true;

        public void ResetFansToAuto() { }

        public bool SetLed(LedZone zone, LedMode mode, byte brightness, byte r, byte g, byte b)
        {
            _currentLedMode = mode;
            _brightness = brightness;
            _color = (r, g, b);
            return true;
        }

        public bool SetAllKeyboardLed(LedMode mode, byte brightness, byte r, byte g, byte b)
        {
            return SetLed(LedZone.AllKeyboard, mode, brightness, r, g, b);
        }

        public bool TurnOffAllLights()
        {
            return SetLed(LedZone.All, LedMode.Off, 0, 0, 0, 0);
        }

        public void Dispose() { }
    }
}
