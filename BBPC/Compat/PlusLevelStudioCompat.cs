using BepInEx.Bootstrap;
using HarmonyLib;
using MTM101BaldAPI.AssetTools;
using System;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

using BBPC.API;

namespace BBPC.Compat
{
    /// <summary>
    /// PlusLevelStudio（mtm101.rulerp.baldiplus.levelstudio）扩展兼容层�?    /// 包含：Path.Combine 重定向（�?LevelStudio �?Data/UI 资源重定向到�?mod �?    /// EditorData 目录）、编辑器模式选择菜单的按钮本地化�?    /// 原实现：feat/pluslevelstudio 分支 PathPatch.cs / EditorModeSelectionMenuPatch.cs�?    /// </summary>
    public static class PlusLevelStudioCompat
    {
        public const string ModGuid = "mtm101.rulerp.baldiplus.levelstudio";
        private const string AssemblyName = "PlusLevelStudio";
        private const string PluginTypeName = "PlusLevelStudio.LevelStudioPlugin";

        private static bool _applied;
        private static bool _isGettingPath;
        private static string? _cachedLevelStudioPath;
        private static string? _cachedModPath;

        public static void Apply(Harmony harmony)
        {
            if (_applied || harmony == null) return;
            if (!Chainloader.PluginInfos.ContainsKey(ModGuid)) return;

            // 1) Path.Combine(string[]) 重定向
            MethodInfo? combine = typeof(Path).GetMethod("Combine", new Type[] { typeof(string[]) });
            if (combine != null)
                harmony.Patch(combine, prefix: new HarmonyMethod(typeof(PlusLevelStudioCompat), nameof(PathCombinePrefix)));

            // 2) 编辑器模式选择菜单
            Type? menuType = CompatReflection.FindType(AssemblyName, "PlusLevelStudio.Menus.EditorModeSelectionMenu");
            if (menuType != null)
            {
                MethodInfo? create = AccessTools.Method(menuType, "CreateMenuButton");
                if (create != null)
                    harmony.Patch(create, postfix: new HarmonyMethod(typeof(PlusLevelStudioCompat), nameof(CreateMenuButtonPostfix)));

                MethodInfo? build = AccessTools.Method(menuType, "Build");
                if (build != null)
                    harmony.Patch(build, postfix: new HarmonyMethod(typeof(PlusLevelStudioCompat), nameof(BuildPostfix)));
            }

            Initialize();
            _applied = true;
            API.Logger.Info($"PlusLevelStudio compat applied (menu={menuType != null}).");
        }

        private static void Initialize()
        {
            try
            {
                _cachedModPath = AssetLoader.GetModPath(Plugin.Instance);
                object? lsInstance = CompatReflection.GetStatic(
                    CompatReflection.FindType(AssemblyName, PluginTypeName), "Instance");
                if (lsInstance is BepInEx.BaseUnityPlugin lsPlugin)
                    _cachedLevelStudioPath = AssetLoader.GetModPath(lsPlugin);
            }
            catch (Exception ex)
            {
                API.Logger.Error($"PlusLevelStudioCompat: Initialize failed - {ex.Message}");
            }
        }

        // ---------- Path.Combine ----------

        private static bool PathCombinePrefix(string[] paths, ref string __result)
        {
            if (BBPCTemp.is_eng || _isGettingPath || paths == null || paths.Length < 3) return true;
            if (_cachedLevelStudioPath == null) return true;
            if (paths[0] != _cachedLevelStudioPath || paths[1] != "Data" || paths[2] != "UI") return true;

            _isGettingPath = true;
            try
            {
                string[] newPaths = new string[paths.Length];
                newPaths[0] = _cachedModPath ?? paths[0];
                newPaths[1] = "EditorData";
                for (int i = 2; i < paths.Length; i++) newPaths[i] = paths[i];
                __result = Path.Combine(newPaths);
                return false;
            }
            catch (Exception ex)
            {
                API.Logger.Error($"PlusLevelStudioCompat PathCombinePrefix: {ex.Message}");
            }
            finally
            {
                _isGettingPath = false;
            }
            return true;
        }

        // ---------- EditorModeSelectionMenu.CreateMenuButton ----------

        private static void CreateMenuButtonPostfix(
            Transform parent,
            string name,
            string text,
            Vector3 localPosition,
            UnityAction action,
            StandardMenuButton __result)
        {
            if (BBPCTemp.is_eng || __result == null || __result.text == null) return;

            string? key = text switch
            {
                "Full" => "EDITOR_Full_button",
                "Compliant" => "EDITOR_Compliant_button",
                "Rooms" => "EDITOR_Rooms_button",
                _ => null
            };

            if (key != null)
            {
                TextLocalizer tl = __result.text.gameObject.GetComponent<TextLocalizer>() ?? __result.text.gameObject.AddComponent<TextLocalizer>();
                tl.key = key;
                tl.RefreshLocalization();
            }
        }

        // ---------- EditorModeSelectionMenu.Build ----------

        private static void BuildPostfix(object __result)
        {
            try
            {
                if (BBPCTemp.is_eng || __result == null) return;

                object? playOrEditParent = CompatReflection.GetInstance(__result, "playOrEditParent");
                if (playOrEditParent == null) return;

                Transform? parentTransform = playOrEditParent switch
                {
                    Component c => c.transform,
                    GameObject g => g.transform,
                    _ => null
                };
                if (parentTransform == null) return;

                foreach (Transform child in parentTransform)
                {
                    TMP_Text tmpText = child.GetComponent<TMP_Text>();
                    if (tmpText == null) continue;

                    string? key = tmpText.text switch
                    {
                        "Edit" => "EDITOR_Edit_button",
                        "Play" => "EDITOR_Play_button",
                        _ => null
                    };

                    if (key != null)
                    {
                        TextLocalizer tl = tmpText.gameObject.GetComponent<TextLocalizer>() ?? tmpText.gameObject.AddComponent<TextLocalizer>();
                        tl.key = key;
                        tl.RefreshLocalization();
                    }
                }
            }
            catch (Exception ex)
            {
                API.Logger.Error($"PlusLevelStudioCompat BuildPostfix: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}
