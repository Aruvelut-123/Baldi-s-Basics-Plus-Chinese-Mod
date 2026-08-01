using BBPC.ExtensionTemplate.API;
using BepInEx;
using HarmonyLib;
using MTM101BaldAPI;

namespace BBPC.ExtensionTemplate
{
    [BepInPlugin(ExtensionInfo.ModGuid, ExtensionInfo.ModName, ExtensionInfo.ModVersion)]
    [BepInDependency("mtm101.rulerp.bbplus.baldidevapi", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(BBPC.API.BBPCTemp.ModGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInProcess("BALDI.exe")]
    public sealed class Plugin : BaseUnityPlugin
    {
        private Harmony? harmony;

        internal static bool IsEnglish => BBPC.API.BBPCTemp.is_eng;
        internal static string CurrentLanguage => BBPC.API.ConfigManager.currect_lang.Value;

        private void Awake()
        {
            API.Logger.Initialize(Logger);
            API.Logger.Info($"Loading {ExtensionInfo.ModName} {ExtensionInfo.ModVersion}");

            harmony = new Harmony(ExtensionInfo.ModGuid);
            harmony.PatchAllConditionals();
        }

        private void OnDestroy()
        {
            harmony?.UnpatchSelf();
            harmony = null;
        }
    }
}
