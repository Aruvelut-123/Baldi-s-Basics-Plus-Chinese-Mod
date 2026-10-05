using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.OptionsAPI;
using MTM101BaldAPI.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;

namespace BBPC.API
{
    public static class ConfigManager
    {
        public static ConfigEntry<bool> EnableTextures { get; private set; } = null!;
        public static ConfigEntry<bool> EnableSounds { get; private set; } = null!;
        public static ConfigEntry<bool> EnableLogging { get; private set; } = null!;
#if DEBUG
        public static ConfigEntry<bool> EnableDevMode { get; private set; } = null!;
#endif
        public static ConfigEntry<string> currect_lang { get; set; } = null!;
        public static ConfigEntry<bool> EnableFontReplacement { get; set; } = null!;
        public static ConfigEntry<string> overrideFontPath { get; private set; } = null!;

        private static ManualLogSource _logger = null!;

        public static void Initialize(BaseUnityPlugin plugin, ManualLogSource logger)
        {
            _logger = logger;

            EnableTextures = plugin.Config.Bind("General", "Enable Textures", true, "Enable or disable texture replacement.");
            EnableSounds = plugin.Config.Bind("General", "Enable Sounds", true, "Enable or disable sound replacement.");
            EnableLogging = plugin.Config.Bind("General", "Enable Logging", true, "Enable or disable logging.");
#if DEBUG
            EnableDevMode = plugin.Config.Bind("Development", "Enable Dev Mode", false, "Enable development mode (scans and exports new posters). DISABLE FOR RELEASE!");
#endif
            currect_lang = plugin.Config.Bind("General", "Currect Language", "SChinese", "The Language that currectly using.");
            EnableFontReplacement = plugin.Config.Bind("General", "Enable Font Replacement", true, "Enable or disable font replacement feature. If enabled, then override font path must be set.");
            overrideFontPath = plugin.Config.Bind("General", "Override Font Path", "ch2", "Start from the mod asset folder, define a tmp font file path to load and replace in game font");
            
            _logger.LogInfo("Config loaded successfully.");
        }

        public static bool AreTexturesEnabled()
        {
            return EnableTextures.Value;
        }

        public static bool IsLoggingEnabled()
        {
            return EnableLogging.Value;
        }

        public static bool AreSoundsEnabled()
        {
            return EnableSounds.Value;
        }

        public static bool IsFontReplacementEnabled()
        {
            return EnableFontReplacement.Value;
        }
#if DEBUG
        public static bool IsDevModeEnabled()
        {
            return EnableDevMode.Value;
        }
#endif
    }

    public class BBPCOptionsCategory : CustomOptionsCategory
    {
        public List<string> languages = [];
        public TextMeshProUGUI LangTip = null!;
        public TextMeshProUGUI CurrectLanguage = null!;
        public TextMeshProUGUI LangNotice = null!;
        private int index;
        private MenuToggle toggleTextureReplace = null!;
        private string current = null!;
        private string? lastLangTipText;
        private string? lastCurrentLanguageText;

        public override void Build()
        {
            SetupTooltipHotspots();
            languages.Clear();
            string modPath = AssetLoader.GetModPath(Plugin.Instance);
            string langsPath = Path.Combine(modPath, "Language");
            if (!string.IsNullOrEmpty(modPath) && Directory.Exists(langsPath))
            {
                IEnumerable<string> languageDirectories = Directory.EnumerateDirectories(langsPath)
                    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase);

                foreach (string directoryPath in languageDirectories)
                {
                    string directoryName = Path.GetFileName(directoryPath);
                    if (!string.IsNullOrEmpty(directoryName))
                    {
                        languages.Add(directoryName);
                    }
                }

                API.Logger.Debug($"Found {languages.Count} language directories: {string.Join(", ", languages)}");
            }
            current = ConfigManager.currect_lang.Value;
            if (languages.Count == 0)
            {
                languages.Add(current);
                API.Logger.Warning($"No language directories were found in: {langsPath}");
            }
            API.Logger.Debug("Current language: " + current);
            API.Logger.Debug($"Language list: {string.Join(", ", languages)}");
            index = languages.IndexOf(current);
            if (index < 0)
            {
                index = 0;
                current = languages[0];
            }

            string langNotice = Plugin.Instance.GetTranslationKey("BBPC_LangNotice", "注意：繁體中文目前處於測試階段\n如遇到問題請提出！", "TChinese", true);
            LangNotice = CreateText("LangNotice", langNotice, new Vector2(0, 65), BaldiFonts.ComicSans18, TextAlignmentOptions.Center, new Vector2(300, 50), Color.red);
            LangTip = CreateText("LangTip", "Please select the language\nthat you want to apply.", new Vector2(0, -30), BaldiFonts.ComicSans24, TextAlignmentOptions.Center, Vector2.one, Color.black);
            LangTip.ApplyLocalization("BBPC_LangTip", true);
            CurrectLanguage = CreateText("CurrectLanguage", Plugin.Instance.GetTranslationKey("BBPC_LangName", current), new Vector2(0, 30), BaldiFonts.ComicSans24, TextAlignmentOptions.Center, new Vector2(50, 10), Color.black);
            StandardMenuButton previousButton = Plugin.CreateButtonWithSprite("PreviousButton", Plugin.LoadAsset<Sprite>("MenuArrowSheet_2"), Plugin.LoadAsset<Sprite>("MenuArrowSheet_0"), transform, new Vector3(-150, 30));
            previousButton.OnPress = new UnityEngine.Events.UnityEvent();
            previousButton.OnPress.AddListener(() => changeLang(false));
            previousButton.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
            StandardMenuButton nextButton = Plugin.CreateButtonWithSprite("NextButton", Plugin.LoadAsset<Sprite>("MenuArrowSheet_3"), Plugin.LoadAsset<Sprite>("MenuArrowSheet_1"), transform, new Vector3(150, 30));
            nextButton.OnPress = new UnityEngine.Events.UnityEvent();
            nextButton.OnPress.AddListener(() => changeLang(true));
            nextButton.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
            toggleTextureReplace = CreateToggle("TextureToggleButton", Plugin.Instance.GetTranslationKey("BBPC_ToggleTexture", "Enable Texture Replacement"), ConfigManager.EnableTextures.Value, new Vector2(50, -75), 250);
            StandardMenuButton applyButton = CreateApplyButton(() => { refresh_localization(); });
            AddTooltip(applyButton, Plugin.Instance.GetTranslationKey("BPPC_Apply_Tooltip", "Apply and restart"));
            CurrectLanguage.gameObject.SetActive(true);
            lastLangTipText = null;
            lastCurrentLanguageText = null;
        }

        private void changeLang(bool is_next)
        {
            if (languages.Count == 0)
            {
                return;
            }

            if (is_next) index++;
            else index--;
            if (index < 0) index = languages.Count - 1;
            if (index >= languages.Count) index = 0;
            current = languages[index];
            CurrectLanguage.text = Plugin.Instance.GetTranslationKey("BBPC_LangName", current, current, true);
            API.Logger.Debug("index: " + index.ToString() + "\ncurrent: " + current + "\nlanguages[index]: " + languages[index] + "\nCurrectLanguage.text: " + CurrectLanguage.text);
        }

        private void refresh_localization()
        {
            bool need_restart = false;
            if (ConfigManager.currect_lang.Value != current)
            {
                ConfigManager.currect_lang.Value = current;
                need_restart = true;
            }
            if (ConfigManager.EnableTextures.Value != toggleTextureReplace.Value)
            {
                ConfigManager.EnableTextures.Value = toggleTextureReplace.Value;
                need_restart = true;
            }
            if (need_restart) Application.Quit();
        }

        private static void RefreshAutoSizeIfTextChanged(TextMeshProUGUI? text, ref string? previousText)
        {
            if (text == null || string.Equals(previousText, text.text, StringComparison.Ordinal))
            {
                return;
            }

            text.autoSizeTextContainer = false;
            text.autoSizeTextContainer = true;
            previousText = text.text;
        }

        private void Update()
        {
            RefreshAutoSizeIfTextChanged(LangTip, ref lastLangTipText);
            RefreshAutoSizeIfTextChanged(CurrectLanguage, ref lastCurrentLanguageText);
        }
    }
}
