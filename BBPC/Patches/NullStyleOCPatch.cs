using HarmonyLib;
using MTM101BaldAPI.OptionsAPI;
using NULL.Manager;
using System.Reflection;
using TMPro;
using UnityEngine;

namespace BBPC.NULLStyle.Patches
{
    [HarmonyPatch(typeof(NullStyleOptionsCategory))]
    public class NullStyleOptionsPatch
    {
        private static TextLocalizer tooltipLocalizer;

        [HarmonyPatch("Build")]
        [HarmonyPostfix]
        public static void BuildPostfix(NullStyleOptionsCategory __instance)
        {
            Transform viewport = __instance.transform.Find("Viewport");
            if (viewport == null)
            {
                API.Logger.Error("Failed to find Viewport in NullStyleOptionsCategory");
                return;
            }

            Transform container = viewport.Find("OptionsContainer");
            if (container == null)
            {
                API.Logger.Error("Failed to find OptionsContainer");
                return;
            }

            LocalizeToggles(container, __instance);
            LocalizeScrollHint(__instance);
        }

        private static void LocalizeToggles(Transform container, NullStyleOptionsCategory instance)
        {
            string[] toggleNames = {
                "AmbienceToggle",
                "CharactersToggle",
                "AllEventsToggle",
                "ResultsTvToggle",
                "LightGlitchToggle",
                "GameCrashToggle",
                "ExtraFloorsToggle"
            };

            string[] toggleKeys = {
                "NULL_Ambience",
                "NULL_Characters",
                "NULL_AllEvents",
                "NULL_DisableResultsTV",
                "NULL_LightGlitch",
                "NULL_GameCrash",
                "NULL_ExtraFloors"
            };

            string[] tooltipKeys = {
                "NULL_Ambience_Tooltip",
                "NULL_Characters_Tooltip",
                "NULL_AllEvents_Tooltip",
                "NULL_DisableResultsTV_Tooltip",
                "NULL_LightGlitch_Tooltip",
                "NULL_GameCrash_Tooltip",
                "NULL_ExtraFloors_Tooltip"
            };

            for (int i = 0; i < toggleNames.Length; i++)
            {
                Transform toggleTransform = container.Find(toggleNames[i]);
                if (toggleTransform == null) continue;

                GameObject toggleObj = toggleTransform.gameObject;
                MenuToggle toggle = toggleObj.GetComponent<MenuToggle>();
                if (toggle == null) continue;

                Transform textTransform = toggleTransform.Find("ToggleText");
                if (textTransform != null)
                {
                    TextMeshProUGUI tmp = textTransform.GetComponent<TextMeshProUGUI>();
                    if (tmp != null)
                    {
                        var existingTL = tmp.gameObject.GetComponent<TextLocalizer>();
                        if (existingTL != null) Object.Destroy(existingTL);

                        TextLocalizer textTL = tmp.gameObject.AddComponent<TextLocalizer>();
                        textTL.key = toggleKeys[i];
                        textTL.RefreshLocalization();
                    }
                }

                Transform hotSpot = toggleTransform.Find("HotSpot");
                if (hotSpot != null)
                {
                    StandardMenuButton btn = hotSpot.GetComponent<StandardMenuButton>();
                    if (btn != null)
                    {
                        var existingTooltip = btn.gameObject.GetComponent<TextLocalizer>();
                        if (existingTooltip != null) Object.Destroy(existingTooltip);

                        tooltipLocalizer = btn.gameObject.AddComponent<TextLocalizer>();
                        tooltipLocalizer.key = tooltipKeys[i];
                        tooltipLocalizer.RefreshLocalization();

                        var method = typeof(NullStyleOptionsCategory).GetMethod("AddTooltip",
                            BindingFlags.NonPublic | BindingFlags.Instance,
                            null,
                            new System.Type[] { typeof(StandardMenuButton), typeof(string) },
                            null);

                        if (method != null)
                        {
                            string translatedTooltip = BBPC.Plugin.Instance.GetTranslationKey(tooltipKeys[i], GetDefaultTooltip(i));
                            method.Invoke(instance, new object[] { btn, translatedTooltip });
                        }
                    }
                }
            }
        }

        private static void LocalizeScrollHint(NullStyleOptionsCategory instance)
        {
            Transform hintTransform = instance.transform.Find("ScrollHint");
            if (hintTransform == null) return;

            TextMeshProUGUI hintText = hintTransform.GetComponent<TextMeshProUGUI>();
            if (hintText != null)
            {
                var existingTL = hintText.gameObject.GetComponent<TextLocalizer>();
                if (existingTL != null) Object.Destroy(existingTL);

                TextLocalizer hintTL = hintText.gameObject.AddComponent<TextLocalizer>();
                hintTL.key = "NULL_ScrollHint";
                hintTL.RefreshLocalization();
            }
        }

        private static string GetDefaultTooltip(int index)
        {
            string[] defaultTooltips = {
                "There is no lighting in the school. Suspenseful background ambient track plays.\n<color=#008000ff>Default is true.",
                "Oh no! Null called other characters to help!\n<color=#008000ff>Default is false.",
                "Oh no! All random events are happening at once!\n<color=#008000ff>Default is false.",
                "If enabled, the score screen in the elevator will be hidden and the animation skipped.\n<color=#008000ff>Default is true.",
                "If enabled, lights near the boss will flicker.\n<color=#008000ff>Default is false.",
                "If enabled, the game will force close when you are caught by NULL.\n<color=#008000ff>Default is true.",
                "Adds two extra floors (F4 and F5) before the boss fight.\n<color=#008000ff>Default is false."
            };
            return index < defaultTooltips.Length ? defaultTooltips[index] : "";
        }
    }
}