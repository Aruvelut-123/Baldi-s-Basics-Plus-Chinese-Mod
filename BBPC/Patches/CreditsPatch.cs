using HarmonyLib;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.ErrorHandler;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BBPC.Patches
{
    [HarmonyPatch]
    public class CreditsPatch
    {
        private static readonly Dictionary<string, string> localizationKeys = new Dictionary<string, string>
        {
            { "Main Credits (5)/Text", "BBPC_Credits_ThankYouText" },
            { "Main Credits (3.5)/Text", "BBPC_Credits_SoundsFromText" },
            { "Main Credits (3.5)/TrademarkText", "BBPC_Credits_WarnerDisclaimerText" },
            { "Main Credits (2)/Text", "BBPC_Credits_TestingFeedbackText" },
            { "Main Credits (2)/Text (1)", "BBPC_Credits_TutorialsText" },
            { "Main Credits (2)/Text (2)", "BBPC_Credits_OtherTestersText" },
            { "Main Credits (1)/Text", "BBPC_Credits_VoicesText" },
            { "Main Credits (1)/Text (1)", "BBPC_Credits_ArtistsText" },
            { "Main Credits (4)/Text", "BBPC_Credits_MusicText" },
            { "Main Credits (4)/Text (1)", "BBPC_Credits_SpecialThanksText" },
            { "Main Credits (4)/Text (2)", "BBPC_Credits_BibleVerseText" },
            { "Main Credits (3)/Text", "BBPC_Credits_ToolsText" },
            { "Main Credits (3)/Text (1)", "BBPC_Credits_AssetsText" },
            { "Main Credits (3.75)/Text", "BBPC_Credits_OpenSourceText" },
            { "Main Credits (3.75)/LicenseText", "BBPC_Credits_LicenseText" },
            { "Main Credits/Text", "BBPC_Credits_MainTitleText" },
            { "Main Credits/TrademarkText", "BBPC_Credits_UnityDisclaimerText" }
        };
        
        [HarmonyPatch(typeof(SceneManager), "LoadScene", new[] { typeof(string) })]
        private static class LoadScenePatch
        {
            [HarmonyPostfix]
            private static void Postfix(string sceneName)
            {
                if (sceneName == "Credits")
                {
                    GameObject patchInitializer = new GameObject("CreditsPatchInitializer");
                    patchInitializer.AddComponent<CreditsPatchInitializer>();
                    Object.DontDestroyOnLoad(patchInitializer);
                }
            }
        }

        [HarmonyPatch(typeof(Credits), "Start")]
        private static class CreditsStartPatch
        {
            [HarmonyPrefix]
            private static void Prefix(Credits __instance)
            {
                if (__instance.screens != null && __instance.screens.Count > 0)
                {
                    JObject credit_json = Credit.credit_json;
                    int num = 0;
                    int num2 = 1;
                    GameObject gameObject = __instance.screens[0].gameObject;
                    API.Logger.Info("Get first screen object: " + gameObject.name);
                    foreach (JToken jtoken in credit_json["pages"])
                    {
                        JObject jobject = (JObject)jtoken;
                        GameObject gameObject2 = GameObject.Instantiate(gameObject, gameObject.transform);
                        gameObject2.name = "ExtraCreditsScreen(" + num.ToString() + ")";
                        TMP_Text[] componentsInChildren = gameObject2.GetComponentsInChildren<TMP_Text>();
                        if (componentsInChildren != null && componentsInChildren.Length != 0)
                        {
                            TMP_Text tmp_Text = componentsInChildren[0];
                            GameObject.Destroy(componentsInChildren[1]);
                            string text = "";
                            string text2 = "";
                            foreach (string text3 in Credit.sponsers)
                            {
                                text2 = text2 + text3 + "\n";
                            }
                            foreach (JToken jtoken2 in jobject["text"])
                            {
                                if (jtoken2.ToString().Contains("{AFDIAN_SPONSERS}"))
                                {
                                    text = text + text2 + "\n";
                                }
                                else
                                {
                                    text = text + jtoken2.ToString() + "\n";
                                }
                            }
                            tmp_Text.text = text;
                            tmp_Text.fontSize = 24f;
                            tmp_Text.alignment = TextAlignmentOptions.Center;
                            tmp_Text.color = Color.white;
                        }
                        __instance.screens.Insert(num2, gameObject2.GetComponent<Canvas>());
                        gameObject2.transform.SetParent(null);
                        gameObject2.SetActive(false);
                        API.Logger.Info(num2.ToString());
                        API.Logger.Info(gameObject2.gameObject.name);
                        API.Logger.Info("Screens list (" + __instance.screens.Count.ToString() + " total): ");
                        foreach (Canvas canvas in __instance.screens)
                        {
                            API.Logger.Info("- " + canvas.name);
                        }
                        num++;
                        num2++;
                    }
                }
            }

            [HarmonyPostfix]
            private static void Postfix(Credits __instance)
            {
                __instance.StartCoroutine(InitializeLocalization(__instance));
            }

            private static IEnumerator InitializeLocalization(Credits credits)
            {
                yield return null;

                ApplyLocalizationToAllCreditsObjects();
            }
        }

        [HarmonyPatch(typeof(Credits), "CreditsScroll")]
        private static class CreditsScrollPatch
        {
            [HarmonyPrefix]
            private static void Prefix(Credits __instance)
            {
                 ApplyLocalizationToAllCreditsObjects();
            }
        }

        [HarmonyPatch(typeof(GameObject), "SetActive")]
        private static class GameObjectSetActivePatch
        {
            [HarmonyPostfix]
            private static void Postfix(GameObject __instance, bool value)
            {
                if (value && SceneManager.GetActiveScene().name == "Credits" &&
                    __instance.name.StartsWith("Main Credits"))
                {
                    ApplyLocalizationDirectly(__instance.transform);
                }
            }
        }

        [HarmonyPatch(typeof(Credits), "Update")]
        private static class CreditsUpdatePatch
        {
            [HarmonyPostfix]
            private static void Postfix(Credits __instance)
            {
                GameObject gameObject = GameObject.Find("ExtraCreditsScreen(0)");
                GameObject gameObject2 = GameObject.Find("All Backers");
                GameObject gameObject3 = GameObject.Find("Main Credits");
                if (gameObject != null && gameObject2 != null)
                {
                    bool flag = gameObject.activeSelf && gameObject2.activeSelf;
                    gameObject2.SetActive(!flag);
                }
                else if (gameObject == null && gameObject2 != null && gameObject3 != null)
                {
                    bool flag = gameObject3.activeSelf && gameObject2.activeSelf;
                    gameObject2.SetActive(!flag);
                }
            }
        }

        public static void ApplyLocalizationToCredits()
        {
            ApplyLocalizationToAllCreditsObjects();
        }

        private static void ApplyLocalizationToAllCreditsObjects()
        {

            Canvas[] screens = Resources.FindObjectsOfTypeAll<Canvas>();
            foreach (Canvas screen in screens)
            {
                if (screen.name.StartsWith("Main Credits"))
                {
                    ApplyLocalizationDirectly(screen.transform);

                    ProcessChildren(screen.transform);
                }
            }
        }

        private static void ProcessChildren(Transform parent)
        {
            foreach (Transform child in parent)
            {
                ApplyLocalizationDirectly(child);
                ProcessChildren(child);
            }
        }

        private static void ApplyLocalizationDirectly(Transform obj)
        {
            foreach (var kvp in localizationKeys)
            {
                string fullPath = GetFullPath(obj);

                if (fullPath == kvp.Key)
                {
                    ApplyLocalizationToComponent(obj.gameObject, kvp.Value);
                    break;
                }
            }
        }

        private static string GetFullPath(Transform obj)
        {
            if (obj.parent == null || obj.parent.name.Contains("Canvas"))
            {
                return obj.name;
            }
            else
            {
                return obj.parent.name + "/" + obj.name;
            }
        }

        private static void ApplyLocalizationToComponent(GameObject textObject, string key)
        {
            TextMeshProUGUI textComponent = textObject.GetComponent<TextMeshProUGUI>();
            if (textComponent != null)
            {
                TextLocalizer localizer = textObject.GetComponent<TextLocalizer>() ?? textObject.AddComponent<TextLocalizer>();
                localizer.key = key;

                localizer.RefreshLocalization();
            }
        }
    }

    public class CreditsPatchInitializer : MonoBehaviour
    {
        private int frameCounter = 0;
        private readonly int framesToWait = 2;

        private void Update()
        {
            frameCounter++;

            if (frameCounter > framesToWait)
            {
                CreditsPatch.ApplyLocalizationToCredits();
                Destroy(this);
            }
        }
    }
    public class Credit
    {
        private string mod_path;
        private string credits_page = null;
        private string credits_default = "{\r\n    \"pages\": [\r\n        {\r\n            \"text\": [\r\n                \"<b>BB+汉化模组</b>\",\r\n                \"\\n\",\r\n                \"汉化模组/安装程序:\",\r\n                \"Baymaxawa\",\r\n                \"文本/贴图汉化:\",\r\n                \"MMZ\"\r\n            ]\r\n        },\r\n        {\r\n            \"text\": [\r\n                \"<b>BB+汉化模组</b>\",\r\n                \"\\n\",\r\n                \"润色: 馒\\n\",\r\n                \"TMP字体: cgq\\n\",\r\n                \"特别鸣谢: ChatGPT、Deepseek\"\r\n            ]\r\n        },\r\n        {\r\n            \"text\": [\r\n                \"<b>BB+汉化模组</b>\",\r\n                \"\\n\",\r\n                \"感谢所有在群内参与测试和提供的人员!\",\r\n                \"没有你们很难做到这里!\"\r\n            ]\r\n        },\r\n        {\r\n            \"text\": [\r\n                \"<b>BB+汉化模组 赞助人员名单</b>\",\r\n                \"\\n\",\r\n                \"{AFDIAN_SPONSERS}\"\r\n            ]\r\n        }\r\n    ]\r\n}";
        public static JObject credit_json;
        public static string[] sponsers = new string[] { "爱发电用户_e57b1", "爱发电用户_40217", "Mrothen" };

        public Credit(Plugin plug)
        {
            this.mod_path = AssetLoader.GetModPath(plug);
            this.credits_page = Path.Combine(this.mod_path, "Data", API.ConfigManager.currect_lang.Value, "Credits.json");
            if (!File.Exists(this.credits_page)) this.credits_page = Path.Combine(this.mod_path, "Data", "SChinese", "Credits.json");
            this.init();
        }

        private void init()
        {
            try
            {
                bool flag = this.mod_path != null && this.mod_path != "";
                if (flag)
                {
                    bool flag2 = !Directory.Exists(this.mod_path);
                    if (flag2)
                    {
                        Directory.CreateDirectory(this.mod_path);
                    }
                    bool flag3 = !File.Exists(this.credits_page);
                    if (flag3)
                    {
                        byte[] bytes = Encoding.UTF8.GetBytes(this.credits_default);
                        using (FileStream fileStream = new FileStream(this.credits_page, FileMode.Create, FileAccess.Write))
                        {
                            fileStream.Write(bytes, 0, bytes.Length);
                        }
                        StreamReader streamReader = File.OpenText(this.credits_page);
                        JsonTextReader jsonTextReader = new JsonTextReader(streamReader);
                        Credit.credit_json = (JObject)JToken.ReadFrom(jsonTextReader);
                        streamReader.Close();
                    }
                    else
                    {
                        StreamReader streamReader2 = File.OpenText(this.credits_page);
                        JsonTextReader jsonTextReader2 = new JsonTextReader(streamReader2);
                        Credit.credit_json = (JObject)JToken.ReadFrom(jsonTextReader2);
                        streamReader2.Close();
                    }
                }
                else
                {
                    API.Logger.Error("Error when trying to get mod_path!");
                }
            }
            catch (System.Exception ex)
            {
                API.Logger.Error(ex.Message);
            }
        }

        public void reload()
        {
            bool flag = this.mod_path != null && this.mod_path != "";
            if (flag)
            {
                bool flag2 = !File.Exists(this.credits_page);
                if (flag2)
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(this.credits_default);
                    using (FileStream fileStream = new FileStream(this.credits_page, FileMode.Create, FileAccess.Write))
                    {
                        fileStream.Write(bytes, 0, bytes.Length);
                    }
                    StreamReader streamReader = File.OpenText(this.credits_page);
                    JsonTextReader jsonTextReader = new JsonTextReader(streamReader);
                    Credit.credit_json = (JObject)JToken.ReadFrom(jsonTextReader);
                    streamReader.Close();
                }
                else
                {
                    StreamReader streamReader2 = File.OpenText(this.credits_page);
                    JsonTextReader jsonTextReader2 = new JsonTextReader(streamReader2);
                    Credit.credit_json = (JObject)JToken.ReadFrom(jsonTextReader2);
                    streamReader2.Close();
                }
            }
            else
            {
                ErrorDisplayer.allErrorDisplayers[0].ShowError("Error when trying to get mod_path", 10f);
            }

        }

    }
}