using System.Diagnostics;

namespace EHelper.Hardware
{
    public static class HardwareBridgeFactory
    {
        public static (IHardwareBridge Bridge, bool IsSimulated) CreateBridge()
        {
            try
            {
                var realBridge = new ExcaliburBridge();
                if (realBridge.IsHardwareConnected)
                {
                    Debug.WriteLine("[HardwareBridgeFactory] Excalibur WMI bridge initialized successfully.");
                    return (realBridge, false);
                }
                
                realBridge.Dispose();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HardwareBridgeFactory] Real bridge init failed: {ex.Message}");
            }

            Debug.WriteLine("[HardwareBridgeFactory] Falling back to MockHardwareBridge.");
            return (new MockHardwareBridge(), true);
        }
    }
}
