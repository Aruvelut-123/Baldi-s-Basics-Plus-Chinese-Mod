using BepInEx;
using HarmonyLib;
using ModManager;
using System;
using System.Reflection;
using TMPro;
using UnityEngine;

namespace BBPC.ModManagerExtension.Patches
{
    [HarmonyPatch(typeof(ModInfo))]
    public class ModInfoPatch
    {
        [HarmonyPatch(MethodType.Constructor, typeof(string))]
        [HarmonyPostfix]
        public static void Postfix(ModInfo __instance, ref MenuToggle ___toggle)
        {
            if (__instance.IsException || ___toggle == null) return;
            if (___toggle.gameObject.GetComponentInChildren<TextMeshProUGUI>() == null)
            {
                API.Logger.Error("get null ;(");
                return;
            }
            GameObject toggle = ___toggle.gameObject.GetComponentInChildren<TextMeshProUGUI>().gameObject;
            TextLocalizer toggleLocalizer = toggle.AddComponent<TextLocalizer>();
            toggleLocalizer.key = "MMg_Active";
            toggleLocalizer.RefreshLocalization();
            var method = typeof(ModManager.ModManager).GetMethod("AddTooltip",
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new Type[] { typeof(MenuToggle), typeof(string) },
                null);
            if (method != null)
            {
                method.Invoke(ModInfo.modManager, new object[] { ___toggle, BBPC.Plugin.Instance.GetTranslationKey("MMg_Active_Introduction", "Is mod active") });
            }
        }

        [HarmonyPatch("SetActive")]
        [HarmonyPostfix]
        public static void SetActivePostfix(ModInfo __instance, bool active)
        {
            if (active)
            {
                ModInfo.modManager.modInfo.text = BBPC.Plugin.Instance.GetTranslationKey("MMg_Name", "Name: ") + __instance.PluginInfo.Metadata.Name + "\nGUID: " + __instance.PluginInfo.Metadata.GUID + "\n" + BBPC.Plugin.Instance.GetTranslationKey("MMg_Version", "Version: ") + __instance.PluginInfo.Metadata.Version;
            }
        }
    }
}
