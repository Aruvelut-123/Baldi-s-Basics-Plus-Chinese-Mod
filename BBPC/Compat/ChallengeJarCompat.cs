using BepInEx.Bootstrap;
using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;

using BBPC.API;

namespace BBPC.Compat
{
    /// <summary>
    /// ChallengeJar（bbplus.challengejar）扩展兼容层�?    /// 模组存在时，为其两个菜单应用本地化文本�?    /// 原实现：feat/challengejar 分支 ChallengeExtraMenuPatch / PickDifficultyMenuPatch�?    /// </summary>
    public static class ChallengeJarCompat
    {
        public const string ModGuid = "bbplus.challengejar";
        private const string AssemblyName = "ChallengeJar";

        private static bool _applied;

        private static readonly IReadOnlyDictionary<string, string> ChallengeExtraKeys =
            new Dictionary<string, string>
            {
                { "Invasion", "CJ_Invasion" },
                { "Locked", "CJ_Locked" },
                { "Robbing", "CJ_Robbing" },
                { "Placeholder", "CJ_Placeholder" }
            };

        private static readonly IReadOnlyDictionary<string, string> DifficultyKeys =
            new Dictionary<string, string>
            {
                { "Normal", "CJ_Normal" },
                { "Hard", "CJ_Hard" }
            };

        public static void Apply(Harmony harmony)
        {
            if (_applied || harmony == null) return;
            if (!Chainloader.PluginInfos.ContainsKey(ModGuid)) return;

            Type? extraMenu = CompatReflection.FindType(AssemblyName, "ChallengeJar.Menu.ChallengeExtraMenu");
            Type? difficultyMenu = CompatReflection.FindType(AssemblyName, "ChallengeJar.Menu.PickDifficultyMenu");

            if (extraMenu != null)
            {
                System.Reflection.MethodInfo? start = AccessTools.Method(extraMenu, "Start");
                if (start != null)
                    harmony.Patch(start, postfix: new HarmonyMethod(typeof(ChallengeJarCompat), nameof(ChallengeExtraMenuStartPostfix)));
            }

            if (difficultyMenu != null)
            {
                System.Reflection.MethodInfo? init = AccessTools.Method(difficultyMenu, "InitButtons");
                if (init != null)
                    harmony.Patch(init, postfix: new HarmonyMethod(typeof(ChallengeJarCompat), nameof(PickDifficultyMenuInitPostfix)));
            }

            _applied = true;
            API.Logger.Info($"ChallengeJar extension compat applied (extra={extraMenu != null}, difficulty={difficultyMenu != null}).");
        }

        private static void ChallengeExtraMenuStartPostfix(object __instance)
        {
            if (BBPCTemp.is_eng || __instance == null) return;
            if (__instance is Component comp)
                comp.transform.ApplyLocalizations(ChallengeExtraKeys, true);
        }

        private static void PickDifficultyMenuInitPostfix(object __instance)
        {
            if (BBPCTemp.is_eng || __instance == null) return;
            if (__instance is Component comp)
                comp.transform.ApplyLocalizations(DifficultyKeys, true);
        }
    }
}

