using HarmonyLib;
using MTM101BaldAPI;
using System;
using UnityEngine;

namespace BBPC
{
    /// <summary>
    /// Skips Unity Material float access when the shader does not expose the
    /// requested property. TMP's bitmap shader does not define _CullMode, and
    /// Unity otherwise emits a native error for every access.
    /// </summary>
    internal static class MaterialPropertyGuard
    {
        public static bool HasProperty(Material material, string name)
        {
            return material != null && !string.IsNullOrEmpty(name) && material.HasProperty(name);
        }

        public static bool HasProperty(Material material, int nameId)
        {
            return material != null && material.HasProperty(nameId);
        }
    }

    [ConditionalPatchAlways]
    [HarmonyPatch(typeof(Material), "SetFloat", new[] { typeof(string), typeof(float) })]
    internal static class MaterialSetFloatPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(Material __instance, string name)
        {
            return __instance == null || MaterialPropertyGuard.HasProperty(__instance, name);
        }
    }

    [ConditionalPatchAlways]
    [HarmonyPatch(typeof(Material), "SetFloat", new[] { typeof(int), typeof(float) })]
    internal static class MaterialSetFloatIDPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(Material __instance, int nameID)
        {
            return __instance == null || MaterialPropertyGuard.HasProperty(__instance, nameID);
        }
    }

    [ConditionalPatchAlways]
    [HarmonyPatch(typeof(Material), "GetFloat", new[] { typeof(string) })]
    internal static class MaterialGetFloatPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(Material __instance, string name, ref float __result)
        {
            if (__instance == null || MaterialPropertyGuard.HasProperty(__instance, name))
            {
                return true;
            }

            __result = 0f;
            return false;
        }
    }

    [ConditionalPatchAlways]
    [HarmonyPatch(typeof(Material), "GetFloat", new[] { typeof(int) })]
    internal static class MaterialGetFloatIDPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(Material __instance, int nameID, ref float __result)
        {
            if (__instance == null || MaterialPropertyGuard.HasProperty(__instance, nameID))
            {
                return true;
            }

            __result = 0f;
            return false;
        }
    }
}
