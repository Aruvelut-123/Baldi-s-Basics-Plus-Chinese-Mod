using BBPC.API;
using HarmonyLib;
using System;

namespace BBPC.Patches
{
    [HarmonyPatch]
    public class InputManagerPatch
    {
        [HarmonyPatch(typeof(InputManager), "GetInputButtonName", new Type[] { typeof(string), typeof(bool), typeof(string[]) })]
        [HarmonyPostfix]
        private static void Postfix(ref string __result)
        {
            if (ConfigManager.currect_lang.Value == "SChinese")
            {
                if (__result == "Left Mouse Button") __result = "鼠标左键";
                else if (__result == "Right Mouse Button") __result = "鼠标右键";
                else if (__result == "Mouse Horizontal") __result = "移动鼠标";
                else if (__result == "Mouse Wheel") __result = "鼠标滚轮";
                else if (__result == "Space") __result = "空格";
                else if (__result == "Up Arrow") __result = "上方向键";
                else if (__result == "Down Arrow") __result = "下方向键";
                else if (__result == "Left Shift") __result = "左 Shift";
                else if (__result == "Left Ctrl") __result = "左 Ctrl";
                else if (__result == "Right Shift") __result = "右 Shift";
                else if (__result == "Right Ctrl") __result = "右 Ctrl";
            }
        }
    }
}
