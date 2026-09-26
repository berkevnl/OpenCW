using System.Text.Json.Serialization;
using EHelper.Hardware;

namespace EHelper.Config
{
    public class AppSettings
    {
        public ExcaliburPowerMode PowerMode { get; set; } = ExcaliburPowerMode.Gaming;
        public ExcaliburLedMode LedMode { get; set; } = ExcaliburLedMode.Static;
        public byte LedBrightness { get; set; } = 2;
        public byte Red { get; set; } = 0;
        public byte Green { get; set; } = 180;
        public byte Blue { get; set; } = 255;
        public bool StartWithWindows { get; set; } = false;
        public int PollingIntervalSeconds { get; set; } = 3;
        public bool IsDarkTheme { get; set; } = true;
        public string Language { get; set; } = "TR";

        [JsonIgnore]
        public string HexColor => $"#{Red:X2}{Green:X2}{Blue:X2}";
    }
}
