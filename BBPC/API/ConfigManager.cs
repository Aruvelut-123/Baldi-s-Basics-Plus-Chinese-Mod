using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.OptionsAPI;
using MTM101BaldAPI.UI;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BBPC.API
{
    public static class ConfigManager
    {
        public static ConfigEntry<bool> EnableTextures { get; private set; } = null!;
        public static ConfigEntry<bool> EnableLogging { get; private set; } = null!;
        public static ConfigEntry<bool> EnableDevMode { get; private set; } = null!;
        public static ConfigEntry<bool> is_dev { get; private set; } = null!;
        public static ConfigEntry<bool> is_beta { get; private set; } = null!;
        public static ConfigEntry<bool> is_alpha { get; private set; } = null!;
        public static ConfigEntry<string> currect_lang { get; set; } = null!;
        public static ConfigEntry<bool> show_watermark { get; private set; } = null!;

        private static ManualLogSource _logger = null!;

        public static void Initialize(BaseUnityPlugin plugin, ManualLogSource logger)
        {
            _logger = logger;

            is_dev = plugin.Config.Bind("Build Check", "is_dev", false, "Check this if is dev build.");
            is_beta = plugin.Config.Bind("Build Check", "is_beta", false, "Check this if is beta build.");
            is_alpha = plugin.Config.Bind("Build Check", "is_alpha", false, "Check this if is alpha build.");
            EnableTextures = plugin.Config.Bind("General", "Enable Textures", true, "Enable or disable texture replacement.");
            EnableLogging = plugin.Config.Bind("General", "Enable Logging", false, "Enable or disable logging.");
            EnableDevMode = plugin.Config.Bind("Development", "Enable Dev Mode", false, "Enable development mode (scans and exports new posters). DISABLE FOR RELEASE!");
            currect_lang = plugin.Config.Bind("General", "Currect Language", "SChinese", "The Language that currectly using.");
            show_watermark = plugin.Config.Bind("General", "Disable Watermark", false, "Enable or disable watermark display.");

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

        public static bool IsDevModeEnabled()
        {
            return EnableDevMode.Value;
        }
    }

    public class BBPCOptionsCategory : CustomOptionsCategory
    {
        public List<string> languages = [];
        public TextMeshProUGUI LangTip = null!;
        public TextMeshProUGUI CurrectLanguage = null!;
        public TextMeshProUGUI LangNotice = null!;
        private int index;
        private StandardMenuButton previousButton = null!;
        private StandardMenuButton nextButton = null!;
        private MenuToggle toggleTextureReplace = null!;
        private string current = null!;

        public override void Build()
        {
            string mod_path = AssetLoader.GetModPath(Plugin.Instance);
            string langsPath = Path.Combine(mod_path, "Language");
            string langPath = Path.Combine(langsPath, ConfigManager.currect_lang.Value);
            if (mod_path != null && mod_path != "")
            {
                if (Directory.Exists(langsPath))
                {
                    DirectoryInfo directoryInfo = new DirectoryInfo(langsPath);
                    DirectoryInfo[] directories = directoryInfo.GetDirectories();
                    API.Logger.Debug("Find "+directories.Length.ToString()+" directories: " + directories.ToArray().ToString());
                    foreach (DirectoryInfo directory in directories)
                    {
                        directory.Refresh();
                        API.Logger.Debug("Find directory: " + directory.ToString());
                        API.Logger.Debug("Add " + directory.Name + " to language list");
                        languages.Add(directory.Name);
                    }
                }
            }
            current = ConfigManager.currect_lang.Value;
            API.Logger.Debug("Current language: " + current);
            API.Logger.Debug("Language list: " + languages.ToArray().ToString());
            index = languages.IndexOf(current);
            LangTip = CreateText("LangTip", "Please select the language\nthat you want to apply.", new Vector2(0, -30), BaldiFonts.ComicSans24, TextAlignmentOptions.Center, Vector2.one, Color.black);
            TextLocalizer localizer = LangTip.gameObject.AddComponent<TextLocalizer>();
            localizer.key = "BBPC_LangTip";
            localizer.RefreshLocalization();
            CurrectLanguage = CreateText("CurrectLanguage", Plugin.Instance.GetTranslationKey("BBPC_LangName", current), new Vector2(0, 30), BaldiFonts.ComicSans24, TextAlignmentOptions.Center, new Vector2(50, 10), Color.black);
            previousButton = Plugin.CreateButtonWithSprite("PreviousButton", Plugin.LoadAsset<Sprite>("MenuArrowSheet_2"), Plugin.LoadAsset<Sprite>("MenuArrowSheet_0"), transform, new Vector3(-150, 30));
            previousButton.OnPress = new UnityEngine.Events.UnityEvent();
            previousButton.OnPress.AddListener(() => changeLang(false));
            previousButton.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
            nextButton = Plugin.CreateButtonWithSprite("NextButton", Plugin.LoadAsset<Sprite>("MenuArrowSheet_3"), Plugin.LoadAsset<Sprite>("MenuArrowSheet_1"), transform, new Vector3(150, 30));
            nextButton.OnPress = new UnityEngine.Events.UnityEvent();
            nextButton.OnPress.AddListener(() => changeLang(true));
            nextButton.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
            toggleTextureReplace = CreateToggle("TextureToggleButton", Plugin.Instance.GetTranslationKey("BBPC_ToggleTexture", "Enable Texture Replacement"), ConfigManager.EnableTextures.Value, new Vector2(50, -75), 250);
            StandardMenuButton applyButton = CreateApplyButton(() => { refresh_localization(); });
            applyButton.transitionOnPress = true;
            applyButton.transitionTime = 0.0167f;
            applyButton.transitionType = UiTransition.Dither;
            AddTooltip(applyButton, Plugin.Instance.GetTranslationKey("BPPC_Apply_Tooltip", "Apply and restart"));
            CurrectLanguage.gameObject.SetActive(true);
        }

        private void changeLang(bool is_next)
        {
            if (is_next) index++;
            else index--;
            if (index < 0) index = languages.Count - 1;
            if (index >= languages.Count) index = 0;
            current = languages[index];
            CurrectLanguage.text = Plugin.Instance.GetTranslationKey("BBPC_LangName", current, current, true);
            if (CurrectLanguage.text == "繁體中文")
            {
                AddTooltip(nextButton, Plugin.Instance.GetTranslationKey("BBPC_LangNotice", "", current, true));
                AddTooltip(previousButton, Plugin.Instance.GetTranslationKey("BBPC_LangNotice", "", current, true));
            }
            else
            {
                AddTooltip(nextButton, "");
                AddTooltip(previousButton, "");
            }
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
            if (!need_restart)
            {
                Plugin.update_watermark();
            }
            if (need_restart) Application.Quit();
        }

        void Update()
        {
            if (LangTip != null)
            {
                LangTip.autoSizeTextContainer = false;
                LangTip.autoSizeTextContainer = true;
            }
            if (CurrectLanguage != null)
            {
                CurrectLanguage.autoSizeTextContainer = false;
                CurrectLanguage.autoSizeTextContainer = true;
            }
        }
    }
} 