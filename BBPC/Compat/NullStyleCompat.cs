using BepInEx.Bootstrap;
using HarmonyLib;
using MTM101BaldAPI.OptionsAPI;
using System;
using System.Reflection;
using TMPro;
using UnityEngine;

using BBPC.API;

namespace BBPC.Compat
{
    /// <summary>
    /// NullStyle（levs_kittne.baldiplus.null）扩展兼容层 —�?选项分类部分�?    /// �?NULL 的选项分类（NullStyleOptionsCategory）的开关标�?/ 悬停提示 /
    /// 滚动提示注入翻译 key�?    /// 原实现：feat/nullstyle 分支 NullStyleOCPatch.cs（NullStyleOptionsPatch）�?    /// 编辑器工具部分见 NullStyleEditorCompat�?    /// </summary>
    public static class NullStyleCompat
    {
        public const string ModGuid = "levs_kittne.baldiplus.null";
        private const string AssemblyName = "NullStyleInBBPlus";
        private const string CategoryTypeName = "NULL.Manager.NullStyleOptionsCategory";

        private static bool _applied;

        private static readonly string[] ToggleNames =
        {
            "AmbienceToggle",
            "CharactersToggle",
            "AllEventsToggle",
            "ResultsTvToggle",
            "LightGlitchToggle",
            "GameCrashToggle",
            "ExtraFloorsToggle"
        };

        private static readonly string[] ToggleKeys =
        {
            "NULL_Ambience",
            "NULL_Characters",
            "NULL_AllEvents",
            "NULL_DisableResultsTV",
            "NULL_LightGlitch",
            "NULL_GameCrash",
            "NULL_ExtraFloors"
        };

        private static readonly string[] TooltipKeys =
        {
            "NULL_Ambience_Tooltip",
            "NULL_Characters_Tooltip",
            "NULL_AllEvents_Tooltip",
            "NULL_DisableResultsTV_Tooltip",
            "NULL_LightGlitch_Tooltip",
            "NULL_GameCrash_Tooltip",
            "NULL_ExtraFloors_Tooltip"
        };

        private static readonly string[] DefaultTooltips =
        {
            "There is no lighting in the school. Suspenseful background ambient track plays.\n<color=#008000ff>Default is true.",
            "Oh no! Null called other characters to help!\n<color=#008000ff>Default is false.",
            "Oh no! All random events are happening at once!\n<color=#008000ff>Default is false.",
            "If enabled, the score screen in the elevator will be hidden and the animation skipped.\n<color=#008000ff>Default is true.",
            "If enabled, lights near the boss will flicker.\n<color=#008000ff>Default is false.",
            "If enabled, the game will force close when you are caught by NULL.\n<color=#008000ff>Default is true.",
            "Adds two extra floors (F4 and F5) before the boss fight.\n<color=#008000ff>Default is false."
        };

        public static void Apply(Harmony harmony)
        {
            if (_applied || harmony == null) return;

            // 编辑器工具部分（独立类）
            NullStyleEditorCompat.Apply(harmony);

            if (!Chainloader.PluginInfos.ContainsKey(ModGuid)) return;

            Type? categoryType = CompatReflection.FindType(AssemblyName, CategoryTypeName);
            if (categoryType == null) return;

            MethodInfo? build = AccessTools.Method(categoryType, "Build");
            if (build != null)
                harmony.Patch(build, postfix: new HarmonyMethod(typeof(NullStyleCompat), nameof(BuildPostfix)));

            _applied = true;
            API.Logger.Info("NullStyle options compat applied.");
        }

        private static void BuildPostfix(object __instance)
        {
            if (BBPCTemp.is_eng || __instance == null) return;

            if (__instance is not Component comp)
            {
                API.Logger.Error("NullStyleCompat: category is not a Component!");
                return;
            }

            Transform viewport = comp.transform.Find("Viewport");
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
            LocalizeScrollHint(comp.transform);
        }

        private static void LocalizeToggles(Transform container, object instance)
        {
            for (int i = 0; i < ToggleNames.Length; i++)
            {
                Transform toggleTransform = container.Find(ToggleNames[i]);
                if (toggleTransform == null) continue;

                MenuToggle toggle = toggleTransform.GetComponent<MenuToggle>();
                if (toggle == null) continue;

                Transform textTransform = toggleTransform.Find("ToggleText");
                if (textTransform != null)
                {
                    TextMeshProUGUI tmp = textTransform.GetComponent<TextMeshProUGUI>();
                    if (tmp != null)
                    {
                        var existingTL = tmp.gameObject.GetComponent<TextLocalizer>();
                        if (existingTL != null) UnityEngine.Object.Destroy(existingTL);

                        TextLocalizer textTL = tmp.gameObject.AddComponent<TextLocalizer>();
                        textTL.key = ToggleKeys[i];
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
                        if (existingTooltip != null) UnityEngine.Object.Destroy(existingTooltip);

                        TextLocalizer tooltipLocalizer = btn.gameObject.AddComponent<TextLocalizer>();
                        tooltipLocalizer.key = TooltipKeys[i];
                        tooltipLocalizer.RefreshLocalization();

                        Type? categoryType = CompatReflection.FindType(AssemblyName, CategoryTypeName);
                        MethodInfo? method = categoryType?.GetMethod("AddTooltip",
                            BindingFlags.NonPublic | BindingFlags.Instance, null,
                            new Type[] { typeof(StandardMenuButton), typeof(string) }, null);
                        if (method != null)
                        {
                            string translatedTooltip = Plugin.Instance.GetTranslationKey(TooltipKeys[i], DefaultTooltips[i]);
                            try
                            {
                                method.Invoke(instance, new object[] { btn, translatedTooltip });
                            }
                            catch (Exception ex)
                            {
                                API.Logger.Error($"NullStyleCompat: AddTooltip failed - {ex.Message}");
                            }
                        }
                    }
                }
            }
        }

        private static void LocalizeScrollHint(Transform root)
        {
            Transform hintTransform = root.Find("ScrollHint");
            if (hintTransform == null) return;

            TextMeshProUGUI hintText = hintTransform.GetComponent<TextMeshProUGUI>();
            if (hintText == null) return;

            var existingTL = hintText.gameObject.GetComponent<TextLocalizer>();
            if (existingTL != null) UnityEngine.Object.Destroy(existingTL);

            TextLocalizer hintTL = hintText.gameObject.AddComponent<TextLocalizer>();
            hintTL.key = "NULL_ScrollHint";
            hintTL.RefreshLocalization();
        }
    }
}
