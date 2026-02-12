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

        public static bool IsUpdateAvailable { get; private set; } = false;
        public static string LatestVersionString { get; private set; } = string.Empty;
        public static string CurrentVersionString { get; private set; } = BBPCTemp.ModVersion;

        public static string GetReleasesPageUrl()
        {
            return UpdateUrl;
        }

        public static async Task CheckForUpdates()
        {
            IsUpdateAvailable = false;
            LatestVersionString = string.Empty;
            CurrentVersionString = BBPCTemp.ModVersion;

            // temporary remove update checking
            return
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("BBPCUpdateChecker", "1.0"));

                    string url = $"https://api.github.com/repos/{RepoOwner}/{RepoName}/releases/latest";
                    HttpResponseMessage response = await client.GetAsync(url);

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
                                //  Logger.Info($"GitHub 上的最新版本 ({RepoOwner}/{RepoName}): {latestVersionTag}");
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
                                catch (Exception)
                                {
                                    //  Logger.Error($"版本比较时出错: {ex.Message}");
                                }
                            }
                            else
                            {
                                //  Logger.Error("从 GitHub API 响应中获取到空版本标签。");
                            }
                        }
                        else
                        {
                            // Logger.Error("在 GitHub API 响应中未找到版本标签。");
                        }
                    }
                    else
                    {
                        // Logger.Error($"GitHub API 请求失败 ({RepoOwner}/{RepoName}): {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
                    }
                }
            }
            catch (Exception)
            {
                // Logger.Error($"检查更新时出现异常: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}