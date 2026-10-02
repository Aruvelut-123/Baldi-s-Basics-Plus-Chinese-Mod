using BepInEx.Bootstrap;
using HarmonyLib;
using MTM101BaldAPI.OptionsAPI;
using System;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

using BBPC.API;

namespace BBPC.Compat
{
    /// <summary>
    /// BaldiTexturePacks（pixelguy.pixelmodding.baldiplus.texturepacks）扩展兼容层�?    /// 包含：主菜单注入 Texture Pack 分类；无包时创建提示按钮；包按钮悬停 tooltip 本地化�?    /// 原实现：feat/texturepack 分支 PackManagerScreenPatch.cs / TexturePacksPluginPatch.cs�?    /// </summary>
    public static class TexturePackCompat
    {
        public const string ModGuid = "pixelguy.pixelmodding.baldiplus.texturepacks";
        private const string AssemblyName = "BaldiTexturePacks";
        private const string PluginTypeName = "BaldiTexturePacks.TexturePacksPlugin";
        private const string ScreenTypeName = "BaldiTexturePacks.PackManagerScreen";
        private const string EntryTypeName = "BaldiTexturePacks.PackEntryUI";

        private static bool _applied;
        private static Type? _pluginType;
        private static Type? _screenType;
        private static object? _legacyFlagValue;
        private static FieldInfo? _tooltipField;
        private static MethodInfo? _tooltipUpdateMethod;
        private static MethodInfo? _tooltipCloseMethod;

        public static void Apply(Harmony harmony)
        {
            if (_applied || harmony == null) return;
            if (!Chainloader.PluginInfos.ContainsKey(ModGuid)) return;

            _pluginType = CompatReflection.FindType(AssemblyName, PluginTypeName);
            _screenType = CompatReflection.FindType(AssemblyName, ScreenTypeName);
            Type? entryType = CompatReflection.FindType(AssemblyName, EntryTypeName);

            // 1) TexturePacksPlugin.AddCategory：主菜单注入分类
            if (_pluginType != null)
            {
                MethodInfo? addCategory = AccessTools.Method(_pluginType, "AddCategory");
                if (addCategory != null)
                    harmony.Patch(addCategory, prefix: new HarmonyMethod(typeof(TexturePackCompat), nameof(AddCategoryPrefix)));
            }

            // 2) PackManagerScreen
            if (_screenType != null)
            {
                MethodInfo? build = AccessTools.Method(_screenType, "Build");
                if (build != null)
                    harmony.Patch(build, prefix: new HarmonyMethod(typeof(TexturePackCompat), nameof(BuildPrefix)));

                MethodInfo? buildMove = AccessTools.Method(_screenType, "BuildPackMoveButton");
                if (buildMove != null)
                    harmony.Patch(buildMove, postfix: new HarmonyMethod(typeof(TexturePackCompat), nameof(BuildPackMoveButtonPostfix)));

                _tooltipField = _screenType.GetField("tooltipController",
                        BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
                    ?? _screenType.BaseType?.GetField("tooltipController",
                        BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            }

            // 3) PackFlags.Legacy 枚举
            Type? packFlagsType = entryType?.GetNestedType("PackFlags");
            if (packFlagsType == null) packFlagsType = CompatReflection.FindTypeBySimpleName(AssemblyName, "PackFlags");
            if (packFlagsType != null && packFlagsType.IsEnum)
            {
                try { _legacyFlagValue = Enum.Parse(packFlagsType, "Legacy"); }
                catch (Exception) { _legacyFlagValue = null; }
            }

            _applied = true;
            API.Logger.Info($"TexturePack compat applied (screen={_screenType != null}).");
        }

        // ---------- TexturePacksPlugin.AddCategory ----------

        private static bool AddCategoryPrefix(OptionsMenu __instance, CustomOptionsHandler handler)
        {
            if (BBPCTemp.is_eng || handler == null || _screenType == null) return true;

            try
            {
                // 游戏进行中不注入，只在主菜单
                if (Singleton<CoreGameManager>.Instance != null) return true;

                MethodInfo? addCategory = handler.GetType().GetMethods()
                    .FirstOrDefault(m => m.Name == "AddCategory" && m.IsGenericMethodDefinition
                        && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(string));
                if (addCategory != null)
                {
                    addCategory.MakeGenericMethod(_screenType).Invoke(handler,
                        new object[] { Plugin.Instance.GetTranslationKey("Texture_options_title", "Texture\nPack") });
                }
                return false;
            }
            catch (Exception ex)
            {
                API.Logger.Error($"TexturePackCompat: AddCategory failed - {ex.Message}");
                return true;
            }
        }

        // ---------- PackManagerScreen.Build ----------

        private static bool BuildPrefix(object __instance)
        {
            if (BBPCTemp.is_eng || __instance == null || _screenType == null || _pluginType == null) return true;

            object? packs = CompatReflection.GetStatic(_pluginType, "packs");
            if (packs != null && CompatReflection.GetCount(packs) != 0) return true;

            MethodInfo? createTextButton = _screenType.GetMethod("CreateTextButton",
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (createTextButton == null)
            {
                API.Logger.Warning("CreateTextButton method not found!");
                return false;
            }

            try
            {
                string? packsPath = CompatReflection.GetStatic(_pluginType, "packsPath") as string;
                string noPackText = Plugin.Instance.GetTranslationKey("notexture", "No Texture Packs Installed!");

                createTextButton.Invoke(__instance, new object[]
                {
                    (UnityAction)(() => Application.OpenURL(packsPath ?? "")),
                    "NoPack",
                    noPackText,
                    Vector3.zero,
                    MTM101BaldAPI.UI.BaldiFonts.ComicSans24,
                    TextAlignmentOptions.Center,
                    new Vector2(300f, 64f),
                    Color.gray
                });
            }
            catch (Exception ex)
            {
                API.Logger.Error($"TexturePackCompat BuildPrefix: {ex.Message}");
            }
            return false;
        }

        // ---------- PackManagerScreen.BuildPackMoveButton ----------

        private static void BuildPackMoveButtonPostfix(object __result, object __instance)
        {
            if (BBPCTemp.is_eng || __result == null || __instance == null || _tooltipField == null) return;
            if (_tooltipField.GetValue(__instance) is not Component tooltipComp) return;

            object? toggle = CompatReflection.GetInstance(__result, "toggle");
            if (toggle is not Component toggleComp) return;

            StandardMenuButton? button = toggleComp.GetComponentInChildren<StandardMenuButton>();
            if (button == null) return;

            object tooltipController = tooltipComp;
            Type tooltipType = tooltipController.GetType();
            _tooltipUpdateMethod = _tooltipUpdateMethod ?? tooltipType.GetMethod("UpdateTooltip");
            _tooltipCloseMethod = _tooltipCloseMethod ?? tooltipType.GetMethod("CloseTooltip");

            button.OnHighlight.RemoveAllListeners();
            button.OnHighlight.AddListener(() =>
            {
                try
                {
                    if (_tooltipUpdateMethod == null) return;

                    object? currentPack = CompatReflection.GetInstance(__result, "currentPack");
                    if (currentPack == null) return;
                    object? metaData = CompatReflection.GetInstance(currentPack, "metaData");
                    if (metaData == null) return;

                    string description = CompatReflection.GetInstance(metaData, "description") as string ?? string.Empty;
                    string author = CompatReflection.GetInstance(metaData, "author") as string ?? string.Empty;
                    object? flags = CompatReflection.GetInstance(currentPack, "flags");

                    string customText = description + "\n" +
                                        Plugin.Instance.GetTranslationKey("author", "Author:") + " " + author +
                                        (_legacyFlagValue != null && Equals(flags, _legacyFlagValue) ? "\n(Legacy Pack!)" : "");

                    _tooltipUpdateMethod.Invoke(tooltipController, new object[] { customText });
                }
                catch (Exception ex)
                {
                    API.Logger.Error($"TexturePackCompat OnHighlight: {ex.Message}");
                }
            });

            button.OffHighlight.RemoveAllListeners();
            button.OffHighlight.AddListener(() =>
            {
                try
                {
                    _tooltipCloseMethod?.Invoke(tooltipController, null);
                }
                catch (Exception ex)
                {
                    API.Logger.Error($"TexturePackCompat OffHighlight: {ex.Message}");
                }
            });
        }
    }
}
