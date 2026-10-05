using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BBPC.API
{
    public enum UpdateDownloadState
    {
        Idle,
        Downloading,
        ReadyToRestart,
        Failed
    }

    internal enum DigestVerificationResult
    {
        Valid,
        Missing,
        Invalid
    }

    public sealed class UpdateReleaseInfo
    {
        public UpdateReleaseInfo(string tagName, string body, string pageUrl, string? assetName,
            Uri? assetUrl, string? assetDigest, long? assetSize)
        {
            TagName = tagName;
            Body = body;
            PageUrl = pageUrl;
            AssetName = assetName;
            AssetUrl = assetUrl;
            AssetDigest = assetDigest;
            AssetSize = assetSize;
        }

        public string TagName { get; }
        public string Body { get; }
        public string PageUrl { get; }
        public string? AssetName { get; }
        public Uri? AssetUrl { get; }
        public string? AssetDigest { get; }
        public long? AssetSize { get; }
    }

    public static class UpdateChecker
    {
        private const string RepoOwner = "Aruvelut-123";
        private const string RepoName = "Baldi-s-Basics-Plus-Chinese-Mod";
        private const string UpdateUrl = "https://gamebanana.com/mods/updates/610816";
        private const string RequiredAssetName = "BBPC.dll";
        private static readonly Uri ReleasesApiUrl =
            new Uri($"https://api.github.com/repos/{RepoOwner}/{RepoName}/releases/latest");
        private static readonly HttpClient HttpClient = CreateHttpClient();
        private static readonly object StateLock = new object();
        private static Task<bool>? downloadTask;
        private static bool isUpdateAvailable;
        private static string latestVersionString = string.Empty;
        private static UpdateReleaseInfo? latestRelease;
        private static UpdateDownloadState downloadState;
        private static float downloadProgress;
        private static string downloadStatus = string.Empty;
        private static string? downloadError;
        private static string? downloadWarning;
        private static string? pendingUpdatePath;
        private static string? pendingVersionString;

        static UpdateChecker()
        {
            try
            {
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            }
            catch
            {
                // The game runtime may already have configured its TLS policy.
            }

            RestorePendingUpdateState();
        }

        public static bool IsUpdateAvailable
        {
            get { lock (StateLock) return isUpdateAvailable; }
        }

        public static string LatestVersionString
        {
            get { lock (StateLock) return latestVersionString; }
        }

        public static string CurrentVersionString => BBPCTemp.ModVersion;

        public static UpdateReleaseInfo? LatestRelease
        {
            get { lock (StateLock) return latestRelease; }
        }

        public static UpdateDownloadState DownloadState
        {
            get { lock (StateLock) return downloadState; }
        }

        public static float DownloadProgress
        {
            get { lock (StateLock) return downloadProgress; }
        }

        public static string DownloadStatus
        {
            get { lock (StateLock) return downloadStatus; }
        }

        public static string? DownloadError
        {
            get { lock (StateLock) return downloadError; }
        }

        public static string? DownloadWarning
        {
            get { lock (StateLock) return downloadWarning; }
        }

        public static bool HasPendingRestart
        {
            get
            {
                lock (StateLock)
                {
                    return !string.IsNullOrEmpty(pendingUpdatePath) && File.Exists(pendingUpdatePath);
                }
            }
        }

        public static string? PendingUpdatePath
        {
            get { lock (StateLock) return pendingUpdatePath; }
        }

        public static string? PendingVersionString
        {
            get { lock (StateLock) return pendingVersionString; }
        }

        public static string TargetAssemblyPath => GetTargetAssemblyPath();

        public static string GetReleasesPageUrl()
        {
            return LatestRelease?.PageUrl ?? UpdateUrl;
        }

        public static async Task CheckForUpdates()
        {
            lock (StateLock)
            {
                isUpdateAvailable = false;
                latestVersionString = string.Empty;
                latestRelease = null;
                downloadError = null;
                downloadWarning = null;
            }

            RestorePendingUpdateState();

            try
            {
                using (HttpResponseMessage response = await HttpClient.GetAsync(ReleasesApiUrl).ConfigureAwait(false))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        Logger.Warning($"检查更新失败，GitHub 返回状态码 {(int)response.StatusCode} ({response.StatusCode})。");
                        return;
                    }

                    string jsonResponse = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    JObject releaseJson = JObject.Parse(jsonResponse);
                    string? latestVersionTag = releaseJson.Value<string>("tag_name")?.Trim();
                    if (string.IsNullOrEmpty(latestVersionTag))
                    {
                        Logger.Warning("检查更新失败，GitHub 响应中缺少版本标签。");
                        return;
                    }

                    string latestVersionText = latestVersionTag!.TrimStart('v', 'V');
                    if (!Version.TryParse(CurrentVersionString, out Version currentVersion) ||
                        !Version.TryParse(latestVersionText, out Version latestVersion))
                    {
                        Logger.Warning($"无法比较模组版本：当前版本 '{CurrentVersionString}'，最新版本 '{latestVersionTag}'。");
                        return;
                    }

                    UpdateReleaseInfo release = ParseRelease(releaseJson, latestVersionTag);
                    lock (StateLock)
                    {
                        latestRelease = release;
                        latestVersionString = latestVersionTag;
                        isUpdateAvailable = latestVersion > currentVersion;
                    }

                    if (latestVersion > currentVersion)
                    {
                        Logger.Warning($"模组有新版本可用: {latestVersionTag}! 当前版本: v{CurrentVersionString}");
                    }
                    else
                    {
                        Logger.Info("已安装最新版本模组。");
                    }
                }
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is JsonException ||
                                       ex is TaskCanceledException || ex is IOException)
            {
                Logger.Warning($"检查更新失败: {ex.Message}");
            }
        }

        public static Task<bool> DownloadUpdateAsync()
        {
            lock (StateLock)
            {
                if (downloadTask != null && !downloadTask.IsCompleted)
                {
                    return downloadTask;
                }

                downloadTask = DownloadUpdateCoreAsync();
                return downloadTask;
            }
        }

        private static async Task<bool> DownloadUpdateCoreAsync()
        {
            UpdateReleaseInfo? release = LatestRelease;
            if (release == null || release.AssetUrl == null)
            {
                SetDownloadFailure("最新版本没有可下载的 BBPC.dll 文件。");
                return false;
            }

            string targetPath = GetTargetAssemblyPath();
            if (string.IsNullOrEmpty(targetPath))
            {
                SetDownloadFailure("无法确定当前 BBPC.dll 的安装路径。");
                return false;
            }

            string? targetDirectory = Path.GetDirectoryName(targetPath);
            if (string.IsNullOrEmpty(targetDirectory))
            {
                SetDownloadFailure("无法确定 BBPC.dll 的插件目录。");
                return false;
            }

            string temporaryPath = targetPath + ".download-" + Guid.NewGuid().ToString("N");
            string stagedPath = GetPendingUpdatePath(targetPath);
            lock (StateLock)
            {
                downloadWarning = null;
            }
            SetDownloadState(UpdateDownloadState.Downloading, 0f, "正在连接更新服务器…", null);

            try
            {
                Directory.CreateDirectory(targetDirectory);
                using (HttpResponseMessage response = await HttpClient.GetAsync(
                    release.AssetUrl, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        SetDownloadFailure($"下载失败，服务器返回 {(int)response.StatusCode} ({response.StatusCode})。");
                        return false;
                    }

                    long? expectedLength = response.Content.Headers.ContentLength ?? release.AssetSize;
                    using (Stream input = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                    using (FileStream output = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write,
                               FileShare.None, 81920, FileOptions.SequentialScan))
                    {
                        byte[] buffer = new byte[81920];
                        long downloaded = 0;
                        int read;
                        while ((read = await input.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0)
                        {
                            await output.WriteAsync(buffer, 0, read).ConfigureAwait(false);
                            downloaded += read;
                            float progress = expectedLength.HasValue && expectedLength.Value > 0
                                ? Math.Min(1f, (float)downloaded / expectedLength.Value)
                                : 0f;
                            SetDownloadState(UpdateDownloadState.Downloading, progress,
                                expectedLength.HasValue
                                    ? $"正在下载更新… {downloaded / 1024d / 1024d:0.00} / {expectedLength.Value / 1024d / 1024d:0.00} MB"
                                    : $"正在下载更新… {downloaded / 1024d / 1024d:0.00} MB",
                                null);
                        }
                    }

                    long actualLength = new FileInfo(temporaryPath).Length;
                    if (actualLength <= 0 || (expectedLength.HasValue && actualLength != expectedLength.Value))
                    {
                        SetDownloadFailure("下载文件大小校验失败，未安装更新。");
                        return false;
                    }

                    DigestVerificationResult digestResult = VerifyDigest(temporaryPath, release.AssetDigest);
                    if (digestResult == DigestVerificationResult.Invalid)
                    {
                        SetDownloadFailure("下载文件 SHA-256 校验失败，未安装更新。");
                        return false;
                    }
                    if (digestResult == DigestVerificationResult.Missing)
                    {
                        lock (StateLock)
                        {
                            downloadWarning = "警告：GitHub 资产没有提供 SHA-256，文件大小已校验但无法完成完整性校验。";
                        }
                    }

                    if (File.Exists(stagedPath))
                    {
                        File.Delete(stagedPath);
                    }
                    File.Move(temporaryPath, stagedPath);
                    lock (StateLock)
                    {
                        pendingUpdatePath = stagedPath;
                        pendingVersionString = release.TagName;
                        downloadState = UpdateDownloadState.ReadyToRestart;
                        downloadProgress = 1f;
                        downloadStatus = "更新下载完成，等待重启替换文件。";
                        downloadError = null;
                    }
                    return true;
                }
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is IOException ||
                                       ex is UnauthorizedAccessException || ex is TaskCanceledException)
            {
                SetDownloadFailure($"下载更新失败：{ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                SetDownloadFailure($"处理更新文件失败：{ex.Message}");
                return false;
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    try { File.Delete(temporaryPath); }
                    catch { /* Keep the temporary file for a later cleanup attempt. */ }
                }
            }
        }

        private static UpdateReleaseInfo ParseRelease(JObject releaseJson, string tagName)
        {
            string body = releaseJson.Value<string>("body") ?? string.Empty;
            string pageUrl = releaseJson.Value<string>("html_url") ?? UpdateUrl;
            string? assetName = null;
            Uri? assetUrl = null;
            string? assetDigest = null;
            long? assetSize = null;

            if (releaseJson["assets"] is JArray assets)
            {
                JObject? asset = assets.OfType<JObject>().FirstOrDefault(item =>
                    string.Equals(item.Value<string>("name"), RequiredAssetName, StringComparison.OrdinalIgnoreCase));
                string? browserUrl = asset?.Value<string>("browser_download_url");
                if (asset != null && Uri.TryCreate(browserUrl, UriKind.Absolute, out Uri parsedUrl))
                {
                    assetName = asset.Value<string>("name");
                    assetUrl = parsedUrl;
                    assetDigest = asset.Value<string>("digest");
                    assetSize = asset.Value<long?>("size");
                }
            }

            return new UpdateReleaseInfo(tagName, body, pageUrl, assetName, assetUrl, assetDigest, assetSize);
        }

        private static DigestVerificationResult VerifyDigest(string path, string? digest)
        {
            if (string.IsNullOrWhiteSpace(digest)) return DigestVerificationResult.Missing;
            string expected = digest!.Trim();
            int separator = expected.IndexOf(':');
            if (separator >= 0) expected = expected.Substring(separator + 1);
            if (expected.Length != 64 || expected.Any(character => !Uri.IsHexDigit(character)))
            {
                return DigestVerificationResult.Invalid;
            }

            using (SHA256 sha256 = SHA256.Create())
            using (FileStream stream = File.OpenRead(path))
            {
                string actual = BitConverter.ToString(sha256.ComputeHash(stream)).Replace("-", string.Empty)
                    .ToLowerInvariant();
                return string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase)
                    ? DigestVerificationResult.Valid
                    : DigestVerificationResult.Invalid;
            }
        }

        private static void SetDownloadState(UpdateDownloadState state, float progress, string status, string? error)
        {
            lock (StateLock)
            {
                downloadState = state;
                downloadProgress = Math.Max(0f, Math.Min(1f, progress));
                downloadStatus = status;
                downloadError = error;
            }
        }

        private static void SetDownloadFailure(string error)
        {
            SetDownloadState(UpdateDownloadState.Failed, downloadProgress, error, error);
            Logger.Warning(error);
        }

        private static void RestorePendingUpdateState()
        {
            string targetPath = GetTargetAssemblyPath();
            if (string.IsNullOrEmpty(targetPath)) return;
            string stagedPath = GetPendingUpdatePath(targetPath);
            if (!File.Exists(stagedPath)) return;

            lock (StateLock)
            {
                pendingUpdatePath = stagedPath;
                pendingVersionString = pendingVersionString ?? string.Empty;
                downloadState = UpdateDownloadState.ReadyToRestart;
                downloadProgress = 1f;
                downloadStatus = "已有下载完成的更新等待重启。";
            }
        }

        private static string GetPendingUpdatePath(string targetPath)
        {
            return targetPath + ".pending";
        }

        private static string GetTargetAssemblyPath()
        {
            try
            {
                string path = typeof(UpdateChecker).Assembly.Location;
                return string.IsNullOrEmpty(path) ? string.Empty : Path.GetFullPath(path);
            }
            catch
            {
                return string.Empty;
            }
        }

        private static HttpClient CreateHttpClient()
        {
            HttpClient client = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(10)
            };
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("BBPCUpdateChecker", "1.0"));
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            return client;
        }
    }
}
