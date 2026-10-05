using System;
using HarmonyLib;

namespace BBPC.Compat
{
    /// <summary>
    /// Applies optional extension compatibility patches without compile-time references.
    /// Each extension is isolated so one incompatible ABI cannot disable the others.
    /// </summary>
    public static class ExtensionCompat
    {
        private static readonly Action<Harmony>[] ApplyActions =
        {
            ChallengeJarCompat.Apply,
            ModManagerCompat.Apply,
            NullStyleCompat.Apply,
            PlusLevelStudioCompat.Apply,
            TexturePackCompat.Apply
        };

        public static void ApplyAll(Harmony harmony)
        {
            if (harmony == null)
            {
                return;
            }

            foreach (Action<Harmony> apply in ApplyActions)
            {
                try
                {
                    apply(harmony);
                }
                catch (Exception ex)
                {
                    API.Logger.Error($"Optional extension compatibility patch failed: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }
    }
}
