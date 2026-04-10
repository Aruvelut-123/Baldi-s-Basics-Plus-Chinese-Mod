using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BBPC.API
{
    public static class UpdateChecker
    {
        private const string RepoOwner = "Aruvelut-123";
        private const string RepoName = "Baldi-s-Basics-Plus-Chinese-Mod";
        private const string UpdateUrl = "https://gamebanana.com/mods/updates/610816";

        private static readonly HttpClient _httpClient = CreateHttpClient();

        public static bool IsUpdateAvailable { get; private set; } = false;
        public static string LatestVersionString { get; private set; } = string.Empty;
        public static string CurrentVersionString { get; private set; } = BBPCTemp.ModVersion;

        private static HttpClient CreateHttpClient()
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("BBPCUpdateChecker", "1.0"));
            return client;
        }

        public static string GetReleasesPageUrl()
        {
            return UpdateUrl;
        }

        public static async Task CheckForUpdates()
        {
            IsUpdateAvailable = false;
            LatestVersionString = string.Empty;
            CurrentVersionString = BBPCTemp.ModVersion;

            try
            {
                string url = $"https://api.github.com/repos/{RepoOwner}/{RepoName}/releases/latest";
                HttpResponseMessage response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();

                    Regex tagRegex = new Regex("\"tag_name\"\\s*:\\s*\"([^\"]+)\"");
                    Match match = tagRegex.Match(jsonResponse);

                    if (match.Success && match.Groups.Count >= 2)
                    {
                        string latestVersionTag = match.Groups[1].Value;

                        if (!string.IsNullOrEmpty(latestVersionTag))
                        {
                            Version currentModVersion = new Version(CurrentVersionString);
                            string sanitizedLatestVersion = latestVersionTag.StartsWith("v") ? latestVersionTag.Substring(1) : latestVersionTag;

                            try
                            {
                                Version latestGitHubVersion = new Version(sanitizedLatestVersion);

                                if (latestGitHubVersion > currentModVersion)
                                {
                                    Logger.Warning($"模组有新版本可用: {latestVersionTag}! 当前版本: v{CurrentVersionString}");
                                    IsUpdateAvailable = true;
                                    LatestVersionString = latestVersionTag;
                                }
                                else
                                {
                                    Logger.Info("已安装最新版本模组。");
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Error($"版本比较时出错: {ex.Message}");
                            }
                        }
                    }
                }
                else
                {
                    Logger.Error($"GitHub API 请求失败 ({RepoOwner}/{RepoName}): {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"检查更新时出现异常: {ex.Message}");
            }
        }
    }
}
