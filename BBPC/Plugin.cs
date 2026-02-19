using BBPC.API;
using BBPC.ModManagerExtension.API;
using BepInEx;
using HarmonyLib;
using MTM101BaldAPI;
using UnityEngine;

namespace BBPC.ModManagerExtension
{
    [BepInPlugin(BBPCMMTemp.ModGUID, BBPCMMTemp.ModName, BBPCMMTemp.ModVersion)]
    [BepInDependency("mtm101.rulerp.bbplus.baldidevapi", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.baymaxawa.bbpc", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("rost.moment.baldiplus.modmanager", BepInDependency.DependencyFlags.HardDependency)]
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

            API.Logger.Info($"插件 {BBPCMMTemp.ModName} 正在初始化...");

            is_english = BBPC.API.BBPCTemp.is_eng;
            current_lang = ConfigManager.currect_lang.Value;

            Harmony harmony = new Harmony(BBPCMMTemp.ModGUID);

            harmony.PatchAllConditionals();

            API.Logger.Info($"Mod {BBPCMMTemp.ModName} is loaded!");
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
