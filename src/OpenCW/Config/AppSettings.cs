using System.Text.Json.Serialization;
using OpenCW.Core;

namespace OpenCW.Config
{
    public class AppSettings
    {
        public PowerMode PowerMode { get; set; } = PowerMode.Gaming;
        public LedMode LedMode { get; set; } = LedMode.Static;
        public byte LedBrightness { get; set; } = 2;
        public byte Red { get; set; } = 0;
        public byte Green { get; set; } = 180;
        public byte Blue { get; set; } = 255;
        public bool StartWithWindows { get; set; } = true;
        public int PollingIntervalSeconds { get; set; } = 3;
        public bool IsDarkTheme { get; set; } = true;
        public string Language { get; set; } = "TR";

        [JsonIgnore]
        public string HexColor => $"#{Red:X2}{Green:X2}{Blue:X2}";
    }
}
