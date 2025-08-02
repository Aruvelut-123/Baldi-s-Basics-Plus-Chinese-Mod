using HarmonyLib;
using UnityEngine;
using TMPro;
using System.Collections;

namespace BBPC.Patches
{
    [HarmonyPatch(typeof(TutorialGameManager), "BeginPlay")]
    public static class TutorialPatch
    {
        [HarmonyPostfix]
        public static void Postfix(TutorialGameManager __instance)
        {
            __instance.StartCoroutine(ApplyChangesWithDelay());
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
                    TextLocalizer existingLocalizer = textTransform.GetComponent<TextLocalizer>();
                    if (existingLocalizer == null)
                    {
                        TextLocalizer localizer = textTransform.gameObject.AddComponent<TextLocalizer>();
                        localizer.key = "BBPC_Tutorial_DefaultCanvas";
                    }
                }
            }
        }
    }
} 