using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Text;
using TMPro;

namespace BBPC
{
    internal class AboutMenuPatch
    {
        private static bool fixesApplied = false;

        private static readonly Dictionary<string, string> LocalizationKeys = new Dictionary<string, string>()
        {
            { "DevUpdateTitle", "BBPC_About_DevUpdateTitle" },
            { "DevUpdateText", "BBPC_About_DevUpdateText" },
            { "Credits", "BBPC_About_CreditsText" },
            { "WebsiteButton", "BBPC_About_WebsiteButtonText" },
            { "DevlogsButton", "BBPC_About_DevlogsButtonText" },
            { "BugsButton", "BBPC_About_BugsButtonText" },
            { "SaveFolderButton", "BBPC_About_SaveFolderButtonText" },
            { "AnniversaryButton", "BBPC_About_AnniversaryButtonText" },
            { "RoadmapButton", "BBPC_About_RoadmapButtonText" }
        };

        private static readonly List<KeyValuePair<string, Vector2>> SizeDeltaTargets = new List<KeyValuePair<string, Vector2>>
        {
            new KeyValuePair<string, Vector2>("SaveFolderButton", new Vector2(150f, 50f))
        };

        private static Dictionary<string, Transform> BuildTransformPathMap(Transform parent)
        {
            var map = new Dictionary<string, Transform>();
            var children = parent.GetComponentsInChildren<Transform>(true);

            foreach (var child in children)
            {
                if (child == parent) continue;

                StringBuilder pathBuilder = new StringBuilder();
                Transform current = child;
                while (current != null && current != parent)
                {
                    if (pathBuilder.Length > 0)
                        pathBuilder.Insert(0, "/");
                    pathBuilder.Insert(0, current.name);
                    current = current.parent;
                }

                if (current == parent)
                {
                    string path = pathBuilder.ToString();
                    if (!map.ContainsKey(path))
                    {
                        map.Add(path, child);
                    }
                }
            }
            return map;
        }

        [HarmonyPatch(typeof(MenuButton), "Press")]
        private static class MenuButtonPressPatch
        {
            [HarmonyPostfix]
            private static void Postfix(MenuButton __instance)
            {
                if (__instance != null && __instance.name == "About")
                {
                    // Debug.Log("[AboutMenuPatch] About按钮按下，重置修复状态");
                    fixesApplied = false;
                }
            }
        }

        [HarmonyPatch(typeof(GameObject), "SetActive")]
        private static class SetActivePatch
        {
            [HarmonyPostfix]
            private static void Postfix(GameObject __instance, bool value)
            {
                if (__instance.name == "Menu" && value)
                {
                    fixesApplied = false;
                }

                if (__instance.name == "About" && value && !fixesApplied)
                {
                    var transformMap = BuildTransformPathMap(__instance.transform);
                    ApplyLocalization(transformMap);
                    ApplySizeDeltaChanges(transformMap);
                    fixesApplied = true;

                    ForceRefreshLocalization(transformMap);
                }
            }
        }

        private static void ForceRefreshLocalization(Dictionary<string, Transform> transformMap)
        {
            // Debug.Log("[AboutMenuPatch] 强制执行本地化刷新");
            foreach (var entry in LocalizationKeys)
            {
                if (transformMap.TryGetValue(entry.Key, out Transform targetTransform))
                {
                    TextLocalizer localizer = targetTransform.GetComponent<TextLocalizer>();
                    if (localizer != null)
                    {
                        localizer.RefreshLocalization();
                    }
                }
                else
                {
                    // Debug.LogWarning($"[AboutMenuPatch] 在About菜单中找不到路径: {entry.Key}");
                }
            }
        }

        private static void ApplySizeDeltaChanges(Dictionary<string, Transform> transformMap)
        {
            // Debug.Log("[AboutMenuPatch] 应用尺寸调整");

            foreach (var target in SizeDeltaTargets)
            {
                if (transformMap.TryGetValue(target.Key, out Transform elementTransform))
                {
                    RectTransform rectTransform = elementTransform.GetComponent<RectTransform>();
                    if (rectTransform != null)
                    {
                        rectTransform.sizeDelta = target.Value;
                        // Debug.Log($"[AboutMenuPatch] 已将尺寸 {target.Value} 应用到 {target.Key}");
                    }
                    else
                    {
                        // Debug.LogWarning($"[AboutMenuPatch] 在 {target.Key} 上找不到RectTransform组件");
                    }
                }
                else
                {
                    // Debug.LogWarning($"[AboutMenuPatch] 在About菜单中找不到路径: {target.Key}");
                }
            }
        }

        private static void ApplyLocalization(Dictionary<string, Transform> transformMap)
        {
            // Debug.Log("[AboutMenuPatch] 正在为About菜单应用本地化");
            foreach (var entry in LocalizationKeys)
            {
                if (transformMap.TryGetValue(entry.Key, out Transform targetTransform))
                {
                    TextMeshProUGUI textComponent = targetTransform.GetComponent<TextMeshProUGUI>();
                    if (textComponent != null)
                    {
                        Component[] components = targetTransform.GetComponents<Component>();
                        foreach (Component component in components)
                        {
                            if (component != null && component.GetType().Name == "TextLocalizer" && component.GetType() != typeof(TextLocalizer))
                            {
                                Object.Destroy(component);
                            }
                        }

                        TextLocalizer localizer = textComponent.GetComponent<TextLocalizer>();
                        if (localizer == null)
                        {
                            localizer = textComponent.gameObject.AddComponent<TextLocalizer>();
                            localizer.key = entry.Value;
                            // Debug.Log($"[AboutMenuPatch] 已为 {entry.Key} 添加TextLocalizer组件，使用键值: {entry.Value}");
                        }
                        else if (localizer.key != entry.Value)
                        {
                            localizer.key = entry.Value;
                            localizer.RefreshLocalization();
                            // Debug.Log($"[AboutMenuPatch] 已更新 {entry.Key} 的TextLocalizer键值: {entry.Value}");
                        }
                    }
                    else
                    {
                        //  Debug.LogWarning($"[AboutMenuPatch] 在 {entry.Key} 上找不到TextMeshProUGUI组件");
                    }
                }
                else
                {
                    // Debug.LogWarning($"[AboutMenuPatch] 在About菜单中找不到路径: {entry.Key}");
                }
            }
        }
    }
}