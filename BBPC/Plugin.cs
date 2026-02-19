using BBPC.API;
using BBPC.PineDebugExtension.API;
using BepInEx;
using HarmonyLib;
using MTM101BaldAPI;
using UnityEngine;

namespace BBPC.PineDebugExtension
{
    [BepInPlugin(BBPCPDTemp.ModGUID, BBPCPDTemp.ModName, BBPCPDTemp.ModVersion)]
    [BepInDependency("mtm101.rulerp.bbplus.baldidevapi", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.baymaxawa.bbpc", BepInDependency.DependencyFlags.HardDependency)]
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

            API.Logger.Info($"插件 {BBPCPDTemp.ModName} 正在初始化...");

            is_english = BBPC.API.BBPCTemp.is_eng;
            current_lang = ConfigManager.currect_lang.Value;

            Harmony harmony = new Harmony(BBPCPDTemp.ModGUID);

            harmony.PatchAllConditionals();

            API.Logger.Info($"Mod {BBPCPDTemp.ModName} is loaded!");
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
