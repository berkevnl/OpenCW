using System.Runtime.InteropServices;

namespace EHelper.Hardware
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SMI_STRUCT_S
    {
        public ushort a0;   // Command Type: 0xFA00 (Read) / 0xFB00 (Write)
        public ushort a1;   // Subfunction Code (0x0100: LED, 0x0200: CPU/GPU, 0x0205: Fan Speed, 0x0206: Fan Override, 0x0207: SYS Fan, 0x0300: Power)
        public uint a2;     // Arg 1 / Return 1
        public uint a3;     // Arg 2 / Return 2
        public uint a4;     // Arg 3 / Return 3
        public uint a5;     // Arg 4 / Return 4
        public uint a6;     // Arg 5 / Return 5
        public uint rev7a;  // Reserved
        public uint rev7b;  // Reserved

        public void Clear()
        {
            a0 = 0; a1 = 0; a2 = 0; a3 = 0;
            a4 = 0; a5 = 0; a6 = 0; rev7a = 0; rev7b = 0;
        }
    }

    public enum ExcaliburPowerMode : uint
    {
        HighPerformance = 0,
        Gaming = 1,
        Office = 2
    }

    public enum ExcaliburLedMode : byte
    {
        Off = 0,
        Static = 1,
        Breathing = 3,
        ColorfulCycle = 6,
        Rainbow = 7,
        Ambilight = 7
    }

    public enum ExcaliburLedZone : uint
    {
        All = 0,
        KeyboardLeft = 3,
        KeyboardCenter = 4,
        KeyboardRight = 5,
        AllKeyboard = 6,
        Trunk = 7,
        Logo = 8
    }

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
