using BBPC.API;
using BepInEx;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.OptionsAPI;
using MTM101BaldAPI.Registers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
    public class PosterTextData
    {
        public string textKey = string.Empty;
        public IntVector2 position;
        public IntVector2 size;
        public int fontSize;
        public Color color;
    }

    [Serializable]
    public class PosterTextTable
    {
        public List<PosterTextData> items = new List<PosterTextData>();
    }

    [BepInPlugin(BBPCTemp.ModGUID, BBPCTemp.ModName, BBPCTemp.ModVersion)]
    [BepInDependency("mtm101.rulerp.bbplus.baldidevapi", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("pixelguy.pixelmodding.baldiplus.bbextracontent", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("pixelguy.pixelmodding.baldiplus.newdecors", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("bbplus.challengejar", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("rost.moment.baldiplus.funsettings", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("wazkitta.plusmod.microeventsplus", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInProcess("BALDI.exe")]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance { get; private set; } = null!;
        private Harmony? harmonyInstance = null!;
        private string[] expectedGameVersions = ["0.13", "0.13.1"];

        private static readonly string[] menuTextureNames =
        {
            "About_Lit", "About_Unlit",
            "Options_Lit", "Options_Unlit",
            "Play_Lit", "Play_Unlit",
            "TempMenu_Low"
        };

        private void Awake()
        {
            Instance = this;
            API.Logger.Init(Logger);
            ConfigManager.Initialize(this, Logger);

            API.Logger.Info($"插件 {BBPCTemp.ModName} 正在初始化...");
            API.Logger.Info($"纹理: {(ConfigManager.AreTexturesEnabled() ? "启用" : "禁用")}, " +
                           $"日志记录: {(ConfigManager.IsLoggingEnabled() ? "启用" : "禁用")}" +
                           $"开发模式: {(ConfigManager.IsDevModeEnabled() ? "启用" : "禁用")}");

            new Harmony(BBPCTemp.ModGUID).PatchAllConditionals();

            VersionCheck.CheckGameVersion(expectedGameVersions, Info);

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
                        object? value = node.Value<object>();
                        if (value != null) result = value.ToString();
                    }
                }
                return result;
            }
            catch (Exception)
            {
                return "";
            }
        }

        private void OnMenu(OptionsMenu menu, CustomOptionsHandler handler)
        {
            BBPCOptionsCategory category = handler.AddCategory<BBPCOptionsCategory>(GetTranslationKey("BBPC_Options_Title", "BBPC"));
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
                    API.Logger.Debug(json_files.ToArray().ToString());
                    foreach (string json_file_path in json_files)
                    {
                        if (!json_file_path.Contains(lang)) continue;
                        API.Logger.Debug(json_file_path);
                        StreamReader file = File.OpenText(json_file_path);
                        JsonTextReader reader = new JsonTextReader(file);
                        JToken lang_json = (JObject)JToken.ReadFrom(reader);
#pragma warning disable CS8602 // 解引用可能出现空引用。
                        foreach (JToken item in lang_json["items"])
                        {
                            if (item["key"].ToString() == key)
                            {
                                return item["value"].ToString();
                            }
                        }
#pragma warning restore CS8602 // 解引用可能出现空引用。
                        file.Close();
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

        public static StandardMenuButton CreateButtonWithSprite(string name, Sprite sprite, Sprite? spriteOnHightlight = null, Transform? parent = null, Vector3? positon = null)
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

            if (ConfigManager.IsDevModeEnabled())
            {
                yield return "提取海报信息中 (开发模式)...";
                PosterScanner.ScanAndExportNewPosters(modPath);
            }

            API.Logger.Info("资源加载完成！");
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
                                sourceData.position = new IntVector2(modifiedData.position.x, modifiedData.position.z);
                                sourceData.size = new IntVector2(modifiedData.size.x, modifiedData.size.z);
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
