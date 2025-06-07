using MTM101BaldAPI.ErrorHandler;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Text;

namespace BBPC
{
    public class Credit
    {
        private string mod_path = Plugin.ModPath;
        private string credits_page = null;
        private string credits_default = "" +
            "{\r\n    \"pages\": [\r\n        {\r\n            \"text\": [\r\n                \"<b>BB+汉化模组</b>\",\r\n                \"\\n\",\r\n                \"主要汉化人员:\",\r\n                \"Baymaxawa & MEMZSystem32\"\r\n            ]\r\n        },\r\n        {\r\n            \"text\": [\r\n                \"<b>BB+汉化模组</b>\",\r\n                \"\\n\",\r\n                \"润色: 馒\\n\",\r\n                \"TMP字体: CMCGZP\\n\",\r\n                \"特别鸣谢: ChatGPT\"\r\n            ]\r\n        },\r\n        {\r\n            \"text\": [\r\n                \"<b>BB+汉化模组</b>\",\r\n                \"\\n\",\r\n                \"感谢所有在群内参与测试和提供的人员!\",\r\n                \"没有你们很难做到这里!\"\r\n            ]\r\n        },\r\n        {\r\n            \"text\": [\r\n                \"<b>BB+汉化模组 赞助人员名单</b>\",\r\n                \"\\n\",\r\n                \"{AFDIAN_SPONSERS}\"\r\n            ]\r\n        }\r\n    ]\r\n}" +
            "";
        public static JObject credit_json;
        public static string[] sponsers = [
            "爱发电用户_e57b1",
            "爱发电用户_40217",
            "Mrothen"
        ];

        public Credit(Plugin plug)
        {
            credits_page = Path.Combine(mod_path, "credits.json");
            init();
        }

        private void init()
        {
            try
            {
                if (mod_path != null && mod_path != "")
                {
                    if (!Directory.Exists(mod_path)) Directory.CreateDirectory(mod_path);
                    if (!File.Exists(credits_page))
                    {
                        byte[] data = Encoding.UTF8.GetBytes(credits_default);
                        using (FileStream fs = new FileStream(credits_page, FileMode.Create, FileAccess.Write))
                        {
                            fs.Write(data, 0, data.Length);
                        }
                        StreamReader file = File.OpenText(credits_page);
                        JsonTextReader reader = new JsonTextReader(file);
                        credit_json = (JObject)JToken.ReadFrom(reader);
                        file.Close();
                    }
                    else
                    {
                        StreamReader file = File.OpenText(credits_page);
                        JsonTextReader reader = new JsonTextReader(file);
                        credit_json = (JObject)JToken.ReadFrom(reader);
                        file.Close();
                    }
                }
                else
                {
                    Plugin.Logger.LogError("Error when trying to get mod_path!");
                }
            }
            catch (Exception e)
            {
                Plugin.Logger.LogError(e);
            }
        }

        public void reload()
        {
            if (mod_path != null && mod_path != "")
            {
                if (!File.Exists(credits_page))
                {
                    byte[] data = Encoding.UTF8.GetBytes(credits_default);
                    using (FileStream fs = new FileStream(credits_page, FileMode.Create, FileAccess.Write))
                    {
                        fs.Write(data, 0, data.Length);
                    }
                    StreamReader file = File.OpenText(credits_page);
                    JsonTextReader reader = new JsonTextReader(file);
                    credit_json = (JObject)JToken.ReadFrom(reader);
                    file.Close();
                }
                else
                {
                    StreamReader file = File.OpenText(credits_page);
                    JsonTextReader reader = new JsonTextReader(file);
                    credit_json = (JObject)JToken.ReadFrom(reader);
                    file.Close();
                }
            }
            else
            {
                ErrorDisplayer.allErrorDisplayers[0].ShowError("Error when trying to get mod_path", 10f);
            }
        }
    }
}
