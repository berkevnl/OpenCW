using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using OpenCW.Config;
using OpenCW.Core;
using OpenCW.Hardware.Casper;
using OpenCW.Hardware.Simulation;
using OpenCW.Services;
using Microsoft.Win32;

using Button = System.Windows.Controls.Button;
using Color = System.Windows.Media.Color;
using MessageBox = System.Windows.MessageBox;
using Point = System.Windows.Point;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace OpenCW
{
    public partial class MainWindow : Window
    {
        private readonly IHardwareProvider _bridge;
        private readonly bool _isSimulated;
        private readonly ConfigManager _config;
        private readonly HardwareMonitorService _monitor;

        private PowerMode _currentPowerMode;
        private LedMode _currentLedMode;
        private LedMode _lastActiveLedMode = LedMode.Static;
        private byte _brightness;
        private byte _red;
        private byte _green;
        private byte _blue;
        private bool _isDarkTheme;

        private bool _isInitialized;

        public MainWindow(IHardwareProvider bridge, bool isSimulated, ConfigManager config, HardwareMonitorService monitor)
        {
            _bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
            _isSimulated = isSimulated;
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));

            InitializeComponent();

            _isInitialized = false;
            ApplyInitialState();
            _isInitialized = true;
            SubscribeTelemetry();
        }

        private void ApplyInitialState()
        {
            var s = _config.CurrentSettings;
            _currentPowerMode = s.PowerMode;
            _currentLedMode = s.LedMode;
            _brightness = Math.Min(s.LedBrightness, (byte)2);
            _red = s.Red;
            _green = s.Green;
            _blue = s.Blue;
            _isDarkTheme = s.IsDarkTheme;

            if (_currentLedMode == LedMode.Off)
            {
                _currentLedMode = LedMode.Static;
                _brightness = 0;
            }
            else
            {
                _lastActiveLedMode = _currentLedMode;
            }

            // Load Language
            LocalizationManager.CurrentLanguage = s.Language ?? "TR";

            // Apply Theme & Localization
            ApplyTheme(_isDarkTheme);
            ApplyLocalization();

            // UI Initial Values
            TxtModelBadge.Text = _bridge.DeviceModel;
            if (TxtVersionInfo != null)
            {
                TxtVersionInfo.Text = $"v{UpdateService.CurrentVersion}";
            }

            SliderBrightness.Value = _brightness;
            UpdateBrightnessUI();
            UpdateColorPreview();

            // Synchronize Windows Startup state (Task Scheduler + Run key)
            StartupManager.EnsureStartupSynchronized(s.StartWithWindows);
            ChkAutoStart.IsChecked = StartupManager.IsStartupEnabled();

            UpdatePowerModeButtonsUI();
            UpdateLedModeButtonsUI();

            // Initial push to hardware
            _bridge.SetPowerMode(_currentPowerMode);
            ExcaliburPowerPlan.ApplyPowerPlan(_currentPowerMode);
            ApplyLedSettings();

            // Gaming laptop EC / BIOS post-boot synchronization:
            // Laptop EC firmware and ACPI drivers often perform a reset 1-3 seconds after Windows logon.
            // Re-applying the user's saved lighting and power mode after short delays ensures
            // user settings are preserved across reboots and shutdowns.
            System.Threading.Tasks.Task.Run(async () =>
            {
                try
                {
                    await System.Threading.Tasks.Task.Delay(1500);
                    await Dispatcher.InvokeAsync(() =>
                    {
                        _bridge.SetPowerMode(_currentPowerMode);
                        ExcaliburPowerPlan.ApplyPowerPlan(_currentPowerMode);
                        ApplyLedSettings();
                    });

                    await System.Threading.Tasks.Task.Delay(3500);
                    await Dispatcher.InvokeAsync(() =>
                    {
                        ApplyLedSettings();
                    });
                }
                catch
                {
                    // Ignore background async re-apply exceptions
                }
            });

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

            TxtCpuTemp.Text = t.CpuTemperature > 0 ? $"{t.CpuTemperature}°C" : "--°C";
            TxtGpuTemp.Text = t.GpuTemperature > 0 ? $"{t.GpuTemperature}°C" : "--°C";

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

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
        }

        protected override void OnDeactivated(EventArgs e)
        {
            base.OnDeactivated(e);
            // Çarpıya basılmadığı sürece pencerenin kapanmaması için OnDeactivated otomatik gizleme kaldırıldı
        }

        private void BtnCloseToTray_Click(object sender, RoutedEventArgs e)
        {
            Hide();
            MemoryOptimizer.TrimMemory();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        #endregion

        #region Power Modes

        private void BtnModeOffice_Click(object sender, RoutedEventArgs e)
        {
            SetPowerMode(PowerMode.Office);
        }

        private void BtnModeGaming_Click(object sender, RoutedEventArgs e)
        {
            SetPowerMode(PowerMode.Gaming);
        }

        private void BtnModeHighPerf_Click(object sender, RoutedEventArgs e)
        {
            SetPowerMode(PowerMode.HighPerformance);
        }

        private void SetPowerMode(PowerMode mode)
        {
            _currentPowerMode = mode;
            UpdatePowerModeButtonsUI();

            _bridge.SetPowerMode(mode);
            ExcaliburPowerPlan.ApplyPowerPlan(mode);

            _config.CurrentSettings.PowerMode = mode;
            _config.SaveSettings();
        }

        private void UpdatePowerModeButtonsUI()
        {
            var normalStyle = TryFindResource("GHelperTileStyle") as Style;
            var activeStyle = TryFindResource("ActiveGHelperTileStyle") as Style;

            if (BtnModeOffice != null && normalStyle != null && activeStyle != null)
            {
                BtnModeOffice.Style = _currentPowerMode == PowerMode.Office ? activeStyle : normalStyle;
                BtnModeGaming.Style = _currentPowerMode == PowerMode.Gaming ? activeStyle : normalStyle;
                BtnModeHighPerf.Style = _currentPowerMode == PowerMode.HighPerformance ? activeStyle : normalStyle;
            }

            if (TxtPowerModeTitle != null)
            {
                TxtPowerModeTitle.Text = LocalizationManager.GetPowerModeTitle(_currentPowerMode);
            }
        }

        #endregion

        #region RGB Keyboard Control

        private void BtnLedStatic_Click(object sender, RoutedEventArgs e)
        {
            SetLedMode(LedMode.Static);
        }

        private void BtnLedBreathing_Click(object sender, RoutedEventArgs e)
        {
            SetLedMode(LedMode.Breathing);
        }

        private void BtnLedDynamic_Click(object sender, RoutedEventArgs e)
        {
            // Orijinal Excalibur Dinamik Işık modu (Mode 6 - Colorful Dynamic Cycle)
            SetLedMode(LedMode.ColorfulCycle);
        }

        private void BtnLedRainbow_Click(object sender, RoutedEventArgs e)
        {
            // Yazılımsal yumuşak ve canlı Gökkuşağı dalgası (Linear Left-to-Right Rainbow Wave)
            SetLedMode(LedMode.Rainbow);
        }

        private void SetLedMode(LedMode mode)
        {
            _currentLedMode = mode;
            _lastActiveLedMode = _currentLedMode;

            // Aydınlatma 0 ise kullanıcı mod seçtiğinde otomatik olarak %100 seviyesine (2) aç
            if (_brightness == 0)
            {
                _brightness = 2;
                if (SliderBrightness != null && SliderBrightness.Value != 2)
                {
                    SliderBrightness.Value = 2;
                }
                UpdateBrightnessUI();
            }

            UpdateLedModeButtonsUI();
            ApplyLedSettings();
        }

        private void UpdateLedModeButtonsUI()
        {
            var normalStyle = TryFindResource("GHelperTileStyle") as Style;
            var activeStyle = TryFindResource("ActiveGHelperTileStyle") as Style;

            if (BtnLedStatic != null && normalStyle != null && activeStyle != null)
            {
                BtnLedStatic.Style = _currentLedMode == LedMode.Static ? activeStyle : normalStyle;
                BtnLedBreathing.Style = _currentLedMode == LedMode.Breathing ? activeStyle : normalStyle;
                BtnLedDynamic.Style = _currentLedMode == LedMode.ColorfulCycle ? activeStyle : normalStyle;
                BtnLedRainbow.Style = _currentLedMode == LedMode.Rainbow ? activeStyle : normalStyle;
            }

            // Özel renk seçimi yalnızca Sabit ve Nefes modlarında geçerlidir
            if (PnlColorSection != null)
            {
                bool isManualColor = _currentLedMode == LedMode.Static || _currentLedMode == LedMode.Breathing;
                PnlColorSection.Visibility = isManualColor ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void UpdateBrightnessUI()
        {
            string symbol;
            string text;

            switch (_brightness)
            {
                case 0:
                    symbol = "🌑";
                    text = LocalizationManager.IsTurkish ? "Kapalı (%0)" : "Off (0%)";
                    break;
                case 1:
                    symbol = "🔅";
                    text = "%50";
                    break;
                case 2:
                default:
                    symbol = "🔆";
                    text = "%100";
                    break;
            }

            if (TxtBrightnessIcon != null)
            {
                TxtBrightnessIcon.Text = symbol;
                TxtBrightnessIcon.ToolTip = text;
            }

            if (SliderBrightness != null)
            {
                SliderBrightness.ToolTip = text;
            }
        }

        private void SliderBrightness_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isInitialized || SliderBrightness == null) return;
            byte val = (byte)Math.Clamp(SliderBrightness.Value, 0, 2);

            _brightness = val;
            UpdateBrightnessUI();
            ApplyLedSettings();
        }

        private void BtnCustomColor_Click(object sender, RoutedEventArgs e)
        {
            var picker = new UI.ColorPickerWindow(_red, _green, _blue)
            {
                Owner = this
            };

            // Donanım WMI haberleşmesini arka plan iş parçacığına taşıyıp arayüzün (UI) 0ms kasmadan çalışmasını sağlıyoruz
            (byte R, byte G, byte B)? pendingColor = null;
            int isSendingHardware = 0;

            void ScheduleHardwarePreview((byte R, byte G, byte B) rgb)
            {
                pendingColor = rgb;
                if (Interlocked.CompareExchange(ref isSendingHardware, 1, 0) == 0)
                {
                    System.Threading.Tasks.Task.Run(async () =>
                    {
                        try
                        {
                            while (pendingColor.HasValue)
                            {
                                var current = pendingColor.Value;
                                pendingColor = null;

                                if (_brightness > 0 && (_currentLedMode == LedMode.Static || _currentLedMode == LedMode.Breathing))
                                {
                                    _bridge.SetAllKeyboardLed(_currentLedMode, _brightness, current.R, current.G, current.B);
                                }

                                // Donanım veri yolunu (EC SMI) tıkamamak için kısa bekleme
                                await System.Threading.Tasks.Task.Delay(60);
                            }
                        }
                        catch { }
                        finally
                        {
                            Interlocked.Exchange(ref isSendingHardware, 0);
                            if (pendingColor.HasValue)
                            {
                                ScheduleHardwarePreview(pendingColor.Value);
                            }
                        }
                    });
                }
            }

            // Renk seçicide gezinirken donanımda asenkron gerçek zamanlı canlı önizleme
            picker.ColorPreviewChanged += (r, g, b) =>
            {
                ScheduleHardwarePreview((r, g, b));
            };

            if (picker.ShowDialog() == true)
            {
                SetRgbColor(picker.SelectedRed, picker.SelectedGreen, picker.SelectedBlue);
            }
            else
            {
                // İptal edilirse orijinal renge geri dön
                ApplyLedSettings();
            }
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
            if (BrdActiveColorPreview != null)
            {
                BrdActiveColorPreview.Background = new SolidColorBrush(Color.FromRgb(_red, _green, _blue));
            }
            if (TxtActiveHex != null)
            {
                TxtActiveHex.Text = $"#{_red:X2}{_green:X2}{_blue:X2}";
            }
        }

        private void ApplyLedSettings()
        {
            if (!_isInitialized || _bridge == null || _config == null) return;

            if (_brightness == 0)
            {
                _bridge.TurnOffAllLights();
            }
            else
            {
                // Hardware EC brightness levels:
                // Level 1 => 50% Brightness
                // Level 2 => 100% Brightness
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
                if (BtnThemeToggle != null) BtnThemeToggle.Content = LocalizationManager.ThemeLight;
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
                if (BtnThemeToggle != null) BtnThemeToggle.Content = LocalizationManager.ThemeDark;
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
            if (!_isInitialized || ChkAutoStart == null || _config == null) return;
            bool enable = ChkAutoStart.IsChecked == true;
            StartupManager.SetStartup(enable);
            _config.CurrentSettings.StartWithWindows = enable;
            _config.SaveSettings();
        }

        private void ApplyLocalization()
        {
            if (TxtModeOfficeLabel != null) TxtModeOfficeLabel.Text = LocalizationManager.PowerOffice;
            if (TxtModeGamingLabel != null) TxtModeGamingLabel.Text = LocalizationManager.PowerGaming;
            if (TxtModeHighPerfLabel != null) TxtModeHighPerfLabel.Text = LocalizationManager.PowerTurbo;

            if (TxtGpuHeader != null) TxtGpuHeader.Text = LocalizationManager.GpuHeader;
            if (TxtSysResourcesHeader != null) TxtSysResourcesHeader.Text = LocalizationManager.SystemResourcesHeader;
            if (TxtRamHeader != null) TxtRamHeader.Text = LocalizationManager.RamLabel;
            if (TxtSsdHeader != null) TxtSsdHeader.Text = LocalizationManager.SsdLabel;

            if (TxtKeyboardHeader != null) TxtKeyboardHeader.Text = LocalizationManager.KeyboardHeader;
            if (LblBrightnessHeader != null) LblBrightnessHeader.Text = LocalizationManager.BrightnessLabel;
            if (LblBrightnessTitle != null) LblBrightnessTitle.Text = LocalizationManager.BrightnessSliderTitle;

            if (BtnLedStatic != null) BtnLedStatic.Content = LocalizationManager.LedStatic;
            if (BtnLedBreathing != null) BtnLedBreathing.Content = LocalizationManager.LedBreathing;
            if (BtnLedDynamic != null) BtnLedDynamic.Content = LocalizationManager.LedDynamic;
            if (BtnLedRainbow != null) BtnLedRainbow.Content = LocalizationManager.LedRainbow;

            if (BtnCustomColor != null) BtnCustomColor.Content = LocalizationManager.CustomColorButton;
            if (TxtCustomColorTitle != null) TxtCustomColorTitle.Text = LocalizationManager.CustomColorTitle;
            if (TxtPresetsTitle != null) TxtPresetsTitle.Text = LocalizationManager.PresetsTitle;

            if (ChkAutoStart != null) ChkAutoStart.Content = LocalizationManager.AutoStart;
            if (BtnCheckUpdates != null) BtnCheckUpdates.Content = LocalizationManager.CheckUpdates;

            if (BtnRefreshTelemetry != null) BtnRefreshTelemetry.Content = LocalizationManager.Refresh;
            if (BtnExitApp != null) BtnExitApp.Content = LocalizationManager.Exit;
            if (BtnLangToggle != null) BtnLangToggle.Content = LocalizationManager.LangButton;

            if (BtnThemeToggle != null)
            {
                BtnThemeToggle.Content = _isDarkTheme ? LocalizationManager.ThemeLight : LocalizationManager.ThemeDark;
            }

            UpdateBrightnessUI();
        }

        private async void BtnCheckUpdates_Click(object sender, RoutedEventArgs e)
        {
            if (BtnCheckUpdates != null) BtnCheckUpdates.IsEnabled = false;
            try
            {
                await UpdateService.CheckForUpdatesAsync(LocalizationManager.IsTurkish == false, this);
            }
            finally
            {
                if (BtnCheckUpdates != null) BtnCheckUpdates.IsEnabled = true;
            }
        }

        private void BtnLangToggle_Click(object sender, RoutedEventArgs e)
        {
            string nextLang = LocalizationManager.IsTurkish ? "EN" : "TR";
            LocalizationManager.CurrentLanguage = nextLang;
            _config.CurrentSettings.Language = nextLang;
            _config.SaveSettings();

            ApplyLocalization();
            UpdatePowerModeButtonsUI();
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