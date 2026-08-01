using BBPC.API;
using HarmonyLib;
using System.Collections.Generic;

namespace BBPC.ExtensionTemplate.Patches
{
    /// <summary>
    /// A compile-checked localization example. It is disabled by default so the
    /// template itself cannot change the game. Remove Prepare after choosing a
    /// real target method and replacing the example object names and keys.
    /// </summary>
    [HarmonyPatch(typeof(OptionsMenu), "Awake")]
    internal static class ExampleLocalizationPatch
    {
        private static readonly IReadOnlyDictionary<string, string> LocalizationKeys =
            new Dictionary<string, string>
            {
                { "ExampleButton", "Example_Button" },
                { "ExampleDescription", "Example_Description" }
            };

        [HarmonyPrepare]
        private static bool Prepare() => false;

        [HarmonyPostfix]
        private static void Postfix(OptionsMenu __instance)
        {
            if (Plugin.IsEnglish)
            {
                return;
            }

            // ApplyLocalizations searches descendants by object name. The final
            // argument forces existing TextLocalizer components to refresh.
            __instance.transform.ApplyLocalizations(LocalizationKeys, forceRefresh: true);

            string label = BBPC.Plugin.Instance.GetTranslationKey(
                "Example_Button",
                "Example button");

            API.Logger.Debug($"Resolved example label for {Plugin.CurrentLanguage}: {label}");
        }
    }
}
