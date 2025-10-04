using BBPC.API;
using HarmonyLib;
using PlusLevelStudio.Menus;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Logger = BBPC.API.Logger;

namespace BBPC.Patches
{
    internal class EditorModeSelectionMenuPatch
    {
        private static readonly Dictionary<string, string> localizationKeys = new Dictionary<string, string>
        {
            { "PlayOrEdit/Text", "PlayEditButton" },
            { "EditorTypeSelection/FullButton", "Ed_ModeSelection_FullButton" },
            { "EditorTypeSelection/ComplaintButton", "Ed_ModeSelection_ComplaintButton" },
            { "EditorTypeSelection/RoomsButton", "Ed_ModeSelection_RoomsButton" }
        };

        private static readonly string PlayButtonKey = "Ed_ModeSelection_PlayButton";
        private static readonly string EditButtonKey = "Ed_ModeSelection_EditButton";

        [HarmonyPatch(typeof(EditorModeSelectionMenu), "Build")]
        private static class EditorModeSelectionMenuBuildPatch
        {
            [HarmonyPostfix]
            private static void Postfix()
            {
                Canvas[] screens = Resources.FindObjectsOfTypeAll<Canvas>();
                foreach (var canvas in screens)
                {
                    Logger.Debug("Find canvas: " + canvas.name);
                    if (canvas.name == "EditorModeSelection")
                    {
                        ApplyLocalizationDirectly(canvas.transform);

                        ProcessChildren(canvas.transform);
                    }
                }

            }

            private static void ProcessChildren(Transform parent)
            {
                foreach (Transform child in parent)
                {
                    Logger.Debug("  Find child: " + child.name);
                    ApplyLocalizationDirectly(child);
                    ProcessChildren(child);
                }
            }

            private static void ApplyLocalizationDirectly(Transform obj)
            {
                foreach (var kvp in localizationKeys)
                {
                    string fullPath = GetFullPath(obj);

                    Logger.Debug("  Find fullpath: " + fullPath);
                    Logger.Debug("  Find kvp key: " + kvp.Key);
                    Logger.Debug("  Find kvp value: " + kvp.Value);

                    if (fullPath == kvp.Key)
                    {
                        ApplyLocalizationToComponent(obj.gameObject, kvp.Value);
                        break;
                    }
                }
            }

            private static string GetFullPath(Transform obj)
            {
                if (obj.parent == null || obj.parent.name.Contains("Canvas"))
                {
                    return obj.name;
                }
                else
                {
                    return obj.parent.name + "/" + obj.name;
                }
            }

            private static string GetKey(TextMeshProUGUI component, string key)
            {
                if (component.text == "Play") return PlayButtonKey;
                else if (component.text == "Edit") return EditButtonKey;
                else return key;
            }

            private static void ApplyLocalizationToComponent(GameObject textObject, string key)
            {
                TextMeshProUGUI textComponent = textObject.GetComponent<TextMeshProUGUI>();

                if (textComponent != null)
                {
                    Logger.Debug("  Find textObject: " + textObject.name);
                    Logger.Debug("  textObject text: " + textComponent.text);
                    Logger.Debug("  Find key: " + GetKey(textComponent, key));

                    TextLocalizer localizer = textObject.GetComponent<TextLocalizer>() ?? textObject.AddComponent<TextLocalizer>();
                    localizer.key = GetKey(textComponent, key);

                    localizer.RefreshLocalization();
                }
                else
                {
                    try
                    {
                        foreach (Transform child in textObject.transform)
                        {
                            if (child.name == "Text")
                            {
                                textComponent = child.GetComponent<TextMeshProUGUI>();

                                Logger.Debug("  Find textObject: " + textObject.name);
                                Logger.Debug("  textObject text: " + textComponent.text);
                                Logger.Debug("  Find key: " + GetKey(textComponent, key));

                                TextLocalizer localizer = child.GetComponent<TextLocalizer>() ?? child.gameObject.AddComponent<TextLocalizer>();
                                localizer.key = GetKey(textComponent, key);

                                localizer.RefreshLocalization();
                                break;
                            }
                        }
                    }
                    catch { }
                }
            }
        }
    }
}
