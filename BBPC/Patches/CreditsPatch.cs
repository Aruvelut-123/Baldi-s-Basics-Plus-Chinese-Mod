using BBPC.API;
using MTM101BaldAPI;
using HarmonyLib;
using MTM101BaldAPI.AssetTools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BBPC.Patches
{
    [ConditionalPatchAlways]
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

        private static int cachedCreditsSceneHandle = -1;
        private static readonly List<Canvas> cachedCreditsScreens = new List<Canvas>();
        private static GameObject? cachedExtraCreditsScreen;
        private static GameObject? cachedAllBackers;
        private static GameObject? cachedMainCredits;
        
        [HarmonyPatch(typeof(SceneManager), "LoadScene", new[] { typeof(string) })]
        private static class LoadScenePatch
        {
            [HarmonyPostfix]
            private static void Postfix(string sceneName)
            {
                if (!string.Equals(sceneName, "Credits", StringComparison.Ordinal))
                {
                    return;
                }

                ResetSceneCaches();
                if (GameObject.Find("CreditsPatchInitializer") != null)
                {
                    return;
                }

                GameObject patchInitializer = new GameObject("CreditsPatchInitializer");
                patchInitializer.AddComponent<CreditsPatchInitializer>();
                UnityEngine.Object.DontDestroyOnLoad(patchInitializer);
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
                    JArray? pages = Credit.credit_json["pages"] as JArray;
                    if (pages == null)
                    {
                        API.Logger.Error("Credits data does not contain a pages array.");
                        return;
                    }

                    int num = 0;
                    int num2 = 1;
                    GameObject gameObject = __instance.screens[0].gameObject;
                    API.Logger.Info("Get first screen object: " + gameObject.name);
                    foreach (JToken jtoken in pages)
                    {
                        JObject jobject = (JObject)jtoken;
                        JArray? lines = jobject["text"] as JArray;
                        if (lines == null)
                        {
                            API.Logger.Warning("Skipping a credits page without a text array.");
                            continue;
                        }

                        GameObject gameObject2 = GameObject.Instantiate(gameObject, gameObject.transform);
                        gameObject2.name = "ExtraCreditsScreen(" + num.ToString() + ")";
                        TMP_Text[] componentsInChildren = gameObject2.GetComponentsInChildren<TMP_Text>();
                        if (componentsInChildren != null && componentsInChildren.Length != 0)
                        {
                            TMP_Text tmp_Text = componentsInChildren[0];
                            if (componentsInChildren.Length > 1)
                            {
                                GameObject.Destroy(componentsInChildren[1]);
                            }

                            string text = "";
                            string text2 = "";
                            foreach (string text3 in Credit.sponsers)
                            {
                                text2 = text2 + text3 + "\n";
                            }
                            foreach (JToken jtoken2 in lines)
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
                EnsureCreditsObjectsCached();
                if (cachedExtraCreditsScreen != null && cachedAllBackers != null)
                {
                    bool shouldShowBackers = !cachedExtraCreditsScreen.activeSelf || !cachedAllBackers.activeSelf;
                    cachedAllBackers.SetActive(shouldShowBackers);
                }
                else if (cachedExtraCreditsScreen == null && cachedAllBackers != null && cachedMainCredits != null)
                {
                    bool shouldShowBackers = !cachedMainCredits.activeSelf || !cachedAllBackers.activeSelf;
                    cachedAllBackers.SetActive(shouldShowBackers);
                }
            }
        }

        public static void ApplyLocalizationToCredits()
        {
            ApplyLocalizationToAllCreditsObjects();
        }

        private static void ResetSceneCaches()
        {
            cachedCreditsSceneHandle = -1;
            cachedCreditsScreens.Clear();
            cachedExtraCreditsScreen = null;
            cachedAllBackers = null;
            cachedMainCredits = null;
        }

        private static void EnsureCreditsObjectsCached()
        {
            int sceneHandle = SceneManager.GetActiveScene().handle;
            if (sceneHandle != cachedCreditsSceneHandle)
            {
                cachedCreditsSceneHandle = sceneHandle;
                cachedCreditsScreens.Clear();
                cachedExtraCreditsScreen = null;
                cachedAllBackers = null;
                cachedMainCredits = null;
            }

            if (cachedExtraCreditsScreen == null)
            {
                cachedExtraCreditsScreen = GameObject.Find("ExtraCreditsScreen(0)");
            }
            if (cachedAllBackers == null)
            {
                cachedAllBackers = GameObject.Find("All Backers");
            }
            if (cachedMainCredits == null)
            {
                cachedMainCredits = GameObject.Find("Main Credits");
            }
        }

        private static void ApplyLocalizationToAllCreditsObjects()
        {
            EnsureCreditsObjectsCached();
            if (cachedCreditsScreens.Count == 0)
            {
                cachedCreditsScreens.AddRange(Resources.FindObjectsOfTypeAll<Canvas>()
                    .Where(screen => screen != null && screen.name.StartsWith("Main Credits", StringComparison.Ordinal)));
            }

            foreach (Canvas screen in cachedCreditsScreens)
            {
                if (screen != null)
                {
                    ApplyLocalizationDirectly(screen.transform);
                }
            }
        }

        private static void ApplyLocalizationDirectly(Transform screen)
        {
            if (screen == null || !screen.name.StartsWith("Main Credits", StringComparison.Ordinal))
            {
                return;
            }

            foreach (KeyValuePair<string, string> entry in localizationKeys)
            {
                int separator = entry.Key.IndexOf('/');
                if (separator <= 0 ||
                    !string.Equals(screen.name, entry.Key.Substring(0, separator), StringComparison.Ordinal))
                {
                    continue;
                }

                string relativePath = entry.Key.Substring(separator + 1);
                Transform? target = screen.Find(relativePath);
                if (target != null)
                {
                    ApplyLocalizationToComponent(target.gameObject, entry.Value);
                }
            }
        }

        private static void ApplyLocalizationToComponent(GameObject textObject, string key)
        {
            TextMeshProUGUI? textComponent = textObject.GetComponent<TextMeshProUGUI>();
            textComponent?.ApplyLocalization(key, true);
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
                Destroy(gameObject);
            }
        }
    }
    public class Credit
    {
        private readonly string mod_path;
        private string credits_page = string.Empty;
        private const string CreditsDefault = "{\r\n    \"pages\": [\r\n        {\r\n            \"text\": [\r\n                \"<b>BB+汉化模组</b>\",\r\n                \"\\n\",\r\n                \"汉化模组/安装程序:\",\r\n                \"Baymaxawa\",\r\n                \"文本/贴图汉化:\",\r\n                \"MMZ\"\r\n            ]\r\n        },\r\n        {\r\n            \"text\": [\r\n                \"<b>BB+汉化模组</b>\",\r\n                \"\\n\",\r\n                \"润色: 馒\\n\",\r\n                \"TMP字体: cgq\\n\",\r\n                \"特别鸣谢: ChatGPT、Deepseek\"\r\n            ]\r\n        },\r\n        {\r\n            \"text\": [\r\n                \"<b>BB+汉化模组</b>\",\r\n                \"\\n\",\r\n                \"感谢所有在群内参与测试和提供的人员!\",\r\n                \"没有你们很难做到这里!\"\r\n            ]\r\n        },\r\n        {\r\n            \"text\": [\r\n                \"<b>BB+汉化模组 赞助人员名单</b>\",\r\n                \"\\n\",\r\n                \"{AFDIAN_SPONSERS}\"\r\n            ]\r\n        }\r\n    ]\r\n}";
        public static JObject credit_json = new JObject();
        public static string[] sponsers = new string[] { "爱发电用户_e57b1", "爱发电用户_40217", "Mrothen", "slxsh89" };

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
                if (!LoadCredits())
                {
                    API.Logger.Error("Error when trying to get mod_path!");
                }
            }
            catch (Exception ex)
            {
                API.Logger.Error(ex.Message);
            }
        }

        public void reload()
        {
            try
            {
                if (!LoadCredits())
                {
                    API.Logger.Error("Error when trying to get mod_path!");
                }
            }
            catch (Exception ex)
            {
                API.Logger.Error(ex.Message);
            }
        }

        private bool LoadCredits()
        {
            if (string.IsNullOrEmpty(mod_path))
            {
                return false;
            }

            string? parentDirectory = Path.GetDirectoryName(credits_page);
            if (!string.IsNullOrEmpty(parentDirectory) && !Directory.Exists(parentDirectory))
            {
                Directory.CreateDirectory(parentDirectory);
            }

            if (!File.Exists(credits_page))
            {
                File.WriteAllBytes(credits_page, Encoding.UTF8.GetBytes(CreditsDefault));
            }

            using (StreamReader streamReader = File.OpenText(credits_page))
            using (JsonTextReader jsonTextReader = new JsonTextReader(streamReader))
            {
                credit_json = (JObject)JToken.ReadFrom(jsonTextReader);
            }

            return true;
        }

    }
}
