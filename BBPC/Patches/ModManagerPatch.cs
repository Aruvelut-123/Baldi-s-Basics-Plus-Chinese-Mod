using HarmonyLib;
using ModManager;
using System;
using System.Reflection;
using UnityEngine;

namespace BBPC.ModManagerExtension.Patches
{
    [HarmonyPatch(typeof(ModManager.ModManager))]
    public class ModManagerPatch
    {
        [HarmonyPatch("Build")]
        [HarmonyPostfix]
        public static void BuildPostfix(ModManager.ModManager __instance)
        {
            TextLocalizer modInfoTL = __instance.modInfo.gameObject.AddComponent<TextLocalizer>();
            modInfoTL.key = "MMg_If_See";
            modInfoTL.RefreshLocalization();
            TextLocalizer modNameTL = __instance.modName.gameObject.AddComponent<TextLocalizer>();
            modNameTL.key = "MMg_If_See";
            modNameTL.RefreshLocalization();
            Transform reloadTransform = __instance.transform.Find("Reload");
            if (reloadTransform == null)
            {
                API.Logger.Error("Failed to find Reload button");
                return;
            }
            GameObject reloadButton = reloadTransform.gameObject;
            if (reloadButton.GetComponent<TextLocalizer>() != null)
            {
                Plugin.Destroy(reloadButton.GetComponent<TextLocalizer>());
            }
            TextLocalizer reloadTL = reloadButton.AddComponent<TextLocalizer>();
            reloadTL.key = "MMg_Reload";
            reloadTL.RefreshLocalization();
            Transform applyTransform = __instance.transform.Find("ApplyButton");
            if (reloadTransform == null)
            {
                API.Logger.Error("Failed to find Apply button");
                return;
            }
            GameObject applyButton = applyTransform.gameObject;
            var method = typeof(ModManager.ModManager).GetMethod("AddTooltip",
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new Type[] { typeof(StandardMenuButton), typeof(string) },
                null);
            if (method != null)
            {
                method.Invoke(__instance, new object[] { reloadButton.GetComponent<StandardMenuButton>(), BBPC.Plugin.Instance.GetTranslationKey("MMg_Reload_Introduction", "Load all .dll files from the plugins folder that haven't been loaded yet\n(helps enable mods without restarting the game)\n<color=red>Very broken thing, can broke game</color>") });
                method.Invoke(__instance, new object[] { applyButton.GetComponent<StandardMenuButton>(), BBPC.Plugin.Instance.GetTranslationKey("MMg_Apply_Introduction", "Apply changes") });
            }
        }

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        public static void UpdatePostfix(ModManager.ModManager __instance, ModInfo ___current)
        {
            bool ctrlPressed = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) ||
                               Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand);
            bool mPressed = Input.GetKeyDown(KeyCode.M);
            if (ctrlPressed && mPressed && ___current?.PluginInfo != null)
            {
                var plugin = ___current.PluginInfo;

                string customBuffer = "GUID: " + plugin.Metadata.GUID + "\n" + BBPC.Plugin.Instance.GetTranslationKey("MMg_Version", "Version: ") + plugin.Metadata.Version + "\n" + BBPC.Plugin.Instance.GetTranslationKey("MMg_Name", "Name: ") + plugin.Metadata.Name;

                GUIUtility.systemCopyBuffer = customBuffer;
            }
        }
    }
}
