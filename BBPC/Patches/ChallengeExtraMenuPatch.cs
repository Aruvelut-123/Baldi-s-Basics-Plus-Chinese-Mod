using BBPC.API;
using ChallengeJar.Menu;
using HarmonyLib;
using System.Collections.Generic;

namespace BBPC.ChallengeJarExtension.Patches
{
    [HarmonyPatch(typeof(ChallengeExtraMenu))]
    public class ChallengeExtraMenuPatch
    {
        private static readonly Dictionary<string, string> LocalizationKeys = new Dictionary<string, string>()
        {
            { "Invasion", "CJ_Invasion" },
            { "Locked", "CJ_Locked" },
            { "Robbing", "CJ_Robbing" },
            { "Placeholder", "CJ_Placeholder" }
        };

        [HarmonyPostfix]
        [HarmonyPatch("Start")]
        private static void Start_Postfix(ChallengeExtraMenu __instance)
        {
            if (!Plugin.is_english)
            {
                ApplyLocalization(__instance);
            }
        }

        private static void ApplyLocalization(ChallengeExtraMenu challengeExtraMenuInstance)
        {
            if (challengeExtraMenuInstance == null) return;
            challengeExtraMenuInstance.transform.ApplyLocalizations(LocalizationKeys, true);
        }

    }
}
