using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using EHelper.Config;
using EHelper.Hardware;
using EHelper.Services;
using Microsoft.Win32;

using Button = System.Windows.Controls.Button;
using Color = System.Windows.Media.Color;
using MessageBox = System.Windows.MessageBox;
using Point = System.Windows.Point;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace EHelper
{
    public partial class MainWindow : Window
    {
        private readonly IHardwareBridge _bridge;
        private readonly bool _isSimulated;
        private readonly ConfigManager _config;
        private readonly HardwareMonitorService _monitor;

        private ExcaliburPowerMode _currentPowerMode;
        private ExcaliburLedMode _currentLedMode;
        private byte _brightness;
        private byte _red;
        private byte _green;
        private byte _blue;
        private bool _isDarkTheme;

        public MainWindow(IHardwareBridge bridge, bool isSimulated, ConfigManager config, HardwareMonitorService monitor)
        {
            InitializeComponent();

            _bridge = bridge;
            _isSimulated = isSimulated;
            _config = config;
            _monitor = monitor;

            ApplyInitialState();
            SubscribeTelemetry();
        }

        private void ApplyInitialState()
        {
            var s = _config.CurrentSettings;
            _currentPowerMode = s.PowerMode;
            _currentLedMode = s.LedMode;
            _brightness = s.LedBrightness;
            _red = s.Red;
            _green = s.Green;
            _blue = s.Blue;
            _isDarkTheme = s.IsDarkTheme;

            // Apply Theme
            ApplyTheme(_isDarkTheme);

            // UI Initial Values
            TxtModelBadge.Text = _bridge.DeviceModel;
            TxtHardwareStatus.Text = _isSimulated 
                ? "Simülasyon Modu" 
                : "WMI ACPI • Aktif";
            
            if (_isSimulated)
            {
                DotStatus.Fill = new SolidColorBrush(Color.FromRgb(245, 158, 11)); // Amber
            }

            SliderBrightness.Value = _brightness;
            TxtBrightnessValue.Text = $"{_brightness}/4";
            UpdateColorPreview();

            ChkAutoStart.IsChecked = IsStartupEnabled();

            UpdatePowerModeButtonsUI();
            UpdateLedModeButtonsUI();

            // Initial push to hardware
            _bridge.SetPowerMode(_currentPowerMode);
            PowerPlanManager.ApplyPowerPlan(_currentPowerMode);
            ApplyLedSettings();

            // Initial System Metrics query
            UpdateSystemResourcesUI();
        }

        private void SubscribeTelemetry()
        {
            _monitor.TelemetryUpdated += telemetry =>
            {
                Dispatcher.InvokeAsync(() =>
                {
                    UpdateTelemetryUI(telemetry);

                    // Only refresh RAM/SSD metrics when the user is actively viewing the panel
                    if (IsVisible)
                    {
                        UpdateSystemResourcesUI();
                    }
                });
            };
        }

        private void UpdateTelemetryUI(HardwareTelemetry t)
        {
            if (!t.IsAvailable)
            {
                TxtCpuTemp.Text = "--°C";
                TxtGpuTemp.Text = "--°C";
                TxtCpuRpm.Text = "--RPM";
                TxtGpuRpm.Text = "--RPM";
                return;
            }

            TxtCpuTemp.Text = $"{t.CpuTemperature}°C";
            PbCpuTemp.Value = Math.Min(100, (double)t.CpuTemperature);

            TxtGpuTemp.Text = $"{t.GpuTemperature}°C";
            PbGpuTemp.Value = Math.Min(100, (double)t.GpuTemperature);

            TxtCpuRpm.Text = $"{t.CpuFanRpm}RPM";
            TxtGpuRpm.Text = $"{t.GpuFanRpm}RPM";
        }

        private void UpdateSystemResourcesUI()
        {
            try
            {
                var metrics = SystemResourceMonitor.GetMetrics();

                TxtRamPercent.Text = $"{metrics.RamPercent}%";
                TxtRamUsage.Text = $"{metrics.RamUsedGb:F1} / {metrics.RamTotalGb:F1} GB";
                PbRamUsage.Value = metrics.RamPercent;

                TxtSsdPercent.Text = $"{metrics.SsdPercent}%";
                TxtSsdUsage.Text = $"{metrics.SsdUsedGb:F0} / {metrics.SsdTotalGb:F0} GB";
                PbSsdUsage.Value = metrics.SsdPercent;
            }
            catch { }
        }

        #region Flyout & Auto-Hide & Memory Trim

        public void PositionBottomRight()
        {
            var workArea = SystemParameters.WorkArea;
            Left = workArea.Right - Width - 8;
            Top = workArea.Bottom - Height - 8;
        }

        public void ToggleFlyout()
        {
            if (IsVisible)
            {
                Hide();
                MemoryOptimizer.TrimMemory();
            }
            else
            {
                PositionBottomRight();
                UpdateSystemResourcesUI();
                Show();
                Activate();
                Focus();
            }
        }

        protected override void OnDeactivated(EventArgs e)
        {
            base.OnDeactivated(e);
            Hide();
            MemoryOptimizer.TrimMemory();
        }

        private void BtnCloseToTray_Click(object sender, RoutedEventArgs e)
        {
            Hide();
            MemoryOptimizer.TrimMemory();
        }

        #endregion

        #region Power Modes

        private void BtnModeOffice_Click(object sender, RoutedEventArgs e)
        {
            SetPowerMode(ExcaliburPowerMode.Office);
        }

        private void BtnModeGaming_Click(object sender, RoutedEventArgs e)
        {
            SetPowerMode(ExcaliburPowerMode.Gaming);
        }

        private void BtnModeHighPerf_Click(object sender, RoutedEventArgs e)
        {
            SetPowerMode(ExcaliburPowerMode.HighPerformance);
        }

        private void SetPowerMode(ExcaliburPowerMode mode)
        {
            _currentPowerMode = mode;
            UpdatePowerModeButtonsUI();

            _bridge.SetPowerMode(mode);
            PowerPlanManager.ApplyPowerPlan(mode);

            _config.CurrentSettings.PowerMode = mode;
            _config.SaveSettings();
        }

        private void UpdatePowerModeButtonsUI()
        {
            var normalStyle = (Style)FindResource("GHelperTileStyle");
            var activeStyle = (Style)FindResource("ActiveGHelperTileStyle");

            BtnModeOffice.Style = _currentPowerMode == ExcaliburPowerMode.Office ? activeStyle : normalStyle;
            BtnModeGaming.Style = _currentPowerMode == ExcaliburPowerMode.Gaming ? activeStyle : normalStyle;
            BtnModeHighPerf.Style = _currentPowerMode == ExcaliburPowerMode.HighPerformance ? activeStyle : normalStyle;

            TxtPowerModeTitle.Text = _currentPowerMode switch
            {
                ExcaliburPowerMode.Office => "Performans Modu: Sessiz",
                ExcaliburPowerMode.Gaming => "Performans Modu: Dengeli",
                ExcaliburPowerMode.HighPerformance => "Performans Modu: Turbo",
                _ => "Performans Modu"
            };
        }

        #endregion

        #region RGB Keyboard Control

        private void BtnLedStatic_Click(object sender, RoutedEventArgs e)
        {
            SetLedMode(ExcaliburLedMode.Static);
        }

        private void BtnLedBreathing_Click(object sender, RoutedEventArgs e)
        {
            SetLedMode(ExcaliburLedMode.Breathing);
        }

        private void BtnLedDynamic_Click(object sender, RoutedEventArgs e)
        {
            // Orijinal Excalibur Dinamik Işık modu (Mode 6 - Colorful Dynamic Cycle)
            SetLedMode(ExcaliburLedMode.ColorfulCycle);
        }

        private void BtnLedOff_Click(object sender, RoutedEventArgs e)
        {
            SetLedMode(ExcaliburLedMode.Off);
        }

        private void SetLedMode(ExcaliburLedMode mode)
        {
            _currentLedMode = mode;
            UpdateLedModeButtonsUI();
            ApplyLedSettings();
        }

        private void UpdateLedModeButtonsUI()
        {
            var normalStyle = (Style)FindResource("GHelperTileStyle");
            var activeStyle = (Style)FindResource("ActiveGHelperTileStyle");

            BtnLedStatic.Style = _currentLedMode == ExcaliburLedMode.Static ? activeStyle : normalStyle;
            BtnLedBreathing.Style = _currentLedMode == ExcaliburLedMode.Breathing ? activeStyle : normalStyle;
            BtnLedDynamic.Style = (_currentLedMode == ExcaliburLedMode.ColorfulCycle || _currentLedMode == ExcaliburLedMode.Rainbow) ? activeStyle : normalStyle;
            BtnLedOff.Style = _currentLedMode == ExcaliburLedMode.Off ? activeStyle : normalStyle;

            // Palette and preset colors only apply to Static and Breathing modes
            bool isManualColor = _currentLedMode == ExcaliburLedMode.Static || _currentLedMode == ExcaliburLedMode.Breathing;
            PnlColorSection.Visibility = isManualColor ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SliderBrightness_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (TxtBrightnessValue == null) return;
            _brightness = (byte)SliderBrightness.Value;
            TxtBrightnessValue.Text = $"{_brightness}/4";

            ApplyLedSettings();
        }

        private void Spectrum_MouseDown(object sender, MouseButtonEventArgs e)
        {
            PickColorFromSpectrum(e.GetPosition(BrdSpectrum));
        }

        private void Spectrum_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                PickColorFromSpectrum(e.GetPosition(BrdSpectrum));
            }
        }

        private void PickColorFromSpectrum(Point pos)
        {
            double width = BrdSpectrum.ActualWidth;
            if (width <= 0) return;

            double fraction = Math.Clamp(pos.X / width, 0.0, 1.0);
            double hue = fraction * 360.0;
            var (r, g, b) = HsvToRgb(hue, 1.0, 1.0);
            SetRgbColor(r, g, b);
        }

        private static (byte R, byte G, byte B) HsvToRgb(double h, double s, double v)
        {
            double c = v * s;
            double x = c * (1 - Math.Abs((h / 60.0) % 2 - 1));
            double m = v - c;

            double r = 0, g = 0, b = 0;
            if (h < 60) { r = c; g = x; b = 0; }
            else if (h < 120) { r = x; g = c; b = 0; }
            else if (h < 180) { r = 0; g = c; b = x; }
            else if (h < 240) { r = 0; g = x; b = 0; }
            else if (h < 300) { r = x; g = 0; b = c; }
            else { r = c; g = 0; b = x; }

            return ((byte)((r + m) * 255), (byte)((g + m) * 255), (byte)((b + m) * 255));
        }

        private void ColorPreset_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string hex)
            {
                string cleanHex = hex.TrimStart('#');
                if (cleanHex.Length == 6 &&
                    byte.TryParse(cleanHex[..2], NumberStyles.HexNumber, null, out byte r) &&
                    byte.TryParse(cleanHex[2..4], NumberStyles.HexNumber, null, out byte g) &&
                    byte.TryParse(cleanHex[4..6], NumberStyles.HexNumber, null, out byte b))
                {
                    SetRgbColor(r, g, b);
                }
            }
        }

        private void SetRgbColor(byte r, byte g, byte b)
        {
            _red = r;
            _green = g;
            _blue = b;

            UpdateColorPreview();
            ApplyLedSettings();
        }

        private void UpdateColorPreview()
        {
            var brush = new SolidColorBrush(Color.FromRgb(_red, _green, _blue));
            if (BrdCurrentColor != null)
            {
                BrdCurrentColor.Background = brush;
            }
        }

        private void ApplyLedSettings()
        {
            if (_currentLedMode == ExcaliburLedMode.Off)
            {
                _bridge.TurnOffAllLights();
            }
            else
            {
                _bridge.SetAllKeyboardLed(_currentLedMode, _brightness, _red, _green, _blue);
            }

            _config.CurrentSettings.LedMode = _currentLedMode;
            _config.CurrentSettings.LedBrightness = _brightness;
            _config.CurrentSettings.Red = _red;
            _config.CurrentSettings.Green = _green;
            _config.CurrentSettings.Blue = _blue;
            _config.SaveSettings();
        }

        #endregion

        #region Theme Switcher (G-Helper Dark / Clean Light)

        private void BtnThemeToggle_Click(object sender, RoutedEventArgs e)
        {
            _isDarkTheme = !_isDarkTheme;
            ApplyTheme(_isDarkTheme);
            _config.CurrentSettings.IsDarkTheme = _isDarkTheme;
            _config.SaveSettings();
        }

        private void ApplyTheme(bool isDark)
        {
            if (isDark)
            {
                // Authentic G-Helper Matte Dark
                Resources["WindowBg"] = new SolidColorBrush(Color.FromRgb(22, 22, 22));        // #161616
                Resources["TileBg"] = new SolidColorBrush(Color.FromRgb(37, 37, 37));          // #252525
                Resources["TileHoverBg"] = new SolidColorBrush(Color.FromRgb(50, 50, 50));     // #323232
                Resources["TileBorder"] = new SolidColorBrush(Color.FromRgb(56, 56, 56));      // #383838
                Resources["TileActiveBg"] = new SolidColorBrush(Color.FromRgb(25, 39, 56));    // #192738
                Resources["TileActiveBorder"] = new SolidColorBrush(Color.FromRgb(0, 144, 255)); // #0090FF
                Resources["TextPrimary"] = new SolidColorBrush(Color.FromRgb(255, 255, 255));  // #FFFFFF
                Resources["TextSecondary"] = new SolidColorBrush(Color.FromRgb(204, 204, 204));// #CCCCCC
                Resources["TextMuted"] = new SolidColorBrush(Color.FromRgb(142, 142, 147));    // #8E8E93
                Resources["ProgressTrack"] = new SolidColorBrush(Color.FromRgb(42, 42, 42));   // #2A2A2A
                BtnThemeToggle.Content = "☀️ Açık";
            }
            else
            {
                // Clean High-Contrast Light
                Resources["WindowBg"] = new SolidColorBrush(Color.FromRgb(242, 242, 247));     // #F2F2F7
                Resources["TileBg"] = new SolidColorBrush(Color.FromRgb(255, 255, 255));       // #FFFFFF
                Resources["TileHoverBg"] = new SolidColorBrush(Color.FromRgb(235, 235, 240));  // #EBEBF0
                Resources["TileBorder"] = new SolidColorBrush(Color.FromRgb(209, 209, 214));   // #D1D1D6
                Resources["TileActiveBg"] = new SolidColorBrush(Color.FromRgb(225, 239, 255)); // #E1EFFF
                Resources["TileActiveBorder"] = new SolidColorBrush(Color.FromRgb(0, 122, 255)); // #007AFF
                Resources["TextPrimary"] = new SolidColorBrush(Color.FromRgb(0, 0, 0));        // #000000
                Resources["TextSecondary"] = new SolidColorBrush(Color.FromRgb(60, 60, 67));   // #3C3C43
                Resources["TextMuted"] = new SolidColorBrush(Color.FromRgb(142, 142, 147));    // #8E8E93
                Resources["ProgressTrack"] = new SolidColorBrush(Color.FromRgb(229, 229, 234));
                BtnThemeToggle.Content = "🌙 Koyu";
            }
        }

        #endregion

        #region Extra Actions & Settings

        private void BtnRefreshTelemetry_Click(object sender, RoutedEventArgs e)
        {
            var telemetry = _bridge.GetTelemetry();
            UpdateTelemetryUI(telemetry);
            UpdateSystemResourcesUI();
        }

        private void ChkAutoStart_Changed(object sender, RoutedEventArgs e)
        {
            bool enable = ChkAutoStart.IsChecked == true;
            SetStartup(enable);
            _config.CurrentSettings.StartWithWindows = enable;
            _config.SaveSettings();
        }

        private const string RunRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "EHelper";

        private static bool IsStartupEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, false);
                return key?.GetValue(AppName) != null;
            }
            catch
            {
                return false;
            }
        }

        private static void SetStartup(bool enable)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, true);
                if (key == null) return;

                if (enable)
                {
                    string? exePath = Environment.ProcessPath;
                    if (!string.IsNullOrEmpty(exePath))
                    {
                        key.SetValue(AppName, $"\"{exePath}\"");
                    }
                }
                else
                {
                    key.DeleteValue(AppName, false);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Startup] Failed to configure registry: {ex.Message}");
            }
        }

        private void BtnExitApp_Click(object sender, RoutedEventArgs e)
        {
            if (System.Windows.Application.Current is App app)
            {
                app.ShutdownApp();
            }
            else
            {
                System.Windows.Application.Current.Shutdown();
            }
        }

        #endregion
    }
}