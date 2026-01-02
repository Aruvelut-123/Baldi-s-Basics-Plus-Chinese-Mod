using MTM101BaldAPI.AssetTools;
using System.IO;

namespace BBPC.API
{
    public class CompatibilityHelper
    {
        public static bool IsModLangExists(string langFileName)
        {
            string currectLangFolder = Path.Combine(AssetLoader.GetModPath(Plugin.Instance), "Language", ConfigManager.currect_lang.Value);
            string modLangFileName = langFileName;
            if (Directory.Exists(currectLangFolder))
            {
                if (File.Exists(Path.Combine(currectLangFolder, modLangFileName)))
                {
                    Logger.Info("语言文件存在！继续应用汉化补丁...");
                    return true;
                }
                else
                {
                    Logger.Warning("语言文件 " + modLangFileName + " 不存在！跳过补丁应用！");
                    return false;
                }
            }
            else
            {
                Logger.Info("语言 " + ConfigManager.currect_lang.Value + "的对应文件夹不存在！跳过补丁应用！");
                return false;
            }
        }
    }
}
