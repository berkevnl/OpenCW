using System;

namespace OpenCW.Core
{
    public enum PowerMode : uint
    {
        HighPerformance = 0,
        Turbo = 0,
        Gaming = 1,
        Balanced = 1,
        Office = 2,
        Silent = 2
    }

    public enum LedMode : byte
    {
        Off = 0,
        Static = 1,
        Breathing = 3,
        ColorfulCycle = 6,
        Rainbow = 7,
        Ambilight = 7
    }

    public enum LedZone : uint
    {
        All = 0,
        KeyboardLeft = 3,
        KeyboardCenter = 4,
        KeyboardRight = 5,
        AllKeyboard = 6,
        Trunk = 7,
        Logo = 8
    }
}
