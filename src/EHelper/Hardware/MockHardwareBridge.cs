using System;

namespace EHelper.Hardware
{
    public class MockHardwareBridge : IHardwareBridge
    {
        private readonly Random _rand = new();
        private ExcaliburPowerMode _currentPowerMode = ExcaliburPowerMode.Gaming;
        private ExcaliburLedMode _currentLedMode = ExcaliburLedMode.Static;
        private byte _brightness = 3;
        private (byte R, byte G, byte B) _color = (0, 150, 255);

        public bool IsHardwareConnected => true;
        public string DeviceModel => "Casper Excalibur (Simülasyon Modu)";

        public (byte R, byte G, byte B) CurrentColor => _color;
        public byte CurrentBrightness => _brightness;
        public ExcaliburLedMode CurrentLedMode => _currentLedMode;
        public ExcaliburPowerMode CurrentPowerMode => _currentPowerMode;

        public HardwareTelemetry GetTelemetry()
        {
            // Simulate realistic laptop temperatures and fan RPMs based on active power mode
            byte baseCpuTemp = _currentPowerMode switch
            {
                ExcaliburPowerMode.Office => (byte)45,
                ExcaliburPowerMode.Gaming => (byte)68,
                ExcaliburPowerMode.HighPerformance => (byte)78,
                _ => (byte)50
            };

            byte baseGpuTemp = _currentPowerMode switch
            {
                ExcaliburPowerMode.Office => (byte)40,
                ExcaliburPowerMode.Gaming => (byte)62,
                ExcaliburPowerMode.HighPerformance => (byte)74,
                _ => (byte)45
            };

            ushort baseCpuRpm = _currentPowerMode switch
            {
                ExcaliburPowerMode.Office => (ushort)1800,
                ExcaliburPowerMode.Gaming => (ushort)3400,
                ExcaliburPowerMode.HighPerformance => (ushort)4900,
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

        public bool SetPowerMode(ExcaliburPowerMode mode)
        {
            _currentPowerMode = mode;
            return true;
        }

        public bool SetManualFanControl(bool enabled) => true;

        public bool SetFanSpeed(byte cpuPercent, byte gpuPercent, byte sysPercent = 255) => true;

        public void ResetFansToAuto() { }

        public bool SetLed(ExcaliburLedZone zone, ExcaliburLedMode mode, byte brightness, byte r, byte g, byte b)
        {
            _currentLedMode = mode;
            _brightness = brightness;
            _color = (r, g, b);
            return true;
        }

        public bool SetAllKeyboardLed(ExcaliburLedMode mode, byte brightness, byte r, byte g, byte b)
        {
            return SetLed(ExcaliburLedZone.AllKeyboard, mode, brightness, r, g, b);
        }

        public bool TurnOffAllLights()
        {
            return SetLed(ExcaliburLedZone.All, ExcaliburLedMode.Off, 0, 0, 0, 0);
        }

        public void Dispose() { }
    }
}
