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
            if (__result == "Left Mouse Button")
            {
                __result = "鼠标左键";
            }
            else if (__result == "Right Mouse Button")
            {
                __result = "鼠标右键";
            }
        }
    }
}
