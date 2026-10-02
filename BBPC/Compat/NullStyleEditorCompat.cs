using BepInEx.Bootstrap;
using HarmonyLib;
using System;

using BBPC.API;

namespace BBPC.Compat
{
    /// <summary>
    /// NullStyle（levs_kittne.baldiplus.null）扩展兼容层 —�?编辑器工具描述部分�?    /// �?NULL 的编辑器工具（NullTool / NullGlitchTool / NullProjectileTool）的
    /// titleKey / descKey 属�?getter 注入翻译 key�?    /// 原实现：feat/nullstyle 分支 NullStyleEditorPatch.cs�?    /// </summary>
    public static class NullStyleEditorCompat
    {
        public const string ModGuid = "levs_kittne.baldiplus.null";
        private const string AssemblyName = "NullStyleInBBPlus";

        private static bool _applied;

        public static void Apply(Harmony harmony)
        {
            if (_applied || harmony == null) return;
            if (!Chainloader.PluginInfos.ContainsKey(ModGuid)) return;

            Type? nullTool = CompatReflection.FindTypeBySimpleName(AssemblyName, "NullTool");
            Type? nullGlitchTool = CompatReflection.FindTypeBySimpleName(AssemblyName, "NullGlitchTool");
            Type? nullProjectileTool = CompatReflection.FindTypeBySimpleName(AssemblyName, "NullProjectileTool");

            if (nullTool != null)
                PatchGetter(harmony, nullTool, "descKey", nameof(NullToolDescKeyPostfix));
            if (nullGlitchTool != null)
                PatchGetter(harmony, nullGlitchTool, "descKey", nameof(NullGlitchToolDescKeyPostfix));
            if (nullProjectileTool != null)
            {
                PatchGetter(harmony, nullProjectileTool, "titleKey", nameof(NullProjectileToolTitleKeyPostfix));
                PatchGetter(harmony, nullProjectileTool, "descKey", nameof(NullProjectileToolDescKeyPostfix));
            }

            _applied = true;
            API.Logger.Info($"NullStyle editor compat applied (nullTool={nullTool != null}, glitch={nullGlitchTool != null}, proj={nullProjectileTool != null}).");
        }

        private static void PatchGetter(Harmony harmony, Type type, string propertyName, string postfixMethod)
        {
            var prop = type.GetProperty(propertyName,
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static);
            var getter = prop?.GetGetMethod(true);
            if (getter != null)
                harmony.Patch(getter, postfix: new HarmonyMethod(typeof(NullStyleEditorCompat), postfixMethod));
        }

        private static void NullToolDescKeyPostfix(ref string __result)
        {
            if (BBPCTemp.is_eng) return;
            __result = Plugin.Instance.GetTranslationKey("NULL_NULL_EditorDesc", "The main antagonist.");
        }

        private static void NullGlitchToolDescKeyPostfix(ref string __result)
        {
            if (BBPCTemp.is_eng) return;
            __result = Plugin.Instance.GetTranslationKey("NULL_NULLGlitch_EditorDesc", "A glitchy variant.");
        }

        private static void NullProjectileToolTitleKeyPostfix(ref string __result)
        {
            if (BBPCTemp.is_eng) return;
            __result = Plugin.Instance.GetTranslationKey("NULL_ProjectileTool_EditorTitle", "Projectile Spawn Point");
        }

        private static void NullProjectileToolDescKeyPostfix(ref string __result)
        {
            if (BBPCTemp.is_eng) return;
            __result = Plugin.Instance.GetTranslationKey("NULL_ProjectileTool_EditorDesc", "Place the projectile exactly where you really need it!");
        }
    }
}

