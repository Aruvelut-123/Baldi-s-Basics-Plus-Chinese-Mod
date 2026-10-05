using BBPC.API;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.UI;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
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
        private static GameObject? dummyReminder;
        private static Canvas? pageCanvas;
        private static GraphicRaycaster? pageRaycaster;
        private static GraphicRaycaster? previousRaycaster;
        private static CursorInitiator? fallbackCursorInitiator;
        private static GameObject? previousMenuObject;
        private static GameObject? previousOptionsObject;
        private static bool pageOpen;
        private static PageMode pageMode;
        private static TextMeshProUGUI? titleText;
        private static TextMeshProUGUI? versionText;
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
            ShowPage(PageMode.RestartRequired);
        }

        private static async void CheckUpdatesAsync()
        {
            await UpdateChecker.CheckForUpdates();
            RefreshMenuReminder();
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

            TextMeshProUGUI? textComponent = reminderObject.GetComponent<TextMeshProUGUI>();
            if (textComponent == null) return;

            bool canOpenPage = UpdateChecker.IsUpdateAvailable || UpdateChecker.HasPendingRestart ||
                               RestartManager.IsLanguageRestartRequested;
            if (!canOpenPage)
            {
                textComponent.ApplyLocalization(ReminderLocalizationKey, true);
                textComponent.raycastTarget = false;
                ReminderButtonMarker? marker = reminderObject.GetComponent<ReminderButtonMarker>();
                if (marker != null)
                {
                    UnityEngine.Object.Destroy(marker.GetComponent<StandardMenuButton>());
                    UnityEngine.Object.Destroy(marker);
                }
                return;
            }

            if (RestartManager.IsLanguageRestartRequested || UpdateChecker.HasPendingRestart)
            {
                textComponent.text = GetText(RestartLocalizationKey, "需要重启");
            }
            else
            {
                textComponent.ApplyLocalization(UpdateLocalizationKey, true);
            }
            textComponent.raycastTarget = true;

            StandardMenuButton? button = reminderObject.GetComponent<StandardMenuButton>();
            ReminderButtonMarker? ownedMarker = reminderObject.GetComponent<ReminderButtonMarker>();
            if (button == null)
            {
                button = reminderObject.AddComponent<StandardMenuButton>();
                button.InitializeAllEvents();
                button.underlineOnHigh = true;
                button.text = textComponent;
                ownedMarker = reminderObject.AddComponent<ReminderButtonMarker>();
            }

            if (ownedMarker != null)
            {
                button.OnPress.RemoveAllListeners();
            }
            button.OnPress.AddListener(OpenUpdatePage);
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
                if (CursorController.Instance != null)
                {
                    CursorController.Instance.graphicRaycaster = pageRaycaster;
                }
                else
                {
                    fallbackCursorInitiator = UIHelpers.AddCursorInitiatorToCanvas(pageCanvas);
                }
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
            pageCanvas.overrideSorting = true;
            pageCanvas.sortingOrder = 500;
            pageRaycaster = pageCanvas.GetComponent<GraphicRaycaster>();
            pageCanvas.gameObject.AddComponent<UpdaterPageDriver>();

            CreateModalBackground(pageCanvas.transform);
            titleText = CreateText("UpdateTitle", string.Empty, new Vector3(240f, 38f),
                BaldiFonts.ComicSans36, new Vector2(450f, 44f), TextAlignmentOptions.Center);
            versionText = CreateText("UpdateVersion", string.Empty, new Vector3(240f, 78f),
                BaldiFonts.ComicSans24, new Vector2(450f, 32f), TextAlignmentOptions.Center);
            releaseNotesText = CreateText("UpdateNotes", string.Empty, new Vector3(240f, 132f),
                BaldiFonts.ComicSans18, new Vector2(430f, 120f), TextAlignmentOptions.TopLeft);
            releaseNotesText.enableWordWrapping = true;
            releaseNotesText.overflowMode = TextOverflowModes.Ellipsis;
            statusText = CreateText("UpdateStatus", string.Empty, new Vector3(240f, 252f),
                BaldiFonts.ComicSans18, new Vector2(450f, 32f), TextAlignmentOptions.Center);
            warningText = CreateText("UpdateWarning", string.Empty, new Vector3(240f, 274f),
                BaldiFonts.ComicSans18, new Vector2(450f, 24f), TextAlignmentOptions.Center);
            warningText.color = Color.red;

            activeBarSprite = FindSprite("Bar");
            inactiveBarSprite = FindSprite("BarTransparent");
            progressBars = CreateProgressBar();

            primaryButton = CreateButton("UpdatePrimary", new Vector3(125f, 320f), new Vector2(190f, 36f),
                OnPrimaryPressed);
            secondaryButton = CreateButton("UpdateSecondary", new Vector3(355f, 320f), new Vector2(190f, 36f),
                OnSecondaryPressed);
        }

        private static void CreateModalBackground(Transform parent)
        {
            GameObject backgroundObject = new GameObject("ModalBackground", typeof(RectTransform), typeof(Image));
            backgroundObject.transform.SetParent(parent, false);
            backgroundObject.transform.SetAsFirstSibling();
            RectTransform rect = backgroundObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image background = backgroundObject.GetComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.88f);
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
            else if (pageMode == PageMode.RestartRequired || pageMode == PageMode.UpdateReady || pageMode == PageMode.Error)
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
            if (titleText == null || versionText == null || releaseNotesText == null || statusText == null ||
                warningText == null || primaryButton == null || secondaryButton == null) return;

            warningText.gameObject.SetActive(false);
            UpdateReleaseInfo? release = UpdateChecker.LatestRelease;
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
                      GetText("BBPC_Update_NewVersion", "待安装版本") + $": {pendingVersion}";
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

            if (isDownloading)
            {
                titleText.text = GetText("BBPC_Update_DownloadingTitle", "正在下载更新");
                versionText.text = GetText("BBPC_Update_CurrentVersion", "当前版本") + $": {UpdateChecker.CurrentVersionString}    " +
                                   GetText("BBPC_Update_NewVersion", "最新版本") + $": {UpdateChecker.LatestVersionString}";
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
            return notes.Length <= 3500 ? notes : notes.Substring(0, 3500) + "\n…";
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
            if (pageMode == PageMode.UpdateReady || pageMode == PageMode.Error)
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

            if (CursorController.Instance != null && previousRaycaster != null)
            {
                CursorController.Instance.graphicRaycaster = previousRaycaster;
            }
            if (fallbackCursorInitiator != null)
            {
                UnityEngine.Object.Destroy(fallbackCursorInitiator);
                fallbackCursorInitiator = null;
            }

            previousRaycaster = null;
            pageOpen = false;
            if (pageCanvas != null) pageCanvas.gameObject.SetActive(false);

            if (returnToMainMenu)
            {
                ReturnToMainMenu();
            }
            RefreshMenuReminder();
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
