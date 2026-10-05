using BBPC.API;
using HarmonyLib;
using System.Linq;
using TMPro;
using MTM101BaldAPI;

namespace BBPC.Patches
{
    internal class ChallengeWinPatch
    {
        private const string WinTextKey = "BBPC_ChallengeWin_Text";

        [ConditionalPatchAlways]
        [HarmonyPatch(typeof(ChallengeWin), "Start")]
        private static class ChallengeWinStartPatch
        {
            [HarmonyPostfix]
            private static void Postfix(ChallengeWin __instance)
            {
                // We search for the text component within the ChallengeWin hierarchy.
                // GetComponentsInChildren(true) is robust and finds components even if they are inactive.
                var textComponents = __instance.GetComponentsInChildren<TextMeshProUGUI>(true);
                var targetText = textComponents.FirstOrDefault(t => t.name == "Text (TMP)");

                if (targetText != null && !BBPCTemp.is_eng)
                {
                    targetText.ApplyLocalization(WinTextKey);
                }
            }
        }

    }
} 