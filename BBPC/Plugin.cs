using BBPC.NULLStyle.API;
using BepInEx;
using HarmonyLib;
using MTM101BaldAPI;
using UnityEngine;

namespace BBPC.NULLStyle
{
    [BepInPlugin(BBPCNULLTemp.ModGUID, BBPCNULLTemp.ModName, BBPCNULLTemp.ModVersion)]
    [BepInDependency("mtm101.rulerp.bbplus.baldidevapi", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.baymaxawa.bbpc", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("levs_kittne.baldiplus.null", BepInDependency.DependencyFlags.HardDependency)]
    [BepInProcess("BALDI.exe")]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance { get; private set; } = null!;
        private Harmony? harmonyInstance = null!;
        public static bool is_english = false;
        public static string current_lang = "SChinese";

        private void Awake()
        {
            Instance = this;
            API.Logger.Init(Logger);
            API.Logger.Info($"Extension {BBPCNULLTemp.ModName} is initializing...");
            is_english = BBPC.API.BBPCTemp.is_eng;
            current_lang = BBPC.API.ConfigManager.currect_lang.Value;
            Harmony harmony = new Harmony(BBPCNULLTemp.ModGUID);
            harmony.PatchAllConditionals();

            API.Logger.Info($"Extension {BBPCNULLTemp.ModName} loaded successfully!");
        }

        void OnDestroy()
        {
            if (harmonyInstance != null)
            {
                harmonyInstance.UnpatchSelf();
                harmonyInstance = null;
            }
        }

        public void des(GameObject obj)
        {
            Destroy(obj);
        }
    }
}
