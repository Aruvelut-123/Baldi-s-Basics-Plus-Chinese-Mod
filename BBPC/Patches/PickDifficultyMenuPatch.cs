using ChallengeJar.Menu;
using HarmonyLib;
using System.Collections.Generic;
using BBPC.API;

namespace BBPC.ChallengeJarExtension.Patches
{
    [HarmonyPatch(typeof(PickDifficultyMenu))]
    public class PickDifficultyMenuPatch
    {
        private static readonly Dictionary<string, string> LocalizationKeys = new Dictionary<string, string>()
        {
            { "Normal", "CJ_Normal" },
            { "Hard", "CJ_Hard" }
        };

        [HarmonyPostfix]
        [HarmonyPatch("InitButtons")]
        private static void InitButtons_Postfix(PickDifficultyMenu __instance)
        {
            API.Logger.Error("test");
            if (!Plugin.is_english)
            {
                ApplyLocalization(__instance);
            }
        }

        private static void ApplyLocalization(PickDifficultyMenu challengeExtraMenuInstance)
        {
            if (challengeExtraMenuInstance == null) return;
            challengeExtraMenuInstance.transform.ApplyLocalizations(LocalizationKeys, true);
        }

    }
}
