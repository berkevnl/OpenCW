using System;
using System.Threading;
using OpenCW.Core;

namespace OpenCW.Services
{
    public class HardwareMonitorService : IDisposable
    {
        private readonly IHardwareProvider _provider;
        private readonly System.Threading.Timer _timer;
        private readonly int _intervalSeconds;
        private bool _isDisposed;
        private int _isPolling;

        public event Action<HardwareTelemetry>? TelemetryUpdated;

        public HardwareTelemetry LatestTelemetry { get; private set; } = new(0, 0, 0, 0, 0, 0, false);

        public HardwareMonitorService(IHardwareProvider provider, int intervalSeconds = 3)
        {
            _provider = provider;
            _intervalSeconds = Math.Max(1, intervalSeconds);
            _timer = new System.Threading.Timer(PollTelemetry, null, Timeout.Infinite, Timeout.Infinite);
        }

        public void Start()
        {
            _timer.Change(0, _intervalSeconds * 1000);
        }

        public void Stop()
        {
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        private void PollTelemetry(object? state)
        {
            if (_isDisposed) return;
            if (Interlocked.CompareExchange(ref _isPolling, 1, 0) != 0) return;

            try
            {
                var telemetry = _provider.GetTelemetry();
                LatestTelemetry = telemetry;
                TelemetryUpdated?.Invoke(telemetry);
            }
            catch
            {
                // Silently handle telemetry polling interruptions
            }
            finally
            {
                Interlocked.Exchange(ref _isPolling, 0);
            }
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            _timer.Dispose();
        }
    }
}
