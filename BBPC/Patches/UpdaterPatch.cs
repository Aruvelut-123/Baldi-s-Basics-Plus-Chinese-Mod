using BBPC.API;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.UI;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BBPC.Patches
{
    [ConditionalPatchAlways]
    [HarmonyPatch]
    internal static class UpdaterPatch
    {
        private enum PageMode
        {
            NoUpdate,
            UpdateReady,
            Downloading,
            RestartRequired,
            Error
        }

        private const int ProgressSegmentCount = 55;
        private const string UpdateLocalizationKey = "BBPC_Menu_UpdateAvailable";
        private const string ReminderLocalizationKey = "BBPC_Menu_Reminder";
        private const string RestartLocalizationKey = "BBPC_Menu_RestartRequired";

        private static bool updateChecked;
        private static bool updateCheckInProgress;
        private static GameObject? dummyReminder;
        private static Canvas? pageCanvas;
        private static GraphicRaycaster? pageRaycaster;
        private static GraphicRaycaster? previousRaycaster;
        private static CursorInitiator? pageCursorInitiator;
        private static CursorInitiator? previousCursorInitiator;
        private static GameObject? previousMenuObject;
        private static GameObject? previousOptionsObject;
        private static bool returnToOptionsAfterPage;
        private static bool pageOpen;
        private static PageMode pageMode;
        private static TextMeshProUGUI? titleText;
        private static TextMeshProUGUI? versionText;
        private static TextMeshProUGUI? releaseNotesHeaderText;
        private static TextMeshProUGUI? releaseNotesText;
        private static TextMeshProUGUI? statusText;
        private static TextMeshProUGUI? warningText;
        private static StandardMenuButton? primaryButton;
        private static StandardMenuButton? secondaryButton;
        private static Image[] progressBars = Array.Empty<Image>();
        private static Sprite? activeBarSprite;
        private static Sprite? inactiveBarSprite;

        [HarmonyPatch(typeof(GameObject), "SetActive")]
        private static class SetActivePatch
        {
            [HarmonyPostfix]
            private static void Postfix(GameObject __instance, bool value)
            {
                if (__instance == null || __instance.name != "Menu" || !value) return;

                if (!updateChecked)
                {
                    CheckUpdatesAsync();
                    updateChecked = true;
                }

                Transform? reminderTransform = __instance.transform.Find("Reminder");
                if (reminderTransform != null)
                {
                    ModifyReminderElement(reminderTransform.gameObject);
                }
            }
        }

        [HarmonyPatch(typeof(Transform), "Find")]
        private static class TransformFindPatch
        {
            [HarmonyPrefix]
            private static bool Prefix(Transform __instance, string n, ref Transform __result)
            {
                if (n == "Reminder")
                {
                    StackFrame[]? stackFrames = new StackTrace(true).GetFrames();
                    if (stackFrames != null)
                    {
                        foreach (StackFrame frame in stackFrames)
                        {
                            MethodBase? method = frame.GetMethod();
                            if (method?.DeclaringType == null) continue;

                            string namespaceName = method.DeclaringType.Namespace ?? string.Empty;
                            if (namespaceName.StartsWith("MTM101BaldAPI", StringComparison.Ordinal) ||
                                namespaceName.IndexOf(".MTM101BaldAPI", StringComparison.Ordinal) >= 0 ||
                                method.DeclaringType.FullName?.IndexOf("MTM101BaldAPI", StringComparison.Ordinal) >= 0)
                            {
                                __result = GetDummyReminder(__instance);
                                return false;
                            }
                        }
                    }
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(Transform), "GetChild")]
        private static class TransformGetChildPatch
        {
            [HarmonyPostfix]
            private static void Postfix(ref Transform __result)
            {
                if (__result == null || __result.name != "Reminder") return;

                StackFrame[]? stackFrames = new StackTrace(true).GetFrames();
                if (stackFrames == null) return;

                foreach (StackFrame frame in stackFrames)
                {
                    MethodBase? method = frame.GetMethod();
                    if (method?.DeclaringType == null) continue;

                    string namespaceName = method.DeclaringType.Namespace ?? string.Empty;
                    if (namespaceName.StartsWith("MTM101BaldAPI", StringComparison.Ordinal) ||
                        namespaceName.IndexOf(".MTM101BaldAPI", StringComparison.Ordinal) >= 0 ||
                        method.DeclaringType.FullName?.IndexOf("MTM101BaldAPI", StringComparison.Ordinal) >= 0)
                    {
                        Transform? oldParent = __result.parent;
                        if (oldParent != null) __result = GetDummyReminder(oldParent);
                        return;
                    }
                }
            }
        }

        public static void RequestRestartPrompt()
        {
            RestartManager.RequestLanguageRestart();
            returnToOptionsAfterPage = true;
            ShowPage(PageMode.RestartRequired);
        }

        private static async void CheckUpdatesAsync()
        {
            if (updateCheckInProgress) return;
            updateCheckInProgress = true;
            try
            {
                await UpdateChecker.CheckForUpdates();
            }
            finally
            {
                updateCheckInProgress = false;
                RefreshMenuReminder();
            }
        }

        private static Transform GetDummyReminder(Transform parent)
        {
            if (dummyReminder == null)
            {
                dummyReminder = new GameObject("DummyReminder");
                dummyReminder.AddComponent<RectTransform>();
                TextMeshProUGUI textComponent = dummyReminder.AddComponent<TextMeshProUGUI>();
                textComponent.text = string.Empty;
                textComponent.enabled = false;
                dummyReminder.SetActive(false);
                GameObject.DontDestroyOnLoad(dummyReminder);
            }

            dummyReminder.transform.SetParent(parent, false);
            return dummyReminder.transform;
        }

        private static void ModifyReminderElement(GameObject reminderObject)
        {
            if (reminderObject == null) return;

            TextMeshProUGUI? textComponent = reminderObject.GetComponent<TextMeshProUGUI>() ??
                                             reminderObject.GetComponentInChildren<TextMeshProUGUI>(true);
            if (textComponent == null) return;

            GameObject buttonObject = textComponent.gameObject;
            ReminderButtonMarker? marker = buttonObject.GetComponent<ReminderButtonMarker>();
            if (RestartManager.IsLanguageRestartRequested || UpdateChecker.HasPendingRestart)
            {
                textComponent.text = GetText(RestartLocalizationKey, "需要重启");
            }
            else if (UpdateChecker.IsUpdateAvailable)
            {
                textComponent.ApplyLocalization(UpdateLocalizationKey, true);
            }
            else
            {
                textComponent.ApplyLocalization(ReminderLocalizationKey, true);
            }
            textComponent.raycastTarget = true;

            StandardMenuButton? button = buttonObject.GetComponent<StandardMenuButton>();
            bool createdButton = false;
            if (button == null)
            {
                button = buttonObject.ConvertToButton<StandardMenuButton>(true);
                createdButton = true;
            }
            button.text = textComponent;

            if (marker == null)
            {
                marker = buttonObject.AddComponent<ReminderButtonMarker>();
                marker.originalTag = buttonObject.tag;
                marker.ownsButton = createdButton;
            }

            buttonObject.tag = "Button";
            button.underlineOnHigh = true;
            bool hadListener = marker.listenerAdded;
            if (marker.ownsButton)
            {
                button.OnPress.RemoveAllListeners();
                button.OnPress.AddListener(OpenUpdatePage);
            }
            else if (!hadListener)
            {
                button.OnPress.AddListener(OpenUpdatePage);
            }
            marker.listenerAdded = true;

            textComponent.SetAllDirty();
            Canvas.ForceUpdateCanvases();
            RefreshMenuCursor(buttonObject, createdButton || !hadListener);
        }

        private static void RefreshMenuCursor(GameObject buttonObject, bool forceReinitialize)
        {
            Canvas? canvas = buttonObject.GetComponentInParent<Canvas>();
            GraphicRaycaster? raycaster = canvas?.GetComponent<GraphicRaycaster>();
            CursorController? cursor = CursorController.Instance;
            if (canvas == null || raycaster == null || cursor == null) return;

            cursor.graphicRaycaster = raycaster;
            CursorInitiator? initiator = canvas.GetComponent<CursorInitiator>() ??
                                          canvas.GetComponentInParent<CursorInitiator>();
            if (initiator != null && (forceReinitialize || initiator.currentCursor != cursor))
            {
                initiator.Inititate();
            }
        }

        private static void OpenUpdatePage()
        {
            if (UpdateChecker.HasPendingRestart || RestartManager.IsLanguageRestartRequested)
            {
                ShowPage(PageMode.RestartRequired);
            }
            else if (UpdateChecker.IsUpdateAvailable)
            {
                ShowPage(PageMode.UpdateReady);
            }
            else
            {
                ShowPage(PageMode.NoUpdate);
            }
        }

        private static void ShowPage(PageMode mode)
        {
            EnsurePage();
            if (pageCanvas == null || pageRaycaster == null) return;

            if (!pageOpen)
            {
                previousMenuObject = GameObject.Find("Menu");
                previousOptionsObject = UnityEngine.Object.FindObjectOfType<OptionsMenu>()?.gameObject;
                previousRaycaster = CursorController.Instance?.graphicRaycaster;
                previousCursorInitiator = previousRaycaster?.GetComponent<CursorInitiator>() ??
                                           previousRaycaster?.GetComponentInParent<CursorInitiator>();
                pageOpen = true;
            }

            pageMode = mode;
            pageCanvas.gameObject.SetActive(true);
            UpdatePageContent();
        }

        private static void EnsurePage()
        {
            if (pageCanvas != null) return;

            pageCanvas = UIHelpers.CreateBlankUIScreen("BBPCUpdatePage", true, false);
            pageCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            pageCanvas.worldCamera = Singleton<GlobalCam>.Instance.Cam;
            pageCanvas.planeDistance = 0.31f;
            pageCanvas.overrideSorting = true;
            pageCanvas.sortingOrder = 500;
            pageRaycaster = pageCanvas.GetComponent<GraphicRaycaster>();
            pageCursorInitiator = UIHelpers.AddCursorInitiatorToCanvas(pageCanvas, new Vector2(480f, 360f));
            pageCanvas.gameObject.AddComponent<UpdaterPageDriver>();

            CreatePageBackground(pageCanvas.transform);
            UIHelpers.AddBordersToCanvas(pageCanvas);
            titleText = CreateText("UpdateTitle", string.Empty, new Vector3(240f, 35f),
                BaldiFonts.ComicSans36, new Vector2(450f, 38f), TextAlignmentOptions.Center);
            versionText = CreateText("UpdateVersion", string.Empty, new Vector3(240f, 76f),
                BaldiFonts.ComicSans24, new Vector2(450f, 28f), TextAlignmentOptions.Center);
            releaseNotesHeaderText = CreateText("UpdateNotesHeader", string.Empty, new Vector3(240f, 101f),
                BaldiFonts.ComicSans18, new Vector2(450f, 22f), TextAlignmentOptions.Center);
            releaseNotesText = CreateText("UpdateNotes", string.Empty, new Vector3(240f, 165f),
                BaldiFonts.ComicSans18, new Vector2(430f, 110f), TextAlignmentOptions.TopLeft);
            releaseNotesText.enableWordWrapping = true;
            releaseNotesText.overflowMode = TextOverflowModes.Ellipsis;
            statusText = CreateText("UpdateStatus", string.Empty, new Vector3(240f, 235f),
                BaldiFonts.ComicSans18, new Vector2(450f, 28f), TextAlignmentOptions.Center);
            warningText = CreateText("UpdateWarning", string.Empty, new Vector3(240f, 260f),
                BaldiFonts.ComicSans18, new Vector2(450f, 20f), TextAlignmentOptions.Center);
            warningText.color = Color.red;

            activeBarSprite = FindSprite("Bar");
            inactiveBarSprite = FindSprite("BarTransparent");
            progressBars = CreateProgressBar();

            primaryButton = CreateButton("UpdatePrimary", new Vector3(125f, 326f), new Vector2(190f, 36f),
                OnPrimaryPressed);
            secondaryButton = CreateButton("UpdateSecondary", new Vector3(355f, 326f), new Vector2(190f, 36f),
                OnSecondaryPressed);
        }

        private static void CreatePageBackground(Transform parent)
        {
            GameObject backgroundObject = new GameObject("PageBackground", typeof(RectTransform), typeof(Image));
            backgroundObject.transform.SetParent(parent, false);
            backgroundObject.transform.SetAsFirstSibling();
            RectTransform rect = backgroundObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.one / 2f;
            rect.anchorMax = Vector2.one / 2f;
            rect.sizeDelta = new Vector2(480f, 360f);
            rect.localPosition = Vector3.zero;
            Image background = backgroundObject.GetComponent<Image>();
            background.color = new Color(0.12f, 0.12f, 0.12f, 1f);
            background.raycastTarget = true;
        }

        private static TextMeshProUGUI CreateText(string name, string text, Vector3 position,
            BaldiFonts font, Vector2 sizeDelta, TextAlignmentOptions alignment)
        {
            if (pageCanvas == null) throw new InvalidOperationException("Update page canvas is not initialized.");
            TextMeshProUGUI result = UIHelpers.CreateText<TextMeshProUGUI>(font, text, pageCanvas.transform, position, true);
            result.name = name;
            result.rectTransform.sizeDelta = sizeDelta;
            result.alignment = alignment;
            result.color = Color.white;
            result.raycastTarget = false;
            return result;
        }

        private static StandardMenuButton CreateButton(string name, Vector3 position, Vector2 sizeDelta,
            UnityAction action)
        {
            TextMeshProUGUI text = CreateText(name, string.Empty, position, BaldiFonts.ComicSans24, sizeDelta,
                TextAlignmentOptions.Center);
            text.raycastTarget = true;
            StandardMenuButton button = text.gameObject.ConvertToButton<StandardMenuButton>(true);
            button.underlineOnHigh = true;
            button.OnPress.AddListener(action);
            return button;
        }

        private static Image[] CreateProgressBar()
        {
            if (pageCanvas == null) return Array.Empty<Image>();

            Image[] bars = new Image[ProgressSegmentCount];
            for (int i = 0; i < bars.Length; i++)
            {
                Vector3 position = new Vector3(24f + i * 8f, 285f);
                Image image;
                if (inactiveBarSprite != null)
                {
                    image = UIHelpers.CreateImage(inactiveBarSprite, pageCanvas.transform, position, true);
                }
                else
                {
                    GameObject barObject = new GameObject("ProgressBar" + i, typeof(RectTransform), typeof(Image));
                    barObject.transform.SetParent(pageCanvas.transform, false);
                    barObject.transform.localPosition = new Vector3(-216f + i * 8f, -105f);
                    image = barObject.GetComponent<Image>();
                    image.rectTransform.sizeDelta = new Vector2(7f, 12f);
                }

                image.name = "ProgressBar" + i;
                image.raycastTarget = false;
                bars[i] = image;
            }

            return bars;
        }

        private static Sprite? FindSprite(string name)
        {
            return Resources.FindObjectsOfTypeAll<Sprite>()
                .FirstOrDefault(sprite => sprite != null && string.Equals(sprite.name, name, StringComparison.Ordinal));
        }

        internal static void TickPage()
        {
            if (!pageOpen || pageCanvas == null || !pageCanvas.gameObject.activeSelf) return;

            if (pageMode == PageMode.Downloading)
            {
                UpdateDownloadPageState();
            }
            else if (pageMode == PageMode.NoUpdate && UpdateChecker.IsUpdateAvailable)
            {
                pageMode = PageMode.UpdateReady;
                UpdatePageContent();
            }
            else if (pageMode == PageMode.NoUpdate || pageMode == PageMode.RestartRequired ||
                     pageMode == PageMode.UpdateReady || pageMode == PageMode.Error)
            {
                UpdatePageContent();
            }
        }

        private static void UpdateDownloadPageState()
        {
            UpdateProgressBar(UpdateChecker.DownloadProgress);
            if (statusText != null) statusText.text = UpdateChecker.DownloadStatus;

            switch (UpdateChecker.DownloadState)
            {
                case UpdateDownloadState.ReadyToRestart:
                    pageMode = PageMode.RestartRequired;
                    UpdatePageContent();
                    break;
                case UpdateDownloadState.Failed:
                    pageMode = PageMode.Error;
                    UpdatePageContent();
                    break;
            }
        }

        private static void UpdatePageContent()
        {
            if (titleText == null || versionText == null || releaseNotesHeaderText == null ||
                releaseNotesText == null || statusText == null || warningText == null ||
                primaryButton == null || secondaryButton == null) return;

            warningText.gameObject.SetActive(false);
            UpdateReleaseInfo? release = UpdateChecker.LatestRelease;
            bool isNoUpdate = pageMode == PageMode.NoUpdate;
            bool isRestart = pageMode == PageMode.RestartRequired;
            bool isDownloading = pageMode == PageMode.Downloading;
            bool isError = pageMode == PageMode.Error;

            if (isRestart)
            {
                titleText.text = GetText("BBPC_Update_RestartTitle", "需要重启游戏");
                string pendingVersion = UpdateChecker.PendingVersionString ?? string.Empty;
                versionText.text = string.IsNullOrEmpty(pendingVersion)
                    ? GetText("BBPC_Update_RestartVersion", "设置已更改，需要重启后生效")
                    : GetText("BBPC_Update_CurrentVersion", "当前版本") + $": {UpdateChecker.CurrentVersionString}    " +
                      GetText("BBPC_Update_PendingVersion", "待安装版本") + $": {pendingVersion}";
                releaseNotesHeaderText.text = GetText("BBPC_Update_NotesHeader", "说明");
                releaseNotesText.text = GetText("BBPC_Update_RestartBody", "更新或语言设置已准备完成，重启游戏后将应用更改。");
                statusText.text = UpdateChecker.HasPendingRestart
                    ? UpdateChecker.DownloadStatus
                    : GetText("BBPC_Update_RestartBody", "更新或语言设置已准备完成，重启游戏后将应用更改。");
                warningText.text = UpdateChecker.DownloadWarning ?? string.Empty;
                warningText.gameObject.SetActive(!string.IsNullOrEmpty(warningText.text));
                primaryButton.text.text = GetText("BBPC_Update_RestartNow", "立即重启");
                secondaryButton.text.text = GetText("BBPC_Update_RestartLater", "稍后重启");
                primaryButton.gameObject.SetActive(true);
                secondaryButton.gameObject.SetActive(true);
                SetProgressVisible(UpdateChecker.HasPendingRestart);
                UpdateProgressBar(UpdateChecker.DownloadProgress);
                return;
            }

            if (isNoUpdate)
            {
                titleText.text = GetText("BBPC_Update_LatestTitle", "已是最新版本");
                string latestVersion = UpdateChecker.LatestVersionString;
                versionText.text = GetText("BBPC_Update_CurrentVersion", "当前版本") + $": {UpdateChecker.CurrentVersionString}    " +
                                   GetText("BBPC_Update_NewVersion", "最新版本") + ": " +
                                   (string.IsNullOrEmpty(latestVersion) ? GetText("BBPC_Update_Checking", "检查中…") : latestVersion);
                releaseNotesHeaderText.text = GetText("BBPC_Update_NotesHeader", "说明");
                releaseNotesText.text = GetText("BBPC_Update_NoUpdateBody", "当前没有可用更新。");
                statusText.text = updateCheckInProgress ? GetText("BBPC_Update_Checking", "正在检查更新…") : string.Empty;
                warningText.gameObject.SetActive(false);
                primaryButton.text.text = GetText("BBPC_Update_CheckAgain", "再次检查");
                secondaryButton.text.text = GetText("BBPC_Update_Back", "返回");
                primaryButton.gameObject.SetActive(true);
                secondaryButton.gameObject.SetActive(true);
                SetProgressVisible(false);
                return;
            }

            if (isDownloading)
            {
                titleText.text = GetText("BBPC_Update_DownloadingTitle", "正在下载更新");
                versionText.text = GetText("BBPC_Update_CurrentVersion", "当前版本") + $": {UpdateChecker.CurrentVersionString}    " +
                                   GetText("BBPC_Update_NewVersion", "最新版本") + $": {UpdateChecker.LatestVersionString}";
                releaseNotesHeaderText.text = GetText("BBPC_Update_NotesHeader", "说明");
                releaseNotesText.text = GetText("BBPC_Update_DownloadingBody", "请稍候，更新文件正在下载并校验。");
                statusText.text = UpdateChecker.DownloadStatus;
                primaryButton.gameObject.SetActive(false);
                secondaryButton.gameObject.SetActive(false);
                SetProgressVisible(true);
                UpdateProgressBar(UpdateChecker.DownloadProgress);
                return;
            }

            titleText.text = isError
                ? GetText("BBPC_Update_ErrorTitle", "更新失败")
                : GetText("BBPC_Update_Title", "发现新版本");
            versionText.text = GetText("BBPC_Update_CurrentVersion", "当前版本") + $": {UpdateChecker.CurrentVersionString}    " +
                               GetText("BBPC_Update_NewVersion", "最新版本") + $": {UpdateChecker.LatestVersionString}";
            releaseNotesHeaderText.text = isError
                ? GetText("BBPC_Update_NotesHeader", "说明")
                : GetText("BBPC_Update_ReleaseNotes", "更新日志");
            releaseNotesText.text = isError
                ? (UpdateChecker.DownloadError ?? GetText("BBPC_Update_ErrorBody", "更新失败，请稍后重试。"))
                : BuildReleaseNotes(release);
            statusText.text = isError
                ? GetText("BBPC_Update_ErrorRetry", "可以重试下载，或稍后再更新。")
                : string.Empty;
            primaryButton.text.text = isError
                ? GetText("BBPC_Update_Retry", "重试更新")
                : GetText("BBPC_Update_Now", "立即更新");
            secondaryButton.text.text = GetText("BBPC_Update_Later", "稍后更新");
            primaryButton.gameObject.SetActive(true);
            secondaryButton.gameObject.SetActive(true);
            SetProgressVisible(false);
        }

        private static string BuildReleaseNotes(UpdateReleaseInfo? release)
        {
            if (release == null || string.IsNullOrWhiteSpace(release.Body))
            {
                return GetText("BBPC_Update_NoNotes", "该版本没有提供更新说明。");
            }

            string notes = release.Body.Replace("\r\n", "\n").Trim();
            if (notes.Length > 3500) notes = notes.Substring(0, 3500) + "\n…";
            return ConvertMarkdownForTextMeshPro(notes);
        }

        private static string ConvertMarkdownForTextMeshPro(string markdown)
        {
            string escaped = SecurityElement.Escape(markdown) ?? string.Empty;
            escaped = Regex.Replace(escaped, @"(?m)^\s*#{1,6}\s+(.+?)\s*$", "<b>$1</b>");
            escaped = Regex.Replace(escaped, @"\[([^\]]+)\]\([^\)]+\)", "$1");
            escaped = Regex.Replace(escaped, @"\*\*(.+?)\*\*", "<b>$1</b>");
            escaped = Regex.Replace(escaped, @"(?<!\*)\*([^*\r\n]+)\*(?!\*)", "<i>$1</i>");
            escaped = Regex.Replace(escaped, @"`([^`]+)`", "<color=#CCCCCC>$1</color>");
            escaped = Regex.Replace(escaped, @"(?m)^\s*[-*]\s+", "• ");
            return escaped;
        }

        private static void SetProgressVisible(bool visible)
        {
            foreach (Image bar in progressBars)
            {
                if (bar != null) bar.gameObject.SetActive(visible);
            }
        }

        private static void UpdateProgressBar(float progress)
        {
            int activeCount = Mathf.Clamp(Mathf.FloorToInt(progress * progressBars.Length), 0, progressBars.Length);
            for (int i = 0; i < progressBars.Length; i++)
            {
                Image bar = progressBars[i];
                if (bar == null) continue;
                if (activeBarSprite != null && inactiveBarSprite != null)
                {
                    bar.sprite = i < activeCount ? activeBarSprite : inactiveBarSprite;
                }
                else
                {
                    bar.color = i < activeCount ? Color.white : new Color(1f, 1f, 1f, 0.25f);
                }
            }
        }

        private static void OnPrimaryPressed()
        {
            if (pageMode == PageMode.NoUpdate)
            {
                CheckUpdatesAsync();
            }
            else if (pageMode == PageMode.UpdateReady || pageMode == PageMode.Error)
            {
                pageMode = PageMode.Downloading;
                UpdatePageContent();
                try
                {
                    _ = UpdateChecker.DownloadUpdateAsync();
                }
                catch (Exception ex)
                {
                    API.Logger.Error($"启动更新下载失败：{ex.Message}");
                    pageMode = PageMode.Error;
                    UpdatePageContent();
                }
            }
            else if (pageMode == PageMode.RestartRequired)
            {
                if (RestartManager.PrepareExit())
                {
                    ClosePage(false);
                    Application.Quit();
                }
                else if (statusText != null)
                {
                    statusText.text = GetText("BBPC_Update_RestartError", "无法自动重启，请手动退出并重新启动游戏。");
                }
            }
        }

        private static void OnSecondaryPressed()
        {
            ClosePage(true);
        }

        private static void ClosePage(bool returnToMainMenu)
        {
            if (!pageOpen) return;

            pageOpen = false;
            if (pageCanvas != null) pageCanvas.gameObject.SetActive(false);

            if (previousCursorInitiator != null)
            {
                previousCursorInitiator.Inititate();
            }
            else if (CursorController.Instance != null && previousRaycaster != null)
            {
                CursorController.Instance.graphicRaycaster = previousRaycaster;
            }

            previousRaycaster = null;
            previousCursorInitiator = null;

            bool restoreOptions = returnToMainMenu && returnToOptionsAfterPage;
            if (restoreOptions)
            {
                if (previousOptionsObject != null)
                {
                    previousOptionsObject.SetActive(true);
                }
                previousOptionsObject = null;
                previousMenuObject = null;
            }
            else if (returnToMainMenu)
            {
                ReturnToMainMenu();
            }
            returnToOptionsAfterPage = false;
            if (!restoreOptions)
            {
                RefreshMenuReminder();
            }
        }

        private static void ReturnToMainMenu()
        {
            if (previousOptionsObject != null && previousOptionsObject != pageCanvas?.gameObject)
            {
                previousOptionsObject.SetActive(false);
            }
            if (previousMenuObject != null)
            {
                previousMenuObject.SetActive(true);
            }

            previousOptionsObject = null;
            previousMenuObject = null;
        }

        private static void RefreshMenuReminder()
        {
            GameObject? menuObject = GameObject.Find("Menu");
            Transform? reminder = menuObject?.transform.Find("Reminder");
            if (reminder != null) ModifyReminderElement(reminder.gameObject);
        }

        private static string GetText(string key, string fallback)
        {
            return Plugin.Instance == null ? fallback : Plugin.Instance.GetTranslationKey(key, fallback);
        }

        private sealed class ReminderButtonMarker : MonoBehaviour
        {
            public string originalTag = "Untagged";
            public bool ownsButton;
            public bool listenerAdded;
        }
    }

    internal sealed class UpdaterPageDriver : MonoBehaviour
    {
        private void Update()
        {
            UpdaterPatch.TickPage();
        }
    }
}
