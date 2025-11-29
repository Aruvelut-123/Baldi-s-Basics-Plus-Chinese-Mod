using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Text;
using TMPro;
using BBPC.API;

namespace BBPC
{
    internal class PickEndlessMapPatch
    {
        private static bool fixesApplied = false;
        
        private static readonly List<KeyValuePair<string, Vector2>> SizeDeltaTargets = new List<KeyValuePair<string, Vector2>>
        {
            new KeyValuePair<string, Vector2>("Random", new Vector2(134f, 100f))
        };
        
        private static readonly Dictionary<string, string> LocalizationKeys = new Dictionary<string, string>()
        {
            { "Text (TMP)", "BBPC_Menu_EndlessMapText" },
        };
        
        [HarmonyPatch(typeof(MenuButton), "Press")]
        private static class MenuButtonPressPatch
        {
            [HarmonyPostfix]
            private static void Postfix(MenuButton __instance)
            {
                if (__instance != null && __instance.name == "Endless" && !BBPCTemp.is_eng)
                {
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
                if (__instance.name == "Menu" && value && !BBPCTemp.is_eng)
                {
                    fixesApplied = false;
                }
                
                if (__instance.name == "PickEndlessMap" && value && !fixesApplied && !BBPCTemp.is_eng)
                {
                    ApplyButtonSizeFixes(__instance.transform);
                    ApplyLocalization(__instance.transform);
                    fixesApplied = true;
                    
                    ForceRefreshLocalization(__instance.transform);
                }
            }
        }
        private static void ForceRefreshLocalization(Transform pickEndlessMapTransform)
        {
            pickEndlessMapTransform.ApplyLocalizations(LocalizationKeys, true);
        }
        
        private static void ApplyButtonSizeFixes(Transform pickEndlessMapTransform)
        {
            pickEndlessMapTransform.SetSizeDeltas(SizeDeltaTargets);
        }
        
        private static void ApplyLocalization(Transform pickEndlessMapTransform)
        {
            pickEndlessMapTransform.ApplyLocalizations(LocalizationKeys);
        }
    }
} 