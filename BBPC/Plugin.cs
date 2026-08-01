using BBPC.EditorExtension.API;
using BBPC.EditorExtension.Patches;
using BepInEx;
using HarmonyLib;
using MTM101BaldAPI;

namespace BBPC.EditorExtension
{
    [BepInPlugin(ExtensionInfo.ModGuid, ExtensionInfo.ModName, ExtensionInfo.ModVersion)]
    [BepInDependency("mtm101.rulerp.bbplus.baldidevapi", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(BBPC.API.BBPCTemp.ModGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("mtm101.rulerp.baldiplus.levelstudioloader", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("mtm101.rulerp.baldiplus.levelstudio", BepInDependency.DependencyFlags.HardDependency)]
    [BepInProcess("BALDI.exe")]
    public sealed class Plugin : BaseUnityPlugin
    {
        private Harmony? harmony;
        internal static bool IsEnglish => BBPC.API.BBPCTemp.is_eng;

        private void Awake()
        {
            API.Logger.Initialize(Logger);
            PathPatch.Initialize();
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
