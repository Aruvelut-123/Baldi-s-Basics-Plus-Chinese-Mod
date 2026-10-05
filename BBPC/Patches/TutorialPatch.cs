using BBPC.API;
using HarmonyLib;
using System.Collections;
using TMPro;
using UnityEngine;
using MTM101BaldAPI;

namespace BBPC.Patches
{
    [ConditionalPatchAlways]
    [HarmonyPatch(typeof(TutorialGameManager), "BeginPlay")]
    public static class TutorialPatch
    {
        [HarmonyPostfix]
        public static void Postfix(TutorialGameManager __instance)
        {
            if (!BBPCTemp.is_eng) __instance.StartCoroutine(ApplyChangesWithDelay());
        }

        private static IEnumerator ApplyChangesWithDelay()
        {
            yield return new WaitForSeconds(0.5f);

            GameObject tutorialManager = GameObject.Find("TutorialGameManager(Clone)");
            if (tutorialManager != null)
            {
                Transform textTransform = tutorialManager.transform.Find("DefaultCanvas/Text");
                if (textTransform != null)
                {
                    TextMeshProUGUI? textComponent = textTransform.GetComponent<TextMeshProUGUI>();
                    textComponent?.ApplyLocalization("BBPC_Tutorial_DefaultCanvas", true);
                }
            }
        }
    }
} 