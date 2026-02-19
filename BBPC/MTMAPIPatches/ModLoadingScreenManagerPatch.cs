using HarmonyLib;
using MTM101BaldAPI;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using TMPro;
using UnityEngine;

namespace BBPC.MTMAPIPatches
{
    [HarmonyPatch(typeof(ModLoadingScreenManager))]
    public class ModLoadingScreenManagerPatch
    {
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        public static void StartPostfix(ModLoadingScreenManager __instance)
        {
            if (__instance.transform.Find("Text") == null) return;
            GameObject loadingText = __instance.transform.Find("Text").gameObject;
            TextLocalizer loadingTextTL = loadingText.AddComponent<TextLocalizer>();
            loadingTextTL.key = "BAPI_loading";
            loadingTextTL.RefreshLocalization();
        }
    }

    public static class MainLoadTranspiler
    {
        public static void Apply(Harmony harmony)
        {
            MethodInfo mainLoad = typeof(ModLoadingScreenManager).GetMethod("MainLoad",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (mainLoad == null)
            {
                API.Logger.Error("未找到 MainLoad 方法");
                return;
            }

            MethodBase moveNext = AccessTools.EnumeratorMoveNext(mainLoad);
            if (moveNext == null)
            {
                API.Logger.Error("无法获取 MoveNext 方法");
                return;
            }

            MethodInfo transpilerMethod = typeof(MainLoadTranspiler).GetMethod(
                nameof(Transpiler),
                BindingFlags.Static | BindingFlags.Public,
                null,
                new Type[] { typeof(IEnumerable<CodeInstruction>) },
                null);

            if (transpilerMethod == null)
            {
                API.Logger.Error("无法找到 Transpiler 方法");
                return;
            }

            harmony.Patch(moveNext, transpiler: new HarmonyMethod(transpilerMethod));
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            MethodInfo localizeMethod = AccessTools.Method(typeof(MainLoadTranspiler), nameof(LocalizeString));

            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldstr)
                {
                    var callInstruction = new CodeInstruction(OpCodes.Call, localizeMethod);
                    codes.Insert(i + 1, callInstruction);
                    i++;
                }
            }

            return codes;
        }

        private static string LocalizeString(string original)
        {
            // 示例映射：根据原字符串返回对应的翻译
            // 实际使用时，请根据你的翻译键完善此映射
            switch (original)
            {
                /*case "Loading Mod Assets... (":
                    return BBPC.Plugin.Instance.GetTranslationKey("mainload_converting", "Loading Mod Assets... (");
                case "Converting LevelObjects to CustomLevelObjects...":
                    return BBPC.Plugin.Instance.GetTranslationKey("mainload_converting", "Converting LevelObjects to CustomLevelObjects...");*/
                case "Invoking Mod Asset Pre-Loading... (":
                    return BBPC.Plugin.Instance.GetTranslationKey("BAPI_Load_Invoking", "Invoking Mod Asset Pre-Loading... (");
                case "Changing ":
                    return BBPC.Plugin.Instance.GetTranslationKey("BAPI_Load_Changing", "Changing ");
                /*case "Changing modded SceneObjects...":
                    return BBPC.Plugin.Instance.GetTranslationKey("BAPI_Load_Changing", "Changing modded SceneObjects...");
                case "Adding MIDIs...":
                    return BBPC.Plugin.Instance.GetTranslationKey("BAPI_Load_Changing", "Adding MIDIs...");*/
                case "Invoking Mod Asset Post-Loading... (":
                    return BBPC.Plugin.Instance.GetTranslationKey("BAPI_Load_Invokingpost", "Invoking Mod Asset Post-Loading... (");
                /*case "Reloading Localization...":
                    return BBPC.Plugin.Instance.GetTranslationKey("BAPI_Load_Invokingpost", "Reloading Localization...");
                case "Reloading highscores...":
                    return BBPC.Plugin.Instance.GetTranslationKey("BAPI_Load_Invokingpost", "Reloading highscores...");
                case "Invoking Mod Asset Finalizing... (":
                    return BBPC.Plugin.Instance.GetTranslationKey("BAPI_Load_Invokingpost", "Invoking Mod Asset Finalizing... (");*/
                default:
                    return original;
            }
        }
    }
}
