using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using TMPro;

namespace BBPC.Patches
{
    static internal class WarningScreenContainer
    {
        static internal (string, bool)[] screens => nonCriticalScreens.Select(x => (x, false)).ToArray().AddRangeToArray(criticalScreens.Select(x => (x, true)).ToArray()).ToArray();

        static internal List<string> nonCriticalScreens = new List<string>();

        static internal List<string> criticalScreens = new List<string>();

        static internal int currentPage = 0;

        static internal string pressAny = "";
    }

    /*[HarmonyPatch(typeof(WarningScreen))]
    [HarmonyPatch("Start")]
    [HarmonyPriority(800)]
    [HarmonyDebug]
    static class WarningScreenStartPatch
    {
        static bool Prefix(WarningScreen __instance)
        {
            string text = "";
            if (Singleton<InputManager>.Instance.SteamInputActive)
            {
                text = string.Format(Singleton<LocalizationManager>.Instance.GetLocalizedText("Men_Warning"), Singleton<InputManager>.Instance.GetInputButtonName("MouseSubmit", "Interface", false));
                WarningScreenContainer.pressAny = string.Format("按 {0} 继续", Singleton<InputManager>.Instance.GetInputButtonName("MouseSubmit", "Interface", false));
            }
            else
            {
                text = string.Format(Singleton<LocalizationManager>.Instance.GetLocalizedText("Men_Warning"), "任意键");
                WarningScreenContainer.pressAny = string.Format("按 {0} 继续", "任意键");
            }
            WarningScreenContainer.nonCriticalScreens.Insert(0, text);
            __instance.textBox.text = text;
            return false;
        }
    }

    [HarmonyPatch(typeof(WarningScreen))]
    [HarmonyPatch("Advance")]
    [HarmonyPriority(800)]
    [HarmonyDebug]
    static class WarningScreenAdvancePatch
    {
        static bool Prefix(WarningScreen __instance)
        {
            if (WarningScreenContainer.currentPage >= WarningScreenContainer.screens.Length)
            {
                return true;
            }
            if ((WarningScreenContainer.screens[WarningScreenContainer.currentPage].Item2) && ((WarningScreenContainer.currentPage + 1) >= WarningScreenContainer.screens.Length))
            {
                return false;
            }
            WarningScreenContainer.currentPage++;
            if (WarningScreenContainer.currentPage >= WarningScreenContainer.screens.Length)
            {
                return true;
            }
            Singleton<GlobalCam>.Instance.Transition(UiTransition.Dither, 0.01666667f);
            if (!WarningScreenContainer.screens[WarningScreenContainer.currentPage].Item2)
            {
                __instance.textBox.text = "<color=yellow>注意！</color>\n" + WarningScreenContainer.screens[WarningScreenContainer.currentPage].Item1 + "\n\n" + WarningScreenContainer.pressAny;
            }
            else
            {
                if (((WarningScreenContainer.currentPage + 1) < WarningScreenContainer.screens.Length))
                {
                    __instance.textBox.text = "<color=red>错误！</color>\n" + WarningScreenContainer.screens[WarningScreenContainer.currentPage].Item1 + "\n\n" + WarningScreenContainer.pressAny;
                }
                else
                {
                    __instance.textBox.text = "<color=red>错误！</color>\n" + WarningScreenContainer.screens[WarningScreenContainer.currentPage].Item1 + "\n\n按 ALT+F4 退出";
                }
            }
            return false;
        }
    }

    // 这个后置补丁会在MTM101BMDE准备好警告屏幕后执行
    // 我们只是将英文文本替换为中文
    [HarmonyPatch(typeof(WarningScreen))]
    [HarmonyDebug]
    internal class WarningScreen_Patch
    {
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        private static void Start_Postfix(WarningScreen __instance)
        {
            TranslateWarningText(__instance.textBox);
        }

        [HarmonyPatch("Advance")]
        [HarmonyPostfix]
        private static void Advance_Postfix(WarningScreen __instance)
        {
            TranslateWarningText(__instance.textBox);
        }

        private static void TranslateWarningText(TMP_Text textBox)
        {
            if (textBox == null) return;

            string currentText = textBox.text;

            // 翻译标题
            currentText = currentText.Replace("WARNING!", "注意！");
            currentText = currentText.Replace("ERROR!", "错误！");

            // 翻译标准警告文本（如果存在）
            if (currentText.Contains("This game is not suitable for children or those who are easily disturbed."))
            {
                currentText = "本游戏不适合儿童或心理承受能力较弱者。\n它包含突然的巨响和恐怖画面。";
            }

            // 翻译"按任意键继续"的提示
            if (currentText.Contains("PRESS ANY BUTTON TO CONTINUE"))
            {
                currentText = currentText.Replace("PRESS ANY BUTTON TO CONTINUE", "按任意键继续");
            }
            else
            {
                // 处理指定具体按键的情况
                currentText = System.Text.RegularExpressions.Regex.Replace(currentText, @"PRESS (.+) TO CONTINUE", "按 $1 继续");
            }

            // 翻译退出提示
            currentText = currentText.Replace("PRESS ALT+F4 TO EXIT", "按 ALT+F4 退出");

            textBox.text = currentText;
        }
    }*/
}