using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BBPC.API
{
    public static class UpdateChecker
    {
        private const string RepoOwner = "Aruvelut-123";
        private const string RepoName = "Baldi-s-Basics-Plus-Chinese-Mod";
        private const string UpdateUrl = "https://gamebanana.com/mods/updates/610816";
        private static readonly Uri ReleasesApiUrl =
            new Uri($"https://api.github.com/repos/{RepoOwner}/{RepoName}/releases/latest");
        private static readonly HttpClient HttpClient = CreateHttpClient();

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

            try
            {
                using (HttpResponseMessage response = await HttpClient.GetAsync(ReleasesApiUrl))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        Logger.Warning($"检查更新失败，GitHub 返回状态码 {(int)response.StatusCode} ({response.StatusCode})。");
                        return;
                    }

                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    string? latestVersionTag = JObject.Parse(jsonResponse).Value<string>("tag_name");
                    if (latestVersionTag == null || latestVersionTag.Trim().Length == 0)
                    {
                        Logger.Warning("检查更新失败，GitHub 响应中缺少版本标签。");
                        return;
                    }

                    if (!Version.TryParse(CurrentVersionString, out Version currentVersion) ||
                        !Version.TryParse(latestVersionTag.TrimStart('v', 'V'), out Version latestVersion))
                    {
                        Logger.Warning($"无法比较模组版本：当前版本 '{CurrentVersionString}'，最新版本 '{latestVersionTag}'。");
                        return;
                    }

                    if (latestVersion > currentVersion)
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
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is JsonException || ex is TaskCanceledException)
            {
                Logger.Warning($"检查更新失败: {ex.Message}");
            }
        }

        private static HttpClient CreateHttpClient()
        {
            HttpClient client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(15)
            };
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("BBPCUpdateChecker", "1.0"));
            return client;
        }
    }
}
