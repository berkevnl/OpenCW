using System;
using System.Management;

namespace EHelper.Hardware
{
    public static class ModelDetector
    {
        public static (string ModelName, string BaseboardProduct, bool IsExcalibur) DetectSystem()
        {
            string modelName = "Casper Excalibur";
            string baseboard = "Unknown Board";
            bool isExcalibur = false;

            try
            {
                using var csSearcher = new ManagementObjectSearcher("SELECT Manufacturer, Model, SystemFamily FROM Win32_ComputerSystem");
                foreach (ManagementObject obj in csSearcher.Get())
                {
                    string mfg = obj["Manufacturer"]?.ToString() ?? "";
                    string model = obj["Model"]?.ToString() ?? "";
                    string family = obj["SystemFamily"]?.ToString() ?? "";

                    if (!string.IsNullOrWhiteSpace(model))
                    {
                        modelName = model;
                    }
                    else if (!string.IsNullOrWhiteSpace(family))
                    {
                        modelName = family;
                    }

                    if (mfg.Contains("Casper", StringComparison.OrdinalIgnoreCase) ||
                        model.Contains("Excalibur", StringComparison.OrdinalIgnoreCase) ||
                        family.Contains("Excalibur", StringComparison.OrdinalIgnoreCase))
                    {
                        isExcalibur = true;
                    }
                }
            }
            catch
            {
                // Fallback if WMI query fails
            }

            try
            {
                using var bbSearcher = new ManagementObjectSearcher("SELECT Manufacturer, Product FROM Win32_BaseBoard");
                foreach (ManagementObject obj in bbSearcher.Get())
                {
                    string bbProduct = obj["Product"]?.ToString() ?? "";
                    string bbMfg = obj["Manufacturer"]?.ToString() ?? "";

                    if (!string.IsNullOrWhiteSpace(bbProduct))
                    {
                        baseboard = bbProduct;
                    }

                    if (bbMfg.Contains("Casper", StringComparison.OrdinalIgnoreCase) ||
                        bbProduct.Contains("G920", StringComparison.OrdinalIgnoreCase) ||
                        bbProduct.Contains("G870", StringComparison.OrdinalIgnoreCase) ||
                        bbProduct.Contains("G770", StringComparison.OrdinalIgnoreCase))
                    {
                        isExcalibur = true;
                    }
                }
            }
            catch
            {
                // Fallback
            }

            // Normalizing presentation string
            if (modelName.Contains("G920", StringComparison.OrdinalIgnoreCase) || baseboard.Contains("G920", StringComparison.OrdinalIgnoreCase))
            {
                modelName = "Casper Excalibur G920";
                isExcalibur = true;
            }
            else if (modelName.Contains("G870", StringComparison.OrdinalIgnoreCase) || baseboard.Contains("G870", StringComparison.OrdinalIgnoreCase))
            {
                modelName = "Casper Excalibur G870";
                isExcalibur = true;
            }
            else if (modelName.Contains("G770", StringComparison.OrdinalIgnoreCase) || baseboard.Contains("G770", StringComparison.OrdinalIgnoreCase))
            {
                modelName = "Casper Excalibur G770";
                isExcalibur = true;
            }

            return (modelName, baseboard, isExcalibur);
        }
    }
}
