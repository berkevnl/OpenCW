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

namespace OpenCW.Services
{
    public static class UpdateService
    {
        private const string GitHubRepo = "berkevnl/OpenCW";
        public const string CurrentVersion = "1.2.1";

        private static readonly HttpClient HttpClient = new();

        static UpdateService()
        {
            HttpClient.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue("OpenCW-App", CurrentVersion)
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

                    MessageBox.Show(owner ?? Application.Current.MainWindow, notFoundMsg, "OpenCW", MessageBoxButton.OK, MessageBoxImage.Information);
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

                    MessageBox.Show(owner ?? Application.Current.MainWindow, invalidMsg, "OpenCW", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (remoteVersion <= localVersion)
                {
                    string upToDateMsg = isEnglish
                        ? $"OpenCW is up to date (v{CurrentVersion}). No new updates found."
                        : $"OpenCW güncel (v{CurrentVersion}). Yeni bir güncelleme bulunmuyor.";

                    MessageBox.Show(owner ?? Application.Current.MainWindow, upToDateMsg, "OpenCW", MessageBoxButton.OK, MessageBoxImage.Information);
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
                    ? $"A new version is available: v{cleanVersion}\n(Current version: v{CurrentVersion})\n\nRelease notes:\n{body}\n\nDo you want to download v{cleanVersion} now?"
                    : $"Yeni bir sürüm mevcut: v{cleanVersion}\n(Mevcut sürüm: v{CurrentVersion})\n\nSürüm notları:\n{body}\n\nv{cleanVersion} sürümünü indirmek istiyor musunuz?";

                var result = MessageBox.Show(
                    owner ?? Application.Current.MainWindow,
                    promptMsg,
                    isEnglish ? "Update Available" : "Güncelleme Mevcut",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information
                );

                if (result == MessageBoxResult.Yes)
                {
                    string targetUrl = !string.IsNullOrEmpty(downloadUrl)
                        ? downloadUrl
                        : $"https://github.com/{GitHubRepo}/releases/latest";

                    Process.Start(new ProcessStartInfo(targetUrl) { UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                string errMsg = isEnglish
                    ? $"Update check failed: {ex.Message}"
                    : $"Güncelleme kontrolü başarısız oldu: {ex.Message}";

                MessageBox.Show(owner ?? Application.Current.MainWindow, errMsg, "OpenCW", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
