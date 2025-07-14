using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/*
 * My codes are so bad that lots of them are using AI to make!
 * So if you see my codes please not laugh at me.
 * Because i'm still bad at coding with c#!
 * And you you would like to help you can open Pull Requests
 */
namespace BBPC
{
    [BepInDependency("mtm101.rulerp.bbplus.baldidevapi", BepInDependency.DependencyFlags.HardDependency)]

    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, "0.0.0.4")]
    public class Plugin : BaseUnityPlugin
    {
        static string _modPath = string.Empty;
        public static string ModPath => _modPath;
        internal static new ManualLogSource Logger;
        private Harmony harmony;
        private ConfigFile config;
        private TextMeshProUGUI versionLabel;
        private Watermark watermarkGO;
        private GameObject watermark;
        public static ConfigEntry<bool> disable_credits;
        private ConfigEntry<bool> is_dev;
        private ConfigEntry<bool> is_beta;
        private ConfigEntry<bool> is_alpha;
        private ConfigEntry<string> version;
        private Credit credit_handler;

        private void Awake()
        {
            _modPath = AssetLoader.GetModPath(this);
            config = new ConfigFile(
                Path.Combine(Paths.ConfigPath, MyPluginInfo.PLUGIN_GUID+".cfg"),
                true
            );
            disable_credits = config.Bind(
                "Debugging",
                "disable_credits",
                false,
                "Disable Credits 'Press any key will go back' Function"
            );
            is_dev = config.Bind(
                "Build Check",
                "is_dev",
                false,
                "Check if is dev build."
            );
            is_beta = config.Bind(
                "Build Check",
                "is_beta",
                false,
                "Check if is beta build."
            );
            is_alpha = config.Bind(
                "Build Check",
                "is_alpha",
                false,
                "Check if is alpha build."
            );
            version = config.Bind(
                "Version Number",
                "version",
                "Dev Build",
                "Version number that displays in game."
            );
            config.Save();
            Logger = base.Logger;
            credit_handler = new Credit(this);
            harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
            harmony.PatchAll();
            watermarkGO = new Watermark(is_dev.Value, is_alpha.Value, is_beta.Value, this);
            SceneManager.sceneLoaded += OnSceneLoaded;
            Logger.LogInfo($"Mod {MyPluginInfo.PLUGIN_NAME} is loaded!");
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "MainMenu") HandleMainMenuUI(scene);
        }

        private List<GameObject> GetAllGameObjects(Scene scene)
        {
            List<GameObject> allObjects = new List<GameObject>();
            Stack<GameObject> stack = new Stack<GameObject>();

            // 添加所有根对象到栈中
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                stack.Push(root);
            }

            while (stack.Count > 0)
            {
                GameObject current = stack.Pop();
                allObjects.Add(current);

                // 将所有子对象添加到栈中
                for (int i = 0; i < current.transform.childCount; i++)
                {
                    stack.Push(current.transform.GetChild(i).gameObject);
                }
            }

            return allObjects;
        }

        private void HandleMainMenuUI(Scene scene)
        {
            try
            {
                GameObject versionLabelGO = null;
                List<GameObject> allObjects = GetAllGameObjects(scene);
                Logger.LogWarning("Objects: "+allObjects.ToString());
                List<GameObject> empty = [];
                if (allObjects == null || allObjects == empty) { Logger.LogError("allObjects is empty!"); return; }
                foreach (GameObject obj in allObjects)
                {
                    Logger.LogWarning("Object: " + obj.name);
                    if (obj.name.Contains("Reminder"))
                    {
                        versionLabelGO = obj;
                        Logger.LogInfo("Assigned versionLabelGO as " + obj.name);
                        break;
                    }
                }
                if (versionLabelGO == null) { Logger.LogError("error when finding reminder!"); return; }
                UpdateVersionLabel(versionLabelGO);
            }
            catch (NullReferenceException e)
            {
                Logger.LogError(e.StackTrace);
            }
            catch (Exception e)
            {
                Logger.LogError($"UI creation failed: {e}");
            }
        }

        private void UpdateVersionLabel(GameObject version_Label)
        {
            Logger.LogWarning(version_Label);
            if (versionLabel == null)
            {
                versionLabel = version_Label.GetComponent<TextMeshProUGUI>();
            }
            Logger.LogWarning(versionLabel);
            versionLabel.text += "\n汉化 " + version.Value;
            Logger.LogWarning(versionLabel.text);
            UpdateBuildFlags();
            UpdateWatermark();
            Logger.LogWarning(versionLabel.text);
        }

        private void UpdateBuildFlags()
        {
            is_dev.Value = version.Value.Contains("Dev Build");
            is_beta.Value = version.Value.Contains("Beta");
            is_alpha.Value = version.Value.Contains("Alpha");
            config.Save();
        }

        private void CreateWatermark()
        {
            if (watermark == null && watermarkGO != null)
            {
                watermark = watermarkGO.create_watermark(
                    is_dev.Value,
                    is_alpha.Value,
                    is_beta.Value
                );
            }
        }

        private void UpdateWatermark()
        {
            if (watermark != null && watermarkGO != null)
            {
                watermarkGO.update_watermark(is_dev.Value, is_alpha.Value, is_beta.Value);
            }
        }

        private void Update()
        {
            if (SceneManager.GetActiveScene().name == "MainMenu")
            {
                try
                {
                    GameObject menu = GameObject.Find("Menu");
                    if (menu.activeSelf)
                    {
                        GameObject reminder = GameObject.Find("Reminder");
                        if (!reminder.GetComponent<TextMeshProUGUI>().text.Contains("汉化")) { HandleMainMenuUI(SceneManager.GetActiveScene()); }
                    }
                }
                catch (NullReferenceException)
                {

                }
            }
            if (SceneManager.GetActiveScene().name != "Credits") return;

            GameObject extra = GameObject.Find("ExtraCreditsScreen(0)");
            GameObject allBackers = GameObject.Find("All Backers");

            if (extra != null && allBackers != null)
            {
                // Simplify the nested if statements
                bool shouldDisable = extra.activeSelf && allBackers.activeSelf;
                allBackers.SetActive(!shouldDisable);
            }
        }

        public void des(GameObject obj)
        {
            Destroy(obj);
        }

        public void LogWarn (object msg)
        {
            Logger.LogWarning(msg);
        }

        [HarmonyPatch(typeof(Credits), "Start")]
        public class Credits_Start_Patch
        {
            static void Postfix(Credits __instance)
            {
                if (__instance.screens != null && __instance.screens.Count > 0)
                {
                    JObject json = Credit.credit_json;
                    int i = 0;
                    int c = 1;
                    GameObject originalScreen = __instance.screens[0].gameObject;
                    foreach (JObject page in json["pages"])
                    {
                        GameObject clonedScreen = Instantiate(originalScreen, originalScreen.transform.parent);
                        clonedScreen.name = "ExtraCreditsScreen(" + i.ToString() + ")";
                        clonedScreen.SetActive(false);
                        TMP_Text[] texts = clonedScreen.GetComponentsInChildren<TMP_Text>();
                        if (texts != null && texts.Length > 0)
                        {
                            TMP_Text mainText = texts[0];
                            Destroy(texts[1]);
                            string text_screen = "";
                            string sponser = "";
                            foreach (string text in Credit.sponsers)
                            {
                                sponser += text + "\n";
                            }
                            foreach (JToken text in page["text"])
                            {
                                if (text.ToString().Contains("{AFDIAN_SPONSERS}"))
                                {
                                    string text2 = sponser;
                                    text_screen += text2 + "\n";
                                }
                                else
                                {
                                    text_screen += text.ToString() + "\n";
                                }
                            }
                            mainText.text = text_screen;
                            mainText.fontSize = 24;
                            mainText.alignment = TextAlignmentOptions.Center;
                            mainText.color = Color.white;
                        } 
                        __instance.screens.Insert(c, clonedScreen.GetComponent<Canvas>());
                        i++;
                        c++;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Credits), "Update")]
        public class Credits_Update_DisableReturnPatch
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = new List<CodeInstruction>(instructions);
                if (Plugin.disable_credits.Value)
                {
                    for (int i = 0; i < codes.Count; i++)
                    {
                        if (codes[i].opcode == OpCodes.Ldstr && (string)codes[i].operand == "MainMenu")
                        {
                            Logger.LogDebug("Found MainMenu load instruction; replacing with NOPs.");
                            codes[i].opcode = OpCodes.Nop;
                            if (i + 1 < codes.Count) codes[i + 1].opcode = OpCodes.Nop;
                            if (i + 2 < codes.Count) codes[i + 2].opcode = OpCodes.Nop;
                        }
                    }
                }
                return codes.AsEnumerable();
            }
        }
    }
}
