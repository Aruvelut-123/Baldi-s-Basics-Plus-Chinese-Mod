using HarmonyLib;
using TMPro;

namespace BBPC.FontPatches
{
    [HarmonyPatch(typeof(TMP_Text), "font", MethodType.Setter)]
    public class TMPTextFontSetPatch
    {
        public static void Postfix(TMP_Text __instance)
        {
            Plugin.ProccessComponent(__instance);
        }
    }
}
