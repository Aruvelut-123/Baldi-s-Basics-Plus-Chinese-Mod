using HarmonyLib;
using ModManager;
using MTM101BaldAPI.OptionsAPI;

namespace BBPC.ModManagerExtension.Patches
{
    [HarmonyPatch(typeof(BasePlugin))]
    public class BasePluginPatch
    {
        [HarmonyPatch("OnMenu")]
        [HarmonyPrefix]
        public static bool OnMenuPrefix(OptionsMenu menu, CustomOptionsHandler handler)
        {
            if (Plugin.is_english) return true;
            handler.AddCategory<ModManager.ModManager>(BBPC.Plugin.Instance.GetTranslationKey("MMg_ModManager", "Mods\nManager", Plugin.current_lang, true));
            return false;
        }
    }
}
