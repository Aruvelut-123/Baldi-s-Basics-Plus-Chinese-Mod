using HarmonyLib;

using BBPC.API;

namespace BBPC.Compat
{
    /// <summary>
    /// 扩展兼容层统一入口。原先这些逻辑散落�?5 个独立扩展插�?    /// （challengejar / ModManager / nullstyle / pluslevelstudio / texturepack）里�?    /// 现已全部并入 main：运行时检测对应模组是否已加载，加载了才挂载反射补丁�?    /// 所有补丁均为手�?Harmony.Patch，main 编译时零模组引用�?    /// </summary>
    public static class ExtensionCompat
    {
        public static void ApplyAll(Harmony harmony)
        {
            if (harmony == null) return;

            ChallengeJarCompat.Apply(harmony);
            ModManagerCompat.Apply(harmony);
            NullStyleCompat.Apply(harmony);
            PlusLevelStudioCompat.Apply(harmony);
            TexturePackCompat.Apply(harmony);
        }
    }
}

