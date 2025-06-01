using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/*
 * My codes are so bad that lots of them are using AI to make!
 * So if you see my codes please not laugh at me.
 * Because i'm still bad at coding with c#!
 */
namespace BBPC
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, "0.0.0.3")]
    public class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger;
        private Harmony harmony;
        private ConfigFile config;
        private TextMeshProUGUI versionLabel;
        private Watermark watermarkGO;
        private GameObject watermark;
        private ConfigEntry<bool> is_dev;
        private ConfigEntry<bool> is_beta;
        private ConfigEntry<bool> is_alpha;
        private ConfigEntry<string> version;

        private void Awake()
        {
            config = new ConfigFile(
                Path.Combine(Paths.ConfigPath, MyPluginInfo.PLUGIN_GUID+".cfg"),
                true
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
            harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
            harmony.PatchAll();
            watermarkGO = new Watermark(is_dev.Value, is_alpha.Value, is_beta.Value, this);
            Logger.LogInfo($"Mod {MyPluginInfo.PLUGIN_NAME} is loaded!");
            //SceneManager.LoadScene("Credits");
        }

        private Canvas uiCanvas;

        void FindUICanvas()
        {
            uiCanvas = GameObject.FindObjectOfType<Canvas>();
            if (uiCanvas == null)
            {
                Logger.LogError("Main canvas not found!");
                return;
            }
        }

        private bool IsValidMainMenuScene()
        {
            return SceneManager.GetActiveScene().name == "MainMenu"
                   && uiCanvas != null
                   && uiCanvas.gameObject.activeInHierarchy
                   && uiCanvas.enabled;
        }

        private void Update()
        {
            if (uiCanvas == null) FindUICanvas();
            try
            {
                if (IsValidMainMenuScene())
                {
                    HandleMainMenuUI();
                }
                HandleCreditsScreen();
            }
            catch (System.Exception e)
            {
                Logger.LogError($"Update loop error: {e}");
            }
        }

        private void HandleMainMenuUI()
        {
            if (SceneManager.GetActiveScene().name != "MainMenu") return;
            if (uiCanvas == null)
            {
                FindUICanvas();
                if (uiCanvas == null) return;
            }
            try
            {
                UpdateVersionLabel();
                CreateWatermark();
            }
            catch (Exception e)
            {
                Logger.LogError($"UI creation failed: {e}");
            }
        }

        private void UpdateVersionLabel()
        {
            if (versionLabel == null)
            {
                GameObject versionLabelGO = GameObject.Find("Reminder");
                versionLabel = versionLabelGO?.GetComponent<TextMeshProUGUI>();
                versionLabel.text += "\n汉化 " + version.Value;
            }

            if (versionLabel != null)
            {
                UpdateBuildFlags();
                UpdateWatermark();
            }
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
                watermarkGO.update_watermark();
            }
        }

        private void HandleCreditsScreen()
        {
            if (SceneManager.GetActiveScene().name != "Credits") return;

            GameObject extra = GameObject.Find("ExtraCreditsScreen");
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

        //Use these when debugging, it disables return to main menu function in credits screen.
        /*[HarmonyPatch(typeof(Credits), "Update")]
        public class Credits_Update_DisableReturnPatch
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = new List<CodeInstruction>(instructions);
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
                return codes.AsEnumerable();
            }
        }*/

        [HarmonyPatch(typeof(Credits), "Start")]
        public class Credits_Start_Patch
        {
            static void Postfix(Credits __instance)
            {
                if (__instance.screens != null && __instance.screens.Count > 0)
                {
                    GameObject originalScreen = __instance.screens[0].gameObject;
                    GameObject clonedScreen = Instantiate(originalScreen, originalScreen.transform.parent);
                    clonedScreen.name = "ExtraCreditsScreen";
                    clonedScreen.SetActive(false);
                    TMP_Text[] texts = clonedScreen.GetComponentsInChildren<TMP_Text>();
                    if (texts != null && texts.Length > 0)
                    {
                        TMP_Text mainText = texts[0];
                        for (int i = 1; i < texts.Length; i++)
                        {
                            Destroy(texts[i].gameObject);
                        }
                        mainText.text = "<b>BB+汉化模组</b>\n\n主要汉化人员:\nBaymaxawa & MEMZSystem32\n\n润色: 馒\n\nTMP字体: CMCGZP\n\n特别鸣谢: ChatGPT\n\n<size=12>感谢所有在群内参与测试和提供的人员! 没有你们很难做到这里!</size>";
                        mainText.fontSize = 24;
                        mainText.alignment = TextAlignmentOptions.Center;
                        mainText.color = Color.white;
                    }
                    __instance.screens.Insert(1, clonedScreen.GetComponent<Canvas>());
                }
            }
        }
    }
}
