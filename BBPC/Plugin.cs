using BBPC.API;
using BepInEx;
using HarmonyLib;
using MTM101BaldAPI;
using UnityEngine;

namespace BBPC
{
    [BepInPlugin(BBPCTemp.ModGUID, BBPCTemp.ModName, BBPCTemp.ModVersion)]
    [BepInDependency("mtm101.rulerp.bbplus.baldidevapi", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("mtm101.rulerp.baldiplus.levelstudioloader", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("mtm101.rulerp.baldiplus.levelstudio", BepInDependency.DependencyFlags.HardDependency)]
    [BepInProcess("BALDI.exe")]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance { get; private set; } = null!;
        private Harmony? harmonyInstance = null!;

        private void Awake()
        {
            Instance = this;
            API.Logger.Init(Logger);

            API.Logger.Info($"插件 {BBPCTemp.ModName} 正在初始化...");

            new Harmony(BBPCTemp.ModGUID).PatchAllConditionals();

            API.Logger.Info($"Mod {MyPluginInfo.PLUGIN_NAME} is loaded!");
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
