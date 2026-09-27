namespace OpenCW.Core
{
    public record HardwareTelemetry(
        byte CpuTemperature,
        byte GpuTemperature,
        ushort CpuFanRpm,
        ushort GpuFanRpm,
        byte SysTemperature = 0,
        ushort SysFanRpm = 0,
        bool IsAvailable = true
    );
}
