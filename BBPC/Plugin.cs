using BBPC.API;
using BepInEx;
using HarmonyLib;
using MTM101BaldAPI;
using UnityEngine;

namespace BBPC.ChallengeJarExtension
{
    [BepInPlugin(API.BBPCCJTemp.ModGUID, API.BBPCCJTemp.ModName, API.BBPCCJTemp.ModVersion)]
    [BepInDependency("mtm101.rulerp.bbplus.baldidevapi", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.baymaxawa.bbpc", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("bbplus.challengejar", BepInDependency.DependencyFlags.HardDependency)]
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

            API.Logger.Info($"插件 {API.BBPCCJTemp.ModName} 正在初始化...");

            is_english = BBPC.API.BBPCTemp.is_eng;
            current_lang = ConfigManager.currect_lang.Value;

            Harmony harmony = new Harmony(API.BBPCCJTemp.ModGUID);

            harmony.PatchAllConditionals();

            API.Logger.Info($"Mod {API.BBPCCJTemp.ModName} is loaded!");
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
