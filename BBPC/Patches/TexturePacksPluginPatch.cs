using BaldiTexturePacks;
using HarmonyLib;
using MTM101BaldAPI.OptionsAPI;

namespace BBPC.TextureExtension.Patches
{
    [HarmonyPatch(typeof(TexturePacksPlugin))]
    public class TexturePacksPluginPatch
    {
        [HarmonyPatch("AddCategory")]
        [HarmonyPrefix]
        public static bool AddCategoryPrefix(OptionsMenu __instance, CustomOptionsHandler handler)
        {
            if (Plugin.IsEnglish) return true;
            if (Singleton<CoreGameManager>.Instance == null)
            {
                handler.AddCategory<PackManagerScreen>(BBPC.Plugin.Instance.GetTranslationKey("Texture_options_title", "Texture\nPack"));
                return false;
            }
            return true;
        }
    }
}
