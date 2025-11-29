using BBPC.API;
using BBPC.Patches;
using BepInEx;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.OptionsAPI;
using MTM101BaldAPI.Registers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PlusLevelStudio.Menus;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BBPC
{
    [Serializable]
    public class PosterTextTable
    {
        public List<PosterTextData> items = new List<PosterTextData>();
    }

    [BepInPlugin(BBPCTemp.ModGUID, BBPCTemp.ModName, BBPCTemp.ModVersion)]
    [BepInDependency("mtm101.rulerp.bbplus.baldidevapi")]
    [BepInProcess("BALDI.exe")]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance { get; private set; } = null!;
        private Harmony? harmonyInstance = null!;
        private const string expectedGameVersion = "0.13";

        private static readonly string[] menuTextureNames =
        {
            "About_Lit", "About_Unlit",
            "Options_Lit", "Options_Unlit",
            "Play_Lit", "Play_Unlit",
            "TempMenu_Low"
        };
        private Watermark watermarkGO { get; set; } = null!;

        private void Awake()
        {
            Instance = this;
            API.Logger.Init(Logger);
            ConfigManager.Initialize(this, Logger);

            watermarkGO = new Watermark(ConfigManager.is_dev.Value, ConfigManager.is_alpha.Value, ConfigManager.is_beta.Value, this);

            API.Logger.Info($"插件 {BBPCTemp.ModName} 已初始化。");
            API.Logger.Info($"纹理: {(ConfigManager.AreTexturesEnabled() ? "启用" : "禁用")}, " +
                           $"日志记录: {(ConfigManager.IsLoggingEnabled() ? "启用" : "禁用")}");

            FileLog.Reset();

            Harmony harmony = new Harmony(BBPCTemp.ModGUID);

            harmony.PatchAllConditionals();

            ConfigManager.is_alpha.Value = false;
            ConfigManager.is_beta.Value = false;
            ConfigManager.is_dev.Value = false;
            if (ConfigManager.version.Value.Contains("Dev")) ConfigManager.is_dev.Value = true;
            else if (ConfigManager.version.Value.Contains("Beta")) ConfigManager.is_beta.Value = true;
            else if (ConfigManager.version.Value.Contains("Alpha")) ConfigManager.is_alpha.Value = true;

            VersionCheck.CheckGameVersion(expectedGameVersion, Info);

            string modPath = AssetLoader.GetModPath(this);
            string langPath = Path.Combine(modPath, "Language", ConfigManager.currect_lang.Value);
            if (ConfigManager.currect_lang.Value == "English") BBPCTemp.is_eng = true;
            if (Directory.Exists(langPath))
            {
                API.Logger.Info($"检测到本地化文件夹: {langPath}");
                AssetLoader.LoadLocalizationFolder(langPath, Language.English);
            }

            LoadingEvents.RegisterOnAssetsLoaded(Info, OnAssetsLoaded(), LoadingEventOrder.Post);

            gameObject.AddComponent<MenuTextureManager>();

            CustomOptionsCore.OnMenuInitialize += OnMenu;

            API.Logger.Info($"Mod {MyPluginInfo.PLUGIN_NAME} is loaded!");
        }

        private static string JSON_SeleteNode(JToken json, string ReName)
        {
            try
            {
                string result = "";
                //这里6.0版块可以用正则匹配
                var node = json.SelectToken("$.." + ReName);
                if (node != null)
                {
                    //判断节点类型
                    if (node.Type == JTokenType.String || node.Type == JTokenType.Integer || node.Type == JTokenType.Float)
                    {
                        //返回string值
                        result = node.Value<object>().ToString();
                    }
                }
                return result;
            }
            catch (Exception ex)
            {
                return "";
            }
        }

        private void OnMenu(OptionsMenu menu, CustomOptionsHandler handler)
        {
            BBPCOptionsCategory category = handler.AddCategory<BBPCOptionsCategory>(GetTranslationKey("BBPC_Options_Title", "BBPC"));
            TextLocalizer localizer = category.gameObject.AddComponent<TextLocalizer>();
            localizer.key = "BBPC_Options_Title";
            localizer.RefreshLocalization();
        }

        public string GetTranslationKey(string key, string default_obj, string lang="SChinese", bool custom_lang=false)
        {
            if (lang != ConfigManager.currect_lang.Value && !custom_lang) lang = ConfigManager.currect_lang.Value;
            string mod_path = AssetLoader.GetModPath(this);
            string langPath = Path.Combine(mod_path, "Language", lang);
            if (mod_path != null && mod_path != "")
            {
                if (Directory.Exists(langPath))
                {
                    string[] json_files = Directory.GetFiles(langPath, "*.json", SearchOption.AllDirectories);
                    API.Logger.Debug(json_files.ToString());
                    foreach (string json_file_path in json_files)
                    {
                        if (!json_file_path.Contains(lang)) continue;
                        API.Logger.Debug(json_file_path);
                        StreamReader file = File.OpenText(json_file_path);
                        JsonTextReader reader = new JsonTextReader(file);
                        JToken lang_json = (JObject)JToken.ReadFrom(reader);
                        foreach (JToken item in lang_json["items"])
                        {
                            if (item["key"].ToString() == key)
                            {
                                return item["value"].ToString();
                            }
                        }
                        file.Close();
                        break;
                    }
                }
            }
            return default_obj;
        }

        public static T LoadAsset<T>(string name) where T : UnityEngine.Object
        {
            return (from x in Resources.FindObjectsOfTypeAll<T>()
                    where x.name.ToLower() == name.ToLower()
                    select x).First();
        }

        public static StandardMenuButton CreateButtonWithSprite(string name, Sprite sprite, Sprite spriteOnHightlight = null, Transform parent = null, Vector3? positon = null)
        {
            GameObject gameObject = new GameObject(name);
            gameObject.layer = 5;
            gameObject.tag = "Button";
            StandardMenuButton res = gameObject.AddComponent<StandardMenuButton>();
            res.image = gameObject.AddComponent<Image>();
            res.image.sprite = sprite;
            res.unhighlightedSprite = sprite;
            res.OnPress = new UnityEvent();
            res.OnRelease = new UnityEvent();
            if (spriteOnHightlight != null)
            {
                res.OnHighlight = new UnityEvent();
                res.swapOnHigh = true;
                res.highlightedSprite = spriteOnHightlight;
            }
            res.transform.SetParent(parent);
            res.transform.localPosition = positon ?? new Vector3(0, 0, 0);
            return res;
        }

        private IEnumerator OnAssetsLoaded()
        {
            yield return 3;

            yield return "正在加载资源...";
            API.Logger.Info("正在加载本地化资源...");

            string modPath = AssetLoader.GetModPath(this);

            if (ConfigManager.currect_lang.Value != "English")
            {
                yield return "加载纹理中...";
                ApplyAllTextures();

                yield return "更新海报中...";
                UpdatePosters(modPath);
            }

            API.Logger.Info("资源加载完成！");
        }

        public static void update_watermark()
        {
            API.Logger.Debug("Try to update watermark");
            Plugin.Instance.watermarkGO.update_watermark(ConfigManager.is_dev.Value, ConfigManager.is_alpha.Value, ConfigManager.is_beta.Value);
        }

        public void ApplyMenuTextures()
        {
            if (!ConfigManager.AreTexturesEnabled()) return;

            string modPath = AssetLoader.GetModPath(this);
            string texturesPath = Path.Combine(modPath, "Textures");

            if (Directory.Exists(texturesPath))
            {
                API.Logger.Info("正在应用主菜单纹理...");
                Texture2D[] allGameTextures = Resources.FindObjectsOfTypeAll<Texture2D>();
                foreach (string textureName in menuTextureNames)
                {
                    Texture2D originalTexture = allGameTextures.FirstOrDefault(t => t.name == textureName);
                    if (originalTexture != null)
                    {
                        string textureFile = Path.Combine(texturesPath, textureName + ".png");
                        if (File.Exists(textureFile))
                        {
                            try
                            {
                                Texture2D newTexture = AssetLoader.TextureFromFile(textureFile);
                                if (newTexture != null)
                                {
                                    if (originalTexture.width != newTexture.width || originalTexture.height != newTexture.height)
                                    {
                                        API.Logger.Warning($"纹理 '{textureName}' 尺寸 ({newTexture.width}x{newTexture.height}) 与原始尺寸 ({originalTexture.width}x{originalTexture.height}) 不匹配。已跳过替换。");
                                        continue;
                                    }

                                    newTexture = AssetLoader.AttemptConvertTo(newTexture, originalTexture.format);
                                    AssetLoader.ReplaceTexture(originalTexture, newTexture);
                                }
                            }
                            catch (Exception e)
                            {
                                API.Logger.Error($"替换纹理 '{textureName}' 时出错: {e.Message}");
                            }
                        }
                    }
                }
            }
        }

        public void ApplyAllTextures()
        {
            if (!ConfigManager.AreTexturesEnabled()) return;

            string modPath = AssetLoader.GetModPath(this);
            string texturesPath = Path.Combine(modPath, "Textures", ConfigManager.currect_lang.Value);

            if (Directory.Exists(texturesPath))
            {
                API.Logger.Info($"检测到纹理文件夹: {texturesPath}, 正在替换...");

                Texture2D[] allGameTextures = Resources.FindObjectsOfTypeAll<Texture2D>();
                string[] textureFiles = Directory.GetFiles(texturesPath, "*.png", SearchOption.AllDirectories);

                foreach (string textureFile in textureFiles)
                {
                    string textureName = Path.GetFileNameWithoutExtension(textureFile);
                    Texture2D originalTexture = allGameTextures.FirstOrDefault(t => t.name == textureName);

                    if (originalTexture != null)
                    {
                        try
                        {
                            Texture2D newTexture = AssetLoader.TextureFromFile(textureFile);
                            if (newTexture != null)
                            {
                                if (originalTexture.width != newTexture.width || originalTexture.height != newTexture.height)
                                {
                                    API.Logger.Warning($"纹理 '{textureName}' 尺寸 ({newTexture.width}x{newTexture.height}) 与原始尺寸 ({originalTexture.width}x{originalTexture.height}) 不匹配。已跳过替换。");
                                    continue;
                                }

                                newTexture = AssetLoader.AttemptConvertTo(newTexture, originalTexture.format);
                                AssetLoader.ReplaceTexture(originalTexture, newTexture);
                                API.Logger.Info($"纹理 '{textureName}' 已替换。");
                            }
                        }
                        catch (Exception e)
                        {
                            API.Logger.Error($"替换纹理 '{textureName}' 时出错: {e.Message}");
                        }
                    }
                    else
                    {
                        API.Logger.Warning($"未找到对应的纹理文件: {textureName}");
                    }
                }
            }
        }

        private void UpdatePosters(string modPath)
        {
            string postersPath = Path.Combine(modPath, "PosterFiles", ConfigManager.currect_lang.Value);
            if (!Directory.Exists(postersPath))
            {
                API.Logger.Warning("未找到海报文件夹，跳过替换。");
                return;
            }

            API.Logger.Info("开始更新海报内容...");
            PosterObject[] allPosters = Resources.FindObjectsOfTypeAll<PosterObject>();
            foreach (PosterObject poster in allPosters)
            {
                string posterDataPath = Path.Combine(postersPath, poster.name, "PosterData.json");
                if (File.Exists(posterDataPath))
                {
                    try
                    {
                        PosterTextTable? posterData = JsonUtility.FromJson<PosterTextTable>(File.ReadAllText(posterDataPath));

                        if (posterData != null)
                        {
                            for (int i = 0; i < Math.Min(posterData.items.Count, poster.textData.Length); i++)
                            {
                                var sourceData = poster.textData[i];
                                var modifiedData = posterData.items[i];

                                sourceData.textKey = modifiedData.textKey;
                                sourceData.position = modifiedData.position;
                                sourceData.size = modifiedData.size;
                                sourceData.fontSize = modifiedData.fontSize;
                                sourceData.color = modifiedData.color;
                            }

                            API.Logger.Info($"海报内容已更新: {poster.name}");
                        }
                    }
                    catch (Exception ex)
                    {
                        API.Logger.Error($"更新海报 {poster.name} 时出错: {ex.Message}");
                    }
                }
            }
            API.Logger.Info("海报更新完成。");
        }

        void OnDestroy()
        {
            if (harmonyInstance != null)
            {
                harmonyInstance.UnpatchSelf();
                harmonyInstance = null;
            }
        }

        public void des(GameObject obj)
        {
            Destroy(obj);
        }
    }
}
