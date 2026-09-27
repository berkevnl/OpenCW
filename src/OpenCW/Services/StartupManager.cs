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
                    string workDir = Path.GetDirectoryName(exePath) ?? AppContext.BaseDirectory;

                    // 1. Create or update Windows Task Scheduler task with highest privileges.
                    // Designed specifically for gaming laptops:
                    // - DisallowStartIfOnBatteries = false (Runs reliably even on battery power)
                    // - StopIfGoingOnBatteries = false (Does not kill process when unplugging AC adapter)
                    // - Delay = PT3S (3-second delay after logon so Windows Explorer & notification tray are ready)
                    // - WorkingDirectory = application directory (preserves native DLL and asset resolution)
                    // - ExecutionTimeLimit = PT0S (infinite, never terminates after 72 hours)
                    bool taskCreated = CreateTaskViaXml(exePath, workDir);
                    if (!taskCreated)
                    {
                        // Fallback to schtasks command line if XML creation fails
                        string taskArgs = $"/create /tn \"{TaskName}\" /tr \"\\\"{exePath}\\\" --autostart\" /sc onlogon /rl highest /f";
                        RunHiddenProcess("schtasks.exe", taskArgs);
                    }

                    // Enforce battery & execution limits via Schedule.Service COM API as extra assurance
                    ConfigureTaskBatterySettings();

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
                    // Refresh task action in case executable path changed, updated, or needs battery settings applied
                    SetStartup(true);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[StartupManager] EnsureStartupSynchronized failed: {ex.Message}");
            }
        }

        private static bool CreateTaskViaXml(string exePath, string workDir)
        {
            string tempXml = Path.Combine(Path.GetTempPath(), $"opencw_task_{Guid.NewGuid():N}.xml");
            try
            {
                string xmlContent = $@"<?xml version=""1.0"" encoding=""UTF-16""?>
<Task version=""1.2"" xmlns=""http://schemas.microsoft.com/windows/2004/02/mit/task"">
  <RegistrationInfo>
    <Author>OpenCW</Author>
    <Description>OpenCW Universal Hardware Controller Startup Task</Description>
    <URI>\{TaskName}</URI>
  </RegistrationInfo>
  <Triggers>
    <LogonTrigger>
      <Enabled>true</Enabled>
      <Delay>PT3S</Delay>
    </LogonTrigger>
  </Triggers>
  <Principals>
    <Principal id=""Author"">
      <LogonType>InteractiveToken</LogonType>
      <RunLevel>HighestAvailable</RunLevel>
    </Principal>
  </Principals>
  <Settings>
    <MultipleInstancesPolicy>IgnoreNew</MultipleInstancesPolicy>
    <DisallowStartIfOnBatteries>false</DisallowStartIfOnBatteries>
    <StopIfGoingOnBatteries>false</StopIfGoingOnBatteries>
    <AllowHardTerminate>true</AllowHardTerminate>
    <StartWhenAvailable>true</StartWhenAvailable>
    <RunOnlyIfNetworkAvailable>false</RunOnlyIfNetworkAvailable>
    <IdleSettings>
      <StopOnIdleEnd>false</StopOnIdleEnd>
      <RestartOnIdle>false</RestartOnIdle>
    </IdleSettings>
    <AllowStartOnDemand>true</AllowStartOnDemand>
    <Enabled>true</Enabled>
    <Hidden>false</Hidden>
    <RunOnlyIfIdle>false</RunOnlyIfIdle>
    <WakeToRun>false</WakeToRun>
    <ExecutionTimeLimit>PT0S</ExecutionTimeLimit>
    <Priority>7</Priority>
  </Settings>
  <Actions Context=""Author"">
    <Exec>
      <Command>{exePath}</Command>
      <Arguments>--autostart</Arguments>
      <WorkingDirectory>{workDir}</WorkingDirectory>
    </Exec>
  </Actions>
</Task>";

                File.WriteAllText(tempXml, xmlContent, System.Text.Encoding.Unicode);
                int exitCode = RunHiddenProcess("schtasks.exe", $"/create /tn \"{TaskName}\" /xml \"{tempXml}\" /f");
                return exitCode == 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[StartupManager] CreateTaskViaXml failed: {ex.Message}");
                return false;
            }
            finally
            {
                try
                {
                    if (File.Exists(tempXml))
                    {
                        File.Delete(tempXml);
                    }
                }
                catch { }
            }
        }

        private static void ConfigureTaskBatterySettings()
        {
            try
            {
                Type? serviceType = Type.GetTypeFromProgID("Schedule.Service");
                if (serviceType == null) return;

                dynamic service = Activator.CreateInstance(serviceType)!;
                service.Connect();
                dynamic folder = service.GetFolder(@"\");
                dynamic task = folder.GetTask(TaskName);
                dynamic def = task.Definition;
                def.Settings.DisallowStartIfOnBatteries = false;
                def.Settings.StopIfGoingOnBatteries = false;
                def.Settings.ExecutionTimeLimit = "PT0S";
                folder.RegisterTaskDefinition(TaskName, def, 6 /* TASK_CREATE_OR_UPDATE */, null, null, 3 /* TASK_LOGON_INTERACTIVE_TOKEN */);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[StartupManager] ConfigureTaskBatterySettings: {ex.Message}");
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
