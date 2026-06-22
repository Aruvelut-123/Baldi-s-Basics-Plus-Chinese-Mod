using HarmonyLib;
using TMPro;

namespace BBPC.FontPatches
{
    [HarmonyPatch(typeof(TextMeshPro), "Awake")]
    public class TextMeshProPatch
    {
        public static void Postfix(TextMeshPro __instance)
        {
            Plugin.ProccessComponent(__instance);
        }
    }
}
