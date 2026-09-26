using System;
using System.Threading;
using System.Windows;
using EHelper.Config;
using EHelper.Hardware;
using EHelper.Services;
using EHelper.UI;

namespace EHelper
{
    public partial class App : System.Windows.Application
    {
        private const string AppMutexName = "EHelper_SingleInstance_Mutex_Excalibur";
        private Mutex? _mutex;
        private IHardwareBridge? _bridge;
        private HardwareMonitorService? _monitor;
        private SystemTrayManager? _trayManager;
        private MainWindow? _mainWindow;
        private int _telemetryCounter;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Global Exception Handling
            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                if (args.ExceptionObject is Exception ex)
                {
                    System.Windows.MessageBox.Show(
                        $"Beklenmeyen bir hata oluştu:\n{ex.Message}\n\n{ex.StackTrace}",
                        "E-Helper Hatası",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                }
            };

            DispatcherUnhandledException += (s, args) =>
            {
                args.Handled = true;
                System.Windows.MessageBox.Show(
                    $"Arayüz hatası yakalandı:\n{args.Exception.Message}\n\n{args.Exception.StackTrace}",
                    "E-Helper Hatası",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
            };

            // Single-instance enforcement
            _mutex = new Mutex(true, AppMutexName, out bool isNewInstance);
            if (!isNewInstance)
            {
                System.Windows.MessageBox.Show(
                    "E-Helper zaten arka planda çalışıyor. Sistem tepsisindeki (Gizli Simgeler) simgeye tıklayabilirsiniz.",
                    "E-Helper",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
                Shutdown();
                return;
            }

            try
            {
                // 1. Initialize Hardware Bridge (WMI or Mock safe fallback)
                var (bridge, isSimulated) = HardwareBridgeFactory.CreateBridge();
                _bridge = bridge;

                // 2. Initialize Configuration Engine
                var config = new ConfigManager();

                // 3. Initialize Hardware Telemetry Service (do not start yet)
                _monitor = new HardwareMonitorService(_bridge, config.CurrentSettings.PollingIntervalSeconds);

                // 4. Initialize Main Flyout Window (stays hidden until tray icon clicked)
                _mainWindow = new MainWindow(_bridge, isSimulated, config, _monitor);

                // 5. Initialize System Tray (Notification Area Icon)
                _trayManager = new SystemTrayManager(_mainWindow, ShutdownApp);

                // 6. Wire up Telemetry Tooltip & Periodic Memory Optimizer
                _monitor.TelemetryUpdated += t =>
                {
                    if (_trayManager != null)
                    {
                        if (t.IsAvailable)
                        {
                            _trayManager.UpdateTooltip($"CPU: {t.CpuTemperature}°C Fan: {t.CpuFanRpm}RPM\nGPU: {t.GpuTemperature}°C Fan: {t.GpuFanRpm}RPM");
                        }
                        else
                        {
                            _trayManager.UpdateTooltip("CPU: --°C Fan: --RPM\nGPU: --°C Fan: --RPM");
                        }
                    }

                    // Periodically keep background memory lean if flyout window is not open
                    if (++_telemetryCounter % 10 == 0)
                    {
                        Dispatcher.InvokeAsync(() =>
                        {
                            if (_mainWindow != null && !_mainWindow.IsVisible)
                            {
                                MemoryOptimizer.TrimMemory();
                            }
                        });
                    }
                };

                // 7. Start Telemetry Service after all UI & Tray objects are ready
                _monitor.Start();

                // Initial working set trim to drop memory to G-Helper levels (~2-5 MB)
                MemoryOptimizer.TrimMemory();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"E-Helper başlatılırken bir hata oluştu:\n{ex.Message}\n\nDetay:\n{ex.StackTrace}",
                    "E-Helper Hatası",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                Shutdown();
            }
        }

        public void ShutdownApp()
        {
            try
            {
                _monitor?.Stop();
                _monitor?.Dispose();
                _trayManager?.Dispose();
                _bridge?.Dispose();
                _mutex?.ReleaseMutex();
                _mutex?.Dispose();
            }
            catch
            {
                // Ignore cleanup errors during shutdown
            }
            finally
            {
                Shutdown();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            ShutdownApp();
            base.OnExit(e);
        }
    }
}
