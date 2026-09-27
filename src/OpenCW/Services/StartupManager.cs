using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace OpenCW.Services
{
    public static class StartupManager
    {
        private const string TaskName = "OpenCW";
        private const string RunRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "OpenCW";

        public static bool IsStartupEnabled()
        {
            try
            {
                // 1. Primary check: Windows Task Scheduler (required for elevated apps)
                int exitCode = RunHiddenProcess("schtasks.exe", $"/query /tn \"{TaskName}\"");
                if (exitCode == 0)
                {
                    return true;
                }

                // 2. Fallback check: CurrentUser Run registry key
                using var key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, false);
                return key?.GetValue(AppName) != null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[StartupManager] IsStartupEnabled query failed: {ex.Message}");
                return false;
            }
        }

        public static void SetStartup(bool enable)
        {
            try
            {
                string? exePath = Environment.ProcessPath;
                if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
                {
                    return;
                }

                if (enable)
                {
                    // 1. Create or update Windows Task Scheduler task with highest privileges
                    // This allows OpenCW (which requires Administrator for ACPI WMI SMI access)
                    // to launch on Windows logon silently without triggering a UAC prompt.
                    string taskArgs = $"/create /tn \"{TaskName}\" /tr \"\\\"{exePath}\\\" --autostart\" /sc onlogon /rl highest /f";
                    int taskResult = RunHiddenProcess("schtasks.exe", taskArgs);
                    Debug.WriteLine($"[StartupManager] schtasks create exit code: {taskResult}");

                    // 2. Also register in HKCU Run for compatibility / visibility in Task Manager Startup tab
                    try
                    {
                        using var key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, true);
                        key?.SetValue(AppName, $"\"{exePath}\" --autostart");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[StartupManager] Registry write failed: {ex.Message}");
                    }
                }
                else
                {
                    // 1. Remove Windows Task Scheduler task
                    int taskResult = RunHiddenProcess("schtasks.exe", $"/delete /tn \"{TaskName}\" /f");
                    Debug.WriteLine($"[StartupManager] schtasks delete exit code: {taskResult}");

                    // 2. Remove from HKCU Run
                    try
                    {
                        using var key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, true);
                        key?.DeleteValue(AppName, false);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[StartupManager] Registry delete failed: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[StartupManager] SetStartup({enable}) failed: {ex.Message}");
            }
        }

        public static void EnsureStartupSynchronized(bool userWantsStartup)
        {
            try
            {
                bool isEnabled = IsStartupEnabled();
                if (userWantsStartup && !isEnabled)
                {
                    SetStartup(true);
                }
                else if (!userWantsStartup && isEnabled)
                {
                    SetStartup(false);
                }
                else if (userWantsStartup && isEnabled)
                {
                    // Refresh task action in case executable path changed or updated
                    SetStartup(true);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[StartupManager] EnsureStartupSynchronized failed: {ex.Message}");
            }
        }

        private static int RunHiddenProcess(string fileName, string arguments)
        {
            try
            {
                using var proc = new Process();
                proc.StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                proc.Start();
                proc.WaitForExit(4000);
                return proc.ExitCode;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[StartupManager] RunHiddenProcess failed ({fileName}): {ex.Message}");
                return -1;
            }
        }
    }
}
