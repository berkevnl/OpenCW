using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using EHelper.Config;
using EHelper.Hardware;
using EHelper.Services;
using Microsoft.Win32;

using Button = System.Windows.Controls.Button;
using Color = System.Windows.Media.Color;
using MessageBox = System.Windows.MessageBox;

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

            // UI Initial Values
            TxtModelBadge.Text = _bridge.DeviceModel;
            TxtHardwareStatus.Text = _isSimulated 
                ? "Simülasyon Modu (Mock ACPI)" 
                : "WMI ACPI SMI • Bağlı";
            
            if (_isSimulated)
            {
                DotStatus.Fill = (SolidColorBrush)FindResource("WarningOrange");
            }

            SliderBrightness.Value = _brightness;
            TxtBrightnessValue.Text = _brightness.ToString();
            TxtHexColor.Text = s.HexColor;
            UpdateColorPreview();

            ChkAutoStart.IsChecked = IsStartupEnabled();

            UpdatePowerModeButtonsUI();
            UpdateLedModeButtonsUI();

            // Initial push to hardware
            _bridge.SetPowerMode(_currentPowerMode);
            PowerPlanManager.ApplyPowerPlan(_currentPowerMode);
            ApplyLedSettings();
        }

        private void SubscribeTelemetry()
        {
            _monitor.TelemetryUpdated += telemetry =>
            {
                Dispatcher.InvokeAsync(() => UpdateTelemetryUI(telemetry));
            };
        }

        private void UpdateTelemetryUI(HardwareTelemetry t)
        {
            if (!t.IsAvailable)
            {
                TxtCpuTemp.Text = "N/A";
                TxtGpuTemp.Text = "N/A";
                TxtCpuRpm.Text = "0 RPM";
                TxtGpuRpm.Text = "0 RPM";
                return;
            }

            TxtCpuTemp.Text = $"{t.CpuTemperature} °C";
            PbCpuTemp.Value = Math.Min(100, (double)t.CpuTemperature);

            if (t.CpuTemperature > 82)
                TxtCpuTemp.Foreground = (SolidColorBrush)FindResource("DangerRed");
            else if (t.CpuTemperature > 68)
                TxtCpuTemp.Foreground = (SolidColorBrush)FindResource("WarningOrange");
            else
                TxtCpuTemp.Foreground = (SolidColorBrush)FindResource("AccentCyan");

            TxtGpuTemp.Text = $"{t.GpuTemperature} °C";
            PbGpuTemp.Value = Math.Min(100, (double)t.GpuTemperature);

            if (t.GpuTemperature > 80)
                TxtGpuTemp.Foreground = (SolidColorBrush)FindResource("DangerRed");
            else if (t.GpuTemperature > 65)
                TxtGpuTemp.Foreground = (SolidColorBrush)FindResource("WarningOrange");
            else
                TxtGpuTemp.Foreground = (SolidColorBrush)FindResource("AccentPurple");

            TxtCpuRpm.Text = $"{t.CpuFanRpm} RPM";
            TxtGpuRpm.Text = $"{t.GpuFanRpm} RPM";
        }

        #region Flyout & Auto-Hide

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
            }
            else
            {
                PositionBottomRight();
                Show();
                Activate();
                Focus();
            }
        }

        protected override void OnDeactivated(EventArgs e)
        {
            base.OnDeactivated(e);
            // Hide automatically when user clicks anywhere outside the flyout
            Hide();
        }

        private void BtnCloseToTray_Click(object sender, RoutedEventArgs e)
        {
            Hide();
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
            var normalStyle = (Style)FindResource("ModeButtonStyle");
            var activeStyle = (Style)FindResource("ActiveModeButtonStyle");

            BtnModeOffice.Style = _currentPowerMode == ExcaliburPowerMode.Office ? activeStyle : normalStyle;
            BtnModeGaming.Style = _currentPowerMode == ExcaliburPowerMode.Gaming ? activeStyle : normalStyle;
            BtnModeHighPerf.Style = _currentPowerMode == ExcaliburPowerMode.HighPerformance ? activeStyle : normalStyle;
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

        private void BtnLedCycle_Click(object sender, RoutedEventArgs e)
        {
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
            var normalStyle = (Style)FindResource("ModeButtonStyle");
            var activeStyle = (Style)FindResource("ActiveModeButtonStyle");

            BtnLedStatic.Style = _currentLedMode == ExcaliburLedMode.Static ? activeStyle : normalStyle;
            BtnLedBreathing.Style = _currentLedMode == ExcaliburLedMode.Breathing ? activeStyle : normalStyle;
            BtnLedCycle.Style = _currentLedMode == ExcaliburLedMode.ColorfulCycle ? activeStyle : normalStyle;
            BtnLedOff.Style = _currentLedMode == ExcaliburLedMode.Off ? activeStyle : normalStyle;
        }

        private void SliderBrightness_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (TxtBrightnessValue == null) return;
            _brightness = (byte)SliderBrightness.Value;
            TxtBrightnessValue.Text = _brightness.ToString();

            ApplyLedSettings();
        }

        private void ColorPreset_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string hex)
            {
                TxtHexColor.Text = hex;
                ParseAndSetColor(hex);
            }
        }

        private void BtnApplyHex_Click(object sender, RoutedEventArgs e)
        {
            ParseAndSetColor(TxtHexColor.Text.Trim());
        }

        private void ParseAndSetColor(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex)) return;

            string cleanHex = hex.TrimStart('#');
            if (cleanHex.Length == 6 &&
                byte.TryParse(cleanHex[..2], NumberStyles.HexNumber, null, out byte r) &&
                byte.TryParse(cleanHex[2..4], NumberStyles.HexNumber, null, out byte g) &&
                byte.TryParse(cleanHex[4..6], NumberStyles.HexNumber, null, out byte b))
            {
                _red = r;
                _green = g;
                _blue = b;

                UpdateColorPreview();
                ApplyLedSettings();
            }
        }

        private void UpdateColorPreview()
        {
            BrdCurrentColor.Background = new SolidColorBrush(Color.FromRgb(_red, _green, _blue));
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

        #region Extra Actions & Settings

        private void BtnResetFans_Click(object sender, RoutedEventArgs e)
        {
            _bridge.ResetFansToAuto();
            MessageBox.Show(
                "Fan denetimi fabrika BIOS akıllı moduna sıfırlandı.", 
                "E-Helper", 
                MessageBoxButton.OK, 
                MessageBoxImage.Information
            );
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