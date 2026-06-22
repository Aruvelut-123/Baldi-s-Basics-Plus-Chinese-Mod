using HarmonyLib;
using TMPro;

namespace BBPC.FontPatches
{
    [HarmonyPatch(typeof(TextMeshProUGUI), "Awake")]
    public class TextMeshProUGUIPatch
    {
        public static void Postfix(TextMeshProUGUI __instance)
        {
            Plugin.ProccessComponent(__instance);
        }
    }
}
