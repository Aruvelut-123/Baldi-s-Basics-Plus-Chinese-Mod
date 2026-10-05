using BBPC.API;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.OptionsAPI;
using MTM101BaldAPI.SaveSystem;
using System;
using System.Reflection;
using TMPro;
using UnityEngine;

namespace BBPC.MTMAPIPatches
{
    [ConditionalPatchAlways]
    [HarmonyPatch(typeof(SaveManagerMenu))]
    public class SaveManagerMenuPatch
    {
        [HarmonyPatch("MenuHook")]
        [HarmonyPrefix]
        public static bool MenuHookPrefix(OptionsMenu __instance, CustomOptionsHandler handler)
        {
            Type saveManagerType = typeof(MTM101BaldAPI.SaveManagerMenu);
            Assembly apiAssembly = saveManagerType.Assembly;
            Type moddedHighscoreType = apiAssembly.GetType("MTM101BaldAPI.ModdedHighscoreMenu");
            MethodInfo genericAddCategory = typeof(CustomOptionsHandler).GetMethod("AddCategory", new Type[] { typeof(string) });
            if (genericAddCategory == null || !genericAddCategory.IsGenericMethodDefinition)
            {
                API.Logger.Error("无法找到 CustomOptionsHandler.AddCategory<T>(string) 方法");
                return true;
            }
            if (saveManagerType != null && MTM101BaldiDevAPI.SaveGamesHandler == SavedGameDataHandler.Modded)
            {
                MethodInfo addSaveManager = genericAddCategory.MakeGenericMethod(saveManagerType);
                string saveManagerName = BBPC.Plugin.Instance.GetTranslationKey("BAPI_MSG", "Modded\nSaved Games");
                addSaveManager.Invoke(handler, new object[] { saveManagerName });
            }
            else if (moddedHighscoreType != null && MTM101BaldiDevAPI.HighscoreHandler == SavedGameDataHandler.Modded)
            {
                MethodInfo addHighscore = genericAddCategory.MakeGenericMethod(moddedHighscoreType);
                string highscoreName = BBPC.Plugin.Instance.GetTranslationKey("BAPI_highscore", "Modded\nHighscores");
                addHighscore.Invoke(handler, new object[] { highscoreName });
            }
            else
            {
                return true;
            }
            return false;
        }

        [HarmonyPatch("ShiftMenu")]
        [HarmonyPostfix]
        public static void ShiftMenuPostfix(TextMeshProUGUI ___seedText, ref int ___internalIndex)
        {
            if (___seedText == null) return;
            var fileManager = Singleton<ModdedFileManager>.Instance;
            int externalIndex = Singleton<ModdedFileManager>.Instance.saveIndexes[___internalIndex];
            var game = fileManager.saveDatas[externalIndex];
            ___seedText.text = game.hasFile ? BBPC.Plugin.Instance.GetTranslationKey("BAPI_SEED", "SEED: ") + game.seed : BBPC.Plugin.Instance.GetTranslationKey("BAPI_NO_DATA", "NO DATA");
        }

        [HarmonyPatch("SwitchToWarning")]
        [HarmonyPostfix]
        public static void SwitchToWarningPostfix(string textToDisplay, TextMeshProUGUI ___warnText)
        {
            if (___warnText == null) return;
            if (textToDisplay == "Deleting a saved game <i>CANNOT</i> be undone!\nAre you sure?")
            {
                ___warnText.text = BBPC.Plugin.Instance.GetTranslationKey("BAPI_WARNING", "<b>WARNING!</b>\n") + BBPC.Plugin.Instance.GetTranslationKey("BAPI_DELWARN", textToDisplay);
            }
            else if (textToDisplay == "This will transfer the game to this save, not duplicate it! If you transfer, you won't be able to load this save with the old mods! Are you sure?")
            {
                ___warnText.text = BBPC.Plugin.Instance.GetTranslationKey("BAPI_WARNING", "<b>WARNING!</b>\n") + BBPC.Plugin.Instance.GetTranslationKey("BAPI_TRANSFERWARN", textToDisplay);
            }
        }

        [HarmonyPatch("Build")]
        [HarmonyPostfix]
        public static void BuildPostfix(GameObject ___mainScreen, GameObject ___warnScreen, StandardMenuButton ___deleteButton, StandardMenuButton ___transferButton)
        {
            if (___mainScreen == null || ___warnScreen == null || ___deleteButton == null || ___transferButton == null) return;
            Transform modListHeaderTransform = ___mainScreen.transform.Find("ModListHeader");
            if (modListHeaderTransform == null) return;
            GameObject modListHeader = modListHeaderTransform.gameObject;
            modListHeader.ApplyLocalization("BAPI_USEDMODS", true);
            ___deleteButton.gameObject.ApplyLocalization("BAPI_but_Delete", true);
            ___transferButton.gameObject.ApplyLocalization("BAPI_but_ttcg", true);
            Transform YesButtonTransform = ___warnScreen.transform.Find("YesButton");
            Transform NoButtonTransform = ___warnScreen.transform.Find("NoButton");
            if (YesButtonTransform == null || NoButtonTransform == null) return;
            GameObject yesButton = YesButtonTransform.gameObject;
            GameObject noButton = NoButtonTransform.gameObject;
            yesButton.ApplyLocalization("BAPI_but_YES", true);
            noButton.ApplyLocalization("BAPI_but_NO", true);
        }
    }
}
