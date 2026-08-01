using BaldiTexturePacks;
using HarmonyLib;
using System.Reflection;
using TMPro;
using UnityEngine;
using Logger = BBPC.TextureExtension.API.Logger;

namespace BBPC.TextureExtension.Patches
{
    [HarmonyPatch(typeof(PackManagerScreen))]
    public class PackManagerScreenPatch
    {
        [HarmonyPatch("Build")]
        [HarmonyPrefix]
        public static bool BuildPrefix(PackManagerScreen __instance)
        {
            if (Plugin.IsEnglish) return true;
            if (TexturePacksPlugin.packs.Count == 0)
            {
                var createTextButtonMethod = typeof(PackManagerScreen).GetMethod(
                    "CreateTextButton",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if (createTextButtonMethod != null)
                {
                    createTextButtonMethod.Invoke(__instance, new object[]
                    {
                        (UnityEngine.Events.UnityAction)(() =>
                        {
                            Application.OpenURL(TexturePacksPlugin.packsPath);
                        }),
                        "NoPack",
                        BBPC.Plugin.Instance.GetTranslationKey("notexture", "No Texture Packs Installed!"),
                        Vector3.zero,
                        MTM101BaldAPI.UI.BaldiFonts.ComicSans24,
                        TextAlignmentOptions.Center,
                        new Vector2(300f, 64f),
                        Color.gray
                    });
                }
                else
                {
                    Logger.Warning("CreateTextButton method not found!");
                }
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(PackManagerScreen))]
    [HarmonyPatch("BuildPackMoveButton")]
    public static class BuildPackMoveButtonTooltipPatch
    {
        public static void Postfix(PackEntryUI __result, PackManagerScreen __instance)
        {
            if (Plugin.IsEnglish) return;
            if (__result == null)
            {
                Logger.Warning("__result is null!");
                return;
            }

            if (__result.toggle == null)
            {
                Logger.Warning("__result.toggle is null!");
                return;
            }

            var tooltipField = typeof(PackManagerScreen).GetField("tooltipController",
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

            if (tooltipField == null)
            {
                var baseType = typeof(PackManagerScreen).BaseType;
                if (baseType != null)
                {
                    tooltipField = baseType.GetField("tooltipController",
                        BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                }
            }

            if (tooltipField == null)
            {
                Logger.Warning("tooltipController field not found in PackManagerScreen or its base class!");
                return;
            }

            var tooltipController = tooltipField.GetValue(__instance);
            if (tooltipController == null)
            {
                Logger.Warning("tooltipController is null!");
                return;
            }

            var button = __result.toggle.GetComponentInChildren<StandardMenuButton>();
            if (button == null)
            {
                Logger.Warning("StandardMenuButton component not found!");
                return;
            }

            button.OnHighlight.RemoveAllListeners();
            button.OnHighlight.AddListener(() => {
                try
                {
                    if (__result.currentPack == null)
                    {
                        Logger.Warning("OnHighlight: currentPack is null!");
                        return;
                    }

                    if (__result.currentPack.metaData == null)
                    {
                        Logger.Warning("OnHighlight: currentPack.metaData is null!");
                        return;
                    }

                    string customText = __result.currentPack.metaData.description + "\n" +
                                       BBPC.Plugin.Instance.GetTranslationKey("author", "Author:") + " " +
                                       __result.currentPack.metaData.author +
                                       (__result.currentPack.flags == PackFlags.Legacy ? "\n(Legacy Pack!)" : "");

                    var updateMethod = tooltipController.GetType().GetMethod("UpdateTooltip");
                    if (updateMethod == null)
                    {
                        Logger.Warning("OnHighlight: UpdateTooltip method not found!");
                        return;
                    }

                    updateMethod.Invoke(tooltipController, new object[] { customText });
                    Logger.Debug($"Tooltip updated for pack: {__result.currentPack.metaData.name}");
                }
                catch (System.Exception ex)
                {
                    Logger.Error($"OnHighlight: Exception occurred - {ex.Message}\n{ex.StackTrace}");
                }
            });

            button.OffHighlight.RemoveAllListeners();
            button.OffHighlight.AddListener(() => {
                try
                {
                    var closeMethod = tooltipController.GetType().GetMethod("CloseTooltip");
                    if (closeMethod == null)
                    {
                        Logger.Warning("OffHighlight: CloseTooltip method not found!");
                        return;
                    }

                    closeMethod.Invoke(tooltipController, null);
                }
                catch (System.Exception ex)
                {
                    Logger.Error($"OffHighlight: Exception occurred - {ex.Message}\n{ex.StackTrace}");
                }
            });

            Logger.Debug($"Successfully patched tooltip for pack: {__result.currentPack?.metaData?.name ?? "Unknown"}");
        }
    }
}
