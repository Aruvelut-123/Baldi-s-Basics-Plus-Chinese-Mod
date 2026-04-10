using BBPC.API;
using BepInEx;
using HarmonyLib;
using MTM101BaldAPI;
using System.Collections.Generic;
using UnityEngine;

namespace BBPC.ExtensionTemplate
{
    // =====================================================================
    // BBPC Extension Template
    // =====================================================================
    // This template demonstrates how to create a BBPC extension plugin.
    // Extensions depend on the main BBPC plugin and can use its API to:
    //   - Translate UI text via GetTranslationKey()
    //   - Apply batch localizations via Transform.ApplyLocalizations()
    //   - Access language/config state via BBPCTemp and ConfigManager
    //   - Use the Logger for structured logging
    //   - Use Harmony patches with BepInEx conditional patching
    //
    // To create a new extension:
    //   1. Copy this template
    //   2. Rename the namespace, GUID, ModName in your own Temp class
    //   3. Add your Harmony patches under the Patches/ folder
    //   4. Add your mod-specific DLL references to the .csproj
    // =====================================================================

    [BepInPlugin(API.BBPCTemp.ModGUID, API.BBPCTemp.ModName, API.BBPCTemp.ModVersion)]
    // Hard dependency on BaldAPI — required for asset loading & conditional patching
    [BepInDependency("mtm101.rulerp.bbplus.baldidevapi", BepInDependency.DependencyFlags.HardDependency)]
    // Hard dependency on the main BBPC plugin — provides the translation API
    [BepInDependency("com.baymaxawa.bbpc", BepInDependency.DependencyFlags.HardDependency)]
    // Add your mod-specific dependencies here, for example:
    // [BepInDependency("your.mod.guid", BepInDependency.DependencyFlags.HardDependency)]
    [BepInProcess("BALDI.exe")]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance { get; private set; } = null!;
        private Harmony? harmonyInstance = null!;

        // ---- Language state (synced from main BBPC plugin) ----
        // is_english: true when the user has selected English (skips localization patches)
        public static bool is_english = false;
        // current_lang: the currently selected language folder name (e.g. "SChinese", "TChinese", "English")
        public static string current_lang = "SChinese";

        private void Awake()
        {
            Instance = this;

            // ---- Logger API ----
            // Initialize the extension's own logger with BepInEx's ManualLogSource.
            // Each extension has its own Logger class (in its own namespace) so log
            // entries are tagged with the correct plugin name.
            API.Logger.Init(Logger);

            // Logger provides these log levels:
            //   Logger.Debug(msg)   — verbose debug info, only shown when logging is enabled
            //   Logger.Info(msg)    — general info messages
            //   Logger.Warning(msg) — warnings
            //   Logger.Error(msg)   — errors (always logged, never suppressed)
            //   Logger.ForceInfo(msg)    — info that bypasses IsLoggingEnabled() check
            //   Logger.ForceWarning(msg) — warning that bypasses IsLoggingEnabled() check
            API.Logger.Info($"Extension {API.BBPCTemp.ModName} is initializing...");

            // ---- Config / Language State ----
            // Read the current language setting from the main BBPC plugin's config.
            // BBPCTemp.is_eng: static bool set by the main plugin when language is "English"
            // ConfigManager.currect_lang: the BepInEx ConfigEntry<string> for the selected language
            is_english = BBPC.API.BBPCTemp.is_eng;
            current_lang = ConfigManager.currect_lang.Value;

            // ---- Harmony Patching ----
            // Create a Harmony instance with your extension's unique GUID.
            // PatchAllConditionals() applies all [HarmonyPatch] classes that pass
            // their [ConditionalPatch] checks (from MTM101BaldAPI).
            Harmony harmony = new Harmony(API.BBPCTemp.ModGUID);
            harmony.PatchAllConditionals();

            API.Logger.Info($"Extension {API.BBPCTemp.ModName} loaded successfully!");
        }

        void OnDestroy()
        {
            // Clean up Harmony patches when the plugin is destroyed
            if (harmonyInstance != null)
            {
                harmonyInstance.UnpatchSelf();
                harmonyInstance = null;
            }
        }

        // =====================================================================
        // API Usage Examples (for reference — call these from your Harmony patches)
        // =====================================================================

        /// <summary>
        /// Example: Translate a single key using the main BBPC plugin's GetTranslationKey API.
        ///
        /// GetTranslationKey searches all JSON language files under Language/{lang}/ for
        /// a matching "key" and returns its "value". Falls back to default_obj if not found.
        ///
        /// Parameters:
        ///   key         — the localization key to look up (e.g. "MyMod_ButtonLabel")
        ///   default_obj — fallback string if the key is not found
        ///   lang        — (optional) language folder override, defaults to current language
        ///   custom_lang — (optional) if true, uses the lang parameter as-is instead of
        ///                 overriding with the user's configured language
        ///
        /// JSON format expected in Language/{lang}/*.json:
        /// {
        ///   "items": [
        ///     { "key": "MyMod_ButtonLabel", "value": "我的按钮" },
        ///     { "key": "MyMod_Tooltip",     "value": "点击我" }
        ///   ]
        /// }
        /// </summary>
        public static void ExampleTranslateSingleKey()
        {
            // Basic usage — look up a key, fall back to English default
            string translated = BBPC.Plugin.Instance.GetTranslationKey(
                "MyMod_ButtonLabel",  // localization key
                "My Button"           // fallback if key not found
            );
            API.Logger.Debug($"Translated text: {translated}");

            // With explicit language override (useful for multi-language comparison)
            string tchinese = BBPC.Plugin.Instance.GetTranslationKey(
                "MyMod_ButtonLabel",
                "My Button",
                "TChinese",  // force Traditional Chinese
                true         // custom_lang=true to use this lang instead of user setting
            );
            API.Logger.Debug($"Traditional Chinese text: {tchinese}");
        }

        /// <summary>
        /// Example: Batch-localize child GameObjects using Transform.ApplyLocalizations().
        ///
        /// This extension method (from BBPC.API) searches children of a Transform by name
        /// and adds a TextLocalizer component with the mapped localization key.
        ///
        /// Parameters:
        ///   localizationKeys — Dictionary mapping child GameObject names to localization keys
        ///   recursive        — if true, searches all descendants (not just direct children)
        ///
        /// This is the preferred way to localize menus and UI panels with multiple text elements.
        /// </summary>
        public static void ExampleBatchLocalization(Transform parentTransform)
        {
            if (is_english) return; // Skip localization when language is English

            // Map child GameObject names → localization keys
            var keys = new Dictionary<string, string>()
            {
                { "StartButton",    "MyMod_Start" },
                { "SettingsButton", "MyMod_Settings" },
                { "QuitButton",     "MyMod_Quit" }
            };

            // Apply to all matching children (recursive=true searches nested children too)
            parentTransform.ApplyLocalizations(keys, true);
        }

        /// <summary>
        /// Example: Conditionally skip localization patches when language is English.
        ///
        /// Most extension patches should check is_english before applying translations.
        /// When the user selects English, the original game text is already correct,
        /// so patching would be unnecessary overhead.
        /// </summary>
        public static void ExampleConditionalPatch()
        {
            if (is_english)
            {
                API.Logger.Debug("English selected, skipping localization patch");
                return;
            }

            // ... apply your localization logic here ...
        }

        /// <summary>
        /// Example: Using Logger for different severity levels.
        ///
        /// All log messages include the calling class and method name automatically
        /// via StackFrame reflection: [ClassName.MethodName] message
        /// </summary>
        public static void ExampleLogging()
        {
            // Debug — verbose, only when logging enabled
            API.Logger.Debug("Loading translation file...");

            // Info — general status updates
            API.Logger.Info("Translation applied successfully");

            // Warning — non-critical issues
            API.Logger.Warning("Translation key 'MyMod_X' not found, using fallback");

            // Error — always logged, never suppressed by IsLoggingEnabled()
            API.Logger.Error("Failed to load translation file: file not found");

            // ForceInfo/ForceWarning — bypass IsLoggingEnabled() check
            API.Logger.ForceInfo("Plugin version: " + API.BBPCTemp.ModVersion);
        }

        /// <summary>
        /// Helper to destroy a GameObject (useful from static Harmony patch methods
        /// that cannot call Destroy directly since it's an instance method on MonoBehaviour).
        /// </summary>
        public void des(GameObject obj)
        {
            Destroy(obj);
        }
    }
}
