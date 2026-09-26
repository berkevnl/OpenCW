using System;

namespace EHelper.Hardware
{
    public interface IHardwareBridge : IDisposable
    {
        bool IsHardwareConnected { get; }
        string DeviceModel { get; }
        
        HardwareTelemetry GetTelemetry();
        bool SetPowerMode(ExcaliburPowerMode mode);
        bool SetManualFanControl(bool enabled);
        bool SetFanSpeed(byte cpuPercent, byte gpuPercent, byte sysPercent = 255);
        void ResetFansToAuto();
        bool SetLed(ExcaliburLedZone zone, ExcaliburLedMode mode, byte brightness, byte r, byte g, byte b);
        bool SetAllKeyboardLed(ExcaliburLedMode mode, byte brightness, byte r, byte g, byte b);
        bool TurnOffAllLights();
    }
}
