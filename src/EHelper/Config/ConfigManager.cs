using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace EHelper.Config
{
    public class ConfigManager
    {
        private static readonly string AppDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "EHelper"
        );

        private static readonly string ConfigFilePath = Path.Combine(AppDataFolder, "config.json");
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
        private readonly object _fileLock = new();

        public AppSettings CurrentSettings { get; private set; }

        public ConfigManager()
        {
            CurrentSettings = LoadSettings();
        }

        public AppSettings LoadSettings()
        {
            lock (_fileLock)
            {
                try
                {
                    if (File.Exists(ConfigFilePath))
                    {
                        string json = File.ReadAllText(ConfigFilePath);
                        var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                        if (settings != null)
                        {
                            return settings;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ConfigManager] Load config failed: {ex.Message}");
                }

                return new AppSettings();
            }
        }

        public void SaveSettings(AppSettings? settings = null)
        {
            if (settings != null)
            {
                CurrentSettings = settings;
            }

            lock (_fileLock)
            {
                try
                {
                    if (!Directory.Exists(AppDataFolder))
                    {
                        Directory.CreateDirectory(AppDataFolder);
                    }

                    string tempPath = ConfigFilePath + ".tmp";
                    string json = JsonSerializer.Serialize(CurrentSettings, JsonOptions);
                    File.WriteAllText(tempPath, json);
                    File.Move(tempPath, ConfigFilePath, true);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ConfigManager] Save config failed: {ex.Message}");
                }
            }
        }
    }
}
