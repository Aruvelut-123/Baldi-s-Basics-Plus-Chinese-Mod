using HarmonyLib;
using PlusLevelStudio.Menus;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace BBPC.EditorExtension.Patches
{
    [HarmonyPatch(typeof(EditorModeSelectionMenu))]
    public class EditorModeSelectionMenuPatch
    {
        // 原有的 CreateMenuButton 补丁
        [HarmonyPatch("CreateMenuButton")]
        [HarmonyPostfix]
        public static void CreateMenuButtonPostfix(
            Transform parent,
            string name,
            string text,
            Vector3 localPosition,
            UnityAction action,
            StandardMenuButton __result)
        {
            API.Logger.Info($"CreateMenuButton: {text}");

            string? key = text switch
            {
                "Full" => "EDITOR_Full_button",
                "Compliant" => "EDITOR_Compliant_button",
                "Rooms" => "EDITOR_Rooms_button",
                _ => null
            };

            if (key != null)
            {
                TextLocalizer tl = __result.text.gameObject.AddComponent<TextLocalizer>();
                tl.key = key;
                tl.RefreshLocalization();
            }
        }

        // 新增的 Build 方法 Postfix
        [HarmonyPatch("Build")]
        [HarmonyPostfix]
        public static void BuildPostfix(EditorModeSelectionMenu __result)
        {
            if (__result == null) return;

            // 访问 playOrEditParent 字段（假设它是公共的，如果是私有字段请使用反射）
            GameObject playOrEditParent = __result.playOrEditParent;
            if (playOrEditParent == null) return;

            // 遍历所有子物体，查找文本组件
            foreach (Transform child in playOrEditParent.transform)
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
                    TextLocalizer tl = tmpText.gameObject.AddComponent<TextLocalizer>();
                    tl.key = key;
                    tl.RefreshLocalization();
                    API.Logger.Info($"Added TextLocalizer to {key}");
                }
            }
        }
    }
}