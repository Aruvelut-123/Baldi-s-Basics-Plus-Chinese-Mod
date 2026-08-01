using BBPC.NULLStyle.API;
using BepInEx;
using HarmonyLib;
using MTM101BaldAPI;

namespace BBPC.NULLStyle
{
    [BepInPlugin(ExtensionInfo.ModGuid, ExtensionInfo.ModName, ExtensionInfo.ModVersion)]
    [BepInDependency("mtm101.rulerp.bbplus.baldidevapi", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(BBPC.API.BBPCTemp.ModGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("levs_kittne.baldiplus.null", BepInDependency.DependencyFlags.HardDependency)]
    [BepInProcess("BALDI.exe")]
    public sealed class Plugin : BaseUnityPlugin
    {
        private Harmony? harmony;
        internal static bool IsEnglish => BBPC.API.BBPCTemp.is_eng;

        private void Awake()
        {
            API.Logger.Initialize(Logger);
            harmony = new Harmony(ExtensionInfo.ModGuid);
            harmony.PatchAllConditionals();
            API.Logger.Info($"Loaded {ExtensionInfo.ModName} {ExtensionInfo.ModVersion}");
        }

        private void OnDestroy()
        {
            harmony?.UnpatchSelf();
            harmony = null;
        }
    }
}
