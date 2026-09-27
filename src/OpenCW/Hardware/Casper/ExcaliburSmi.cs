using System.Runtime.InteropServices;
using OpenCW.Core;

namespace OpenCW.Hardware.Casper
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

    public static class ExcaliburSmiHelper
    {
        public const ushort CMD_READ = 0xFA00;
        public const ushort CMD_WRITE = 0xFB00;

        public const ushort SUB_LED = 0x0100;
        public const ushort SUB_CPU_GPU_TELEMETRY = 0x0200;
        public const ushort SUB_FAN_SPEED = 0x0205;
        public const ushort SUB_FAN_OVERRIDE = 0x0206;
        public const ushort SUB_SYS_FAN_TELEMETRY = 0x0207;
        public const ushort SUB_POWER_MODE = 0x0300;

        public static uint PackLedData(LedMode mode, byte brightness, byte r, byte g, byte b)
        {
            // Format: [Mode (4-bit)] [Brightness (4-bit)] [Red (8-bit)] [Green (8-bit)] [Blue (8-bit)]
            uint modeAndAlpha = (uint)(((byte)mode << 4) | (brightness & 0x0F));
            uint rgb = (uint)((r << 16) | (g << 8) | b);
            return (modeAndAlpha << 24) | rgb;
        }
    }
}
