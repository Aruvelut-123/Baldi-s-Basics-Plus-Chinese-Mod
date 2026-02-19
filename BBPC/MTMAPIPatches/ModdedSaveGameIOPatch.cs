using HarmonyLib;
using MTM101BaldAPI.SaveSystem;
using System.Text;

namespace BBPC.MTMAPIPatches
{
    [HarmonyPatch(typeof(ModdedSaveGameIOBinary))]
    public class ModdedSaveGameIOPatch
    {
        [HarmonyPatch("DisplayTagsDefault")]
        [HarmonyPostfix]
        public static void Postfix(string[] tags, ref string __result)
        {
            if (tags.Length == 0)
            {
                __result = BBPC.Plugin.Instance.GetTranslationKey("BAPI_Mod_data_NoTag_Introduction", __result);
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine(BBPC.Plugin.Instance.GetTranslationKey("BAPI_Mod_data_Tag_Introduction", "<b>Tags:</b>"));
                for (int i = 0; i < tags.Length; i++)
                {
                    sb.AppendLine(tags[i]);
                }
                __result = sb.ToString();
            }
        }
    }
}
