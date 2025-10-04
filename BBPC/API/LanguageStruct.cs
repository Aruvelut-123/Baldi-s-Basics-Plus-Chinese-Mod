using System.Collections.Generic;

namespace BBPC.API
{
    [System.Serializable]
    public class LanguageStruct
    {
        public string LanguageCodeName = "English";

        private Dictionary<string, string> localizationTable = new Dictionary<string, string>();

        public void AddKey(string key, string value)
        {
            if (!localizationTable.ContainsKey(key))
            {
                localizationTable.Add(key, value);
            }
            else
            {
                Logger.Warning($"键 {key} 已存在！\n请使用 language_LANGCODENAME.config 文件配置如何处理重复键：替换为新值或跳过。");
            }
        }

        public bool ContainsKey(string key)
        {
            return localizationTable.ContainsKey(key);
        }

        public bool ContainsValue(string value)
        {
            return localizationTable.ContainsValue(value);
        }

        public string GetLocalizatedText(string key)
        {
            return localizationTable[key];
        }
    }
}