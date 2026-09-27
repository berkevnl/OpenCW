using System.Collections.Generic;

namespace OpenCW.Core
{
    public record HardwareCapabilities(
        bool HasRgbKeyboard = true,
        bool HasBrightnessControl = true,
        byte MaxBrightnessLevel = 2,
        bool HasManualFanControl = false,
        int FanCount = 2,
        bool HasGpuModeSwitch = false,
        IReadOnlyList<PowerMode>? SupportedPowerModes = null,
        IReadOnlyList<LedMode>? SupportedLedModes = null
    );
}
