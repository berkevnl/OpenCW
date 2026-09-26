using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using MessageBox = System.Windows.MessageBox;
using Application = System.Windows.Application;

namespace EHelper.Services
{
    public static class UpdateService
    {
        private const string GitHubRepo = "berkevnl/e-helper";
        public const string CurrentVersion = "1.0.0";

        private static readonly HttpClient HttpClient = new();

        static UpdateService()
        {
            HttpClient.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue("EHelper-App", CurrentVersion)
            );
            HttpClient.Timeout = TimeSpan.FromSeconds(15);
        }

        public static async Task CheckForUpdatesAsync(bool isEnglish, Window? owner = null)
        {
            try
            {
                string url = $"https://api.github.com/repos/{GitHubRepo}/releases/latest";
                var response = await HttpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    string notFoundMsg = isEnglish
                        ? "No published Releases found on GitHub yet, or repository is Private.\nUpdates will be downloaded here automatically once a release is published."
                        : "GitHub üzerinde henüz yayınlanmış bir Sürüm (Release) bulunmuyor veya depo gizli (Private) durumda.\nİlk sürüm yayınlandığında güncellemeler buradan otomatik kontrol edilecektir.";

                    MessageBox.Show(owner ?? Application.Current.MainWindow, notFoundMsg, "E-Helper", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                string json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                string tagName = root.TryGetProperty("tag_name", out var tagProp) ? tagProp.GetString() ?? "" : "";
                string body = root.TryGetProperty("body", out var bodyProp) ? bodyProp.GetString() ?? "" : "";

                // Parse version (e.g., "v1.0.1" -> "1.0.1")
                string cleanVersion = tagName.TrimStart('v', 'V').Trim();
                if (!Version.TryParse(cleanVersion, out var remoteVersion) ||
                    !Version.TryParse(CurrentVersion, out var localVersion))
                {
                    string invalidMsg = isEnglish
                        ? $"Current version is v{CurrentVersion}. Could not verify remote tag '{tagName}'."
                        : $"Mevcut sürüm v{CurrentVersion}. Uzak sürüm etiketi '{tagName}' doğrulanamadı.";

                    MessageBox.Show(owner ?? Application.Current.MainWindow, invalidMsg, "E-Helper", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (remoteVersion <= localVersion)
                {
                    string upToDateMsg = isEnglish
                        ? $"E-Helper is up to date (v{CurrentVersion}). No new updates found."
                        : $"E-Helper güncel (v{CurrentVersion}). Yeni bir güncelleme bulunmuyor.";

                    MessageBox.Show(owner ?? Application.Current.MainWindow, upToDateMsg, "E-Helper", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // New version found!
                string? downloadUrl = null;

                if (root.TryGetProperty("assets", out var assetsProp) && assetsProp.ValueKind == JsonValueKind.Array)
                {
                    foreach (var asset in assetsProp.EnumerateArray())
                    {
                        if (asset.TryGetProperty("name", out var nameProp) &&
                            asset.TryGetProperty("browser_download_url", out var urlProp))
                        {
                            string name = nameProp.GetString() ?? "";
                            if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                            {
                                downloadUrl = urlProp.GetString();
                                break;
                            }
                        }
                    }
                }

                string promptMsg = isEnglish
                    ? $"A new version is available: v{cleanVersion}\n(Current version: v{CurrentVersion})\n\nRelease notes:\n{body}\n\nDo you want to update to v{cleanVersion} now?"
                    : $"Yeni bir sürüm mevcut: v{cleanVersion}\n(Mevcut sürüm: v{CurrentVersion})\n\nSürüm notları:\n{body}\n\nv{cleanVersion} sürümüne güncellemek istiyor musunuz?";

                var result = MessageBox.Show(
                    owner ?? Application.Current.MainWindow,
                    promptMsg,
                    isEnglish ? "Update Available" : "Güncelleme Mevcut",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question
                );

                if (result != MessageBoxResult.Yes) return;

                if (string.IsNullOrEmpty(downloadUrl))
                {
                    // Fall back to opening GitHub release page in browser
                    string releasePage = root.TryGetProperty("html_url", out var pageProp) 
                        ? pageProp.GetString() ?? $"https://github.com/{GitHubRepo}/releases" 
                        : $"https://github.com/{GitHubRepo}/releases";

                    Process.Start(new ProcessStartInfo(releasePage) { UseShellExecute = true });
                    return;
                }

                // Download the asset and self-update
                await PerformUpdateAsync(downloadUrl, isEnglish, owner);
            }
            catch (Exception ex)
            {
                string errMsg = isEnglish
                    ? $"Update check failed: {ex.Message}"
                    : $"Güncelleme kontrolü başarısız oldu: {ex.Message}";

                MessageBox.Show(owner ?? Application.Current.MainWindow, errMsg, "E-Helper", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private static async Task PerformUpdateAsync(string downloadUrl, bool isEnglish, Window? owner)
        {
            string currentExe = Environment.ProcessPath ?? "";
            if (string.IsNullOrEmpty(currentExe) || !File.Exists(currentExe))
            {
                Process.Start(new ProcessStartInfo(downloadUrl) { UseShellExecute = true });
                return;
            }

            string tempDownloadPath = Path.Combine(Path.GetTempPath(), $"EHelper_update_{Guid.NewGuid():N}.exe");
            string updaterScript = Path.Combine(Path.GetTempPath(), $"EHelper_updater_{Guid.NewGuid():N}.bat");

            try
            {
                var bytes = await HttpClient.GetByteArrayAsync(downloadUrl);
                await File.WriteAllBytesAsync(tempDownloadPath, bytes);

                int pid = Environment.ProcessId;

                // Write batch script to wait for this process, replace EXE, and restart
                string scriptContent = $@"@echo off
timeout /t 1 /nobreak >nul
:waitloop
tasklist /fi ""pid eq {pid}"" | find ""{pid}"" >nul
if not errorlevel 1 (
    timeout /t 1 /nobreak >nul
    goto waitloop
)
copy /y ""{tempDownloadPath}"" ""{currentExe}"" >nul
del ""{tempDownloadPath}"" >nul
start """" ""{currentExe}""
del ""%~f0"" & exit
";

                await File.WriteAllTextAsync(updaterScript, scriptContent);

                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c \"\"{updaterScript}\"\"",
                    CreateNoWindow = true,
                    UseShellExecute = false
                };

                Process.Start(psi);

                // Exit cleanly so user settings in AppData are preserved and file unlocked
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                string failMsg = isEnglish
                    ? $"Failed to apply update: {ex.Message}\nYou can download it manually from GitHub."
                    : $"Güncelleme uygulanamadı: {ex.Message}\nManuel olarak GitHub'dan indirebilirsiniz.";

                MessageBox.Show(owner ?? Application.Current.MainWindow, failMsg, "E-Helper", MessageBoxButton.OK, MessageBoxImage.Error);
                Process.Start(new ProcessStartInfo($"https://github.com/{GitHubRepo}/releases") { UseShellExecute = true });
            }
        }
    }
}
