using System;
using System.Threading;
using OpenCW.Core;

namespace OpenCW.Services
{
    /// <summary>
    /// Smooth software-driven RGB Rainbow Wave service that animates vibrant linear transitions
    /// across the 3 keyboard zones (Left -> Center -> Right) with full saturation and brightness.
    /// </summary>
    public class RainbowAnimationService : IDisposable
    {
        private readonly IHardwareProvider _bridge;
        private readonly System.Threading.Timer _timer;
        private double _currentHue;
        private byte _brightness = 2;
        private bool _isRunning;
        private int _isTicking;

        public bool IsRunning => _isRunning;

        public RainbowAnimationService(IHardwareProvider bridge)
        {
            _bridge = bridge;
            _timer = new System.Threading.Timer(OnTick, null, Timeout.Infinite, Timeout.Infinite);
        }

        public void Start(byte brightness)
        {
            _brightness = brightness;
            _isRunning = true;
            // Update every 50ms (~20 FPS) for buttery smooth transition with negligible CPU usage
            _timer.Change(0, 50);
        }

        public void UpdateBrightness(byte brightness)
        {
            _brightness = brightness;
            if (_brightness == 0)
            {
                Stop();
            }
            else if (!_isRunning)
            {
                Start(brightness);
            }
        }

        public void Stop()
        {
            _isRunning = false;
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        private void OnTick(object? state)
        {
            if (!_isRunning || _brightness == 0) return;
            if (Interlocked.CompareExchange(ref _isTicking, 1, 0) != 0) return;

            try
            {
                // Advance hue linearly and slowly:
                // 360 degrees in ~12 seconds = 30 deg/sec = 1.5 deg per 50ms tick
                _currentHue = (_currentHue + 1.5) % 360.0;

                // Linear wave from Left to Right across the 3 keyboard hardware zones:
                // Left zone receives leading hue, Center is phase-delayed, Right is further delayed
                double hLeft = _currentHue;
                double hCenter = (hLeft - 45.0 + 360.0) % 360.0;
                double hRight = (hLeft - 90.0 + 360.0) % 360.0;

                // Full saturation (1.0) and full value (1.0) for maximum vibrancy matching Static mode
                var (rL, gL, bL) = HsvToRgb(hLeft, 1.0, 1.0);
                var (rC, gC, bC) = HsvToRgb(hCenter, 1.0, 1.0);
                var (rR, gR, bR) = HsvToRgb(hRight, 1.0, 1.0);

                // Base prime for single-zone keyboards
                _bridge.SetLed(LedZone.All, LedMode.Static, _brightness, rL, gL, bL);
                _bridge.SetLed(LedZone.AllKeyboard, LedMode.Static, _brightness, rL, gL, bL);

                // Multi-zone hardware: 3 distinct zones moving from Left to Right
                _bridge.SetLed(LedZone.KeyboardLeft, LedMode.Static, _brightness, rL, gL, bL);
                _bridge.SetLed(LedZone.KeyboardCenter, LedMode.Static, _brightness, rC, gC, bC);
                _bridge.SetLed(LedZone.KeyboardRight, LedMode.Static, _brightness, rR, gR, bR);
            }
            catch
            {
                // Suppress background thread interruptions
            }
            finally
            {
                Interlocked.Exchange(ref _isTicking, 0);
            }
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
            else if (h < 240) { r = 0; g = x; b = c; }
            else if (h < 300) { r = x; g = 0; b = c; }
            else { r = c; g = 0; b = x; }

            return ((byte)((r + m) * 255), (byte)((g + m) * 255), (byte)((b + m) * 255));
        }

        public void Dispose()
        {
            Stop();
            _timer.Dispose();
        }
    }
}
