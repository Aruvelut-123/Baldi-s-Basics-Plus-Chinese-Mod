using BBPC.API;
using HarmonyLib;
using MTM101BaldAPI;
using System;
using UnityEngine;

namespace BBPC
{
    /// <summary>
    /// 保护性补丁：拦截 Material.SetFloat(string, float) 与 Material.SetFloat(int, float)。
    /// Unity TMP 引擎会在 TextMeshPro.LoadFontAsset / TextMeshProUGUI.SetCulling 中
    /// 对字体材质无条件调用 SetFloat("_CullMode", ...)，而 COMIC_Pro Material 使用的
    /// TextMeshPro/Bitmap shader 没有 _CullMode 属性，导致引擎在日志里刷
    /// "Material ... doesn't have a float or range property '_CullMode'" error。
    /// 此补丁在属性不存在时直接跳过，消除日志噪音且不影响任何正常渲染。
    /// </summary>
    [ConditionalPatchAlways]
    [HarmonyPatch(typeof(Material), "SetFloat", new Type[] { typeof(string), typeof(float) })]
    internal static class MaterialSetFloatPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(Material __instance, string name)
        {
            if (__instance == null) return true;

            // 诊断：对 COMIC 材质的所有 SetFloat 调用打 Debug 日志，确认补丁是否被执行。
            if (__instance.name != null && __instance.name.Contains("COMIC"))
            {
                bool hasProp = !string.IsNullOrEmpty(name) && __instance.HasProperty(name);
                API.Logger.Debug($"[SetFloatDiag] 材质 '{__instance.name}' 调用 SetFloat('{name}',...) 属性存在={hasProp}");
            }

            // 字符串重载不存在该属性时，SetFloat 只会打印错误并不会生效。
            // 直接跳过以消除无意义的引擎误报日志（属性仍由 HasProperty 的用户代码自行保护）。
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            if (!__instance.HasProperty(name))
            {
                API.Logger.Debug($"跳过对材质 '{__instance.name}' 设置不存在的属性 '{name}'");
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// 补充拦截：Material.SetFloat(int, float) 重载。
    /// 部分引擎/代码路径会用 Shader.PropertyToID 缓存后的 nameID 调用 SetFloat。
    /// 直接用 HasProperty(nameID) 判断属性是否存在，不存在则跳过，消除引擎误报。
    /// </summary>
    [ConditionalPatchAlways]
    [HarmonyPatch(typeof(Material), "SetFloat", new Type[] { typeof(int), typeof(float) })]
    internal static class MaterialSetFloatIDPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(Material __instance, int nameID)
        {
            if (__instance == null) return true;

            // 诊断：对 COMIC 材质的所有 SetFloat(int) 调用打 Debug 日志，确认调用路径。
            if (__instance.name != null && __instance.name.Contains("COMIC"))
            {
                API.Logger.Debug($"[SetFloatDiagID] 材质 '{__instance.name}' 调用 SetFloat(0x{nameID:X8},...) 属性存在={__instance.HasProperty(nameID)}");
            }

            // 核心修复：属性不存在于该材质时，native SetFloat 会打印误报错误且不生效，
            // 直接跳过以消除日志噪音。
            if (!__instance.HasProperty(nameID))
            {
                API.Logger.Debug($"跳过对材质 '{__instance.name}' 设置不存在的属性ID 0x{nameID:X8}");
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// 拦截 Material.GetFloat(string) 与 Material.GetFloat(int)。
    /// Unity 原生 GetFloat 在属性不存在时会打印与 SetFloat 完全相同的
    /// "doesn't have a float or range property" 错误（TMP_SubMesh 等路径会
    /// 检查 A 材质、却对 B 材质（fontSharedMaterial）直接 GetFloat）。
    /// 属性不存在时返回默认值 0 并跳过原生调用，消除误报且保持语义等价。
    /// </summary>
    [ConditionalPatchAlways]
    [HarmonyPatch(typeof(Material), "GetFloat", new Type[] { typeof(string) })]
    internal static class MaterialGetFloatPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(Material __instance, string name, ref float __result)
        {
            if (__instance == null) return true;

            if (__instance.name != null && __instance.name.Contains("COMIC"))
            {
                bool hasProp = !string.IsNullOrEmpty(name) && __instance.HasProperty(name);
                API.Logger.Debug($"[GetFloatDiag] 材质 '{__instance.name}' 调用 GetFloat('{name}') 属性存在={hasProp}");
            }

            if (string.IsNullOrEmpty(name))
            {
                __result = 0f;
                return false;
            }

            if (!__instance.HasProperty(name))
            {
                API.Logger.Debug($"GetFloat 跳过材质 '{__instance.name}' 的不存在属性 '{name}'，返回 0");
                __result = 0f;
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// 拦截 Material.GetFloat(int) 重载（Shader.PropertyToID 缓存路径）。
    /// </summary>
    [ConditionalPatchAlways]
    [HarmonyPatch(typeof(Material), "GetFloat", new Type[] { typeof(int) })]
    internal static class MaterialGetFloatIDPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(Material __instance, int nameID, ref float __result)
        {
            if (__instance == null) return true;

            if (__instance.name != null && __instance.name.Contains("COMIC"))
            {
                API.Logger.Debug($"[GetFloatDiagID] 材质 '{__instance.name}' 调用 GetFloat(0x{nameID:X8}) 属性存在={__instance.HasProperty(nameID)}");
            }

            if (!__instance.HasProperty(nameID))
            {
                API.Logger.Debug($"GetFloat 跳过材质 '{__instance.name}' 的不存在属性ID 0x{nameID:X8}，返回 0");
                __result = 0f;
                return false;
            }

            return true;
        }
    }
}
