using BBPC.API;
using BBPC.MTMAPIPatches;
using BBPC.Patches;
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
using TMPro;
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
    [BepInDependency("levs_kittne.baldiplus.null", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInProcess("BALDI.exe")]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance { get; private set; } = null!;
        public static Dictionary<string, AudioClip> AllClips { get; private set; } = new Dictionary<string, AudioClip>();
        private Harmony? harmonyInstance;
        private string[] expectedGameVersions = ["0.14", "0.14.1", "0.14.2", "0.14.3", "0.14.4"];

        private static readonly string[] menuTextureNames =
        {
            "About_Lit", "About_Unlit",
            "Options_Lit", "Options_Unlit",
            "Play_Lit", "Play_Unlit",
            "TempMenu_Low"
        };

        private readonly Dictionary<string, Dictionary<string, string>> translationsByLanguage =
            new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        private void Awake()
        {
            Instance = this;

            API.Logger.Init(Logger);
            ConfigManager.Initialize(this, Logger);

            API.Logger.Info($"插件 {BBPCTemp.ModName} 正在初始化...");
            API.Logger.Info($"纹理: {(ConfigManager.AreTexturesEnabled() ? "启用" : "禁用")}\n" +
                           $"日志记录: {(ConfigManager.IsLoggingEnabled() ? "启用" : "禁用")}\n" +
                           $"音频替换: {(ConfigManager.AreSoundsEnabled() ? "启用" : "禁用")}\n" +
                           $"字体替换: {(ConfigManager.IsFontReplacementEnabled() ? "启用" : "禁用")}\n"
#if DEBUG
                           + $"开发模式: {(ConfigManager.IsDevModeEnabled() ? "启用" : "禁用")}");
#else
                           );
#endif

            harmonyInstance = new Harmony(BBPCTemp.ModGUID);

            MainLoadTranspiler.Apply(harmonyInstance);

            new Credit(this);

            harmonyInstance.PatchAll();

            VersionCheck.CheckGameVersion(expectedGameVersions);

            RegisterFallbackFont(FontHelper.GetTextMeshProFont());

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

        private void OnMenu(OptionsMenu menu, CustomOptionsHandler handler)
        {
            BBPCOptionsCategory category = handler.AddCategory<BBPCOptionsCategory>(GetTranslationKey("BBPC_Options_Title", "BBPC"));
        }

        public string GetTranslationKey(string key, string default_obj, string lang="SChinese", bool custom_lang=false)
        {
            if (!custom_lang)
            {
                lang = ConfigManager.currect_lang.Value;
            }

            Dictionary<string, string> translations = GetTranslations(lang);
            return translations.TryGetValue(key, out string value) ? value : default_obj;
        }

        private Dictionary<string, string> GetTranslations(string language)
        {
            if (translationsByLanguage.TryGetValue(language, out Dictionary<string, string> cachedTranslations))
            {
                return cachedTranslations;
            }

            Dictionary<string, string> translations = new Dictionary<string, string>(StringComparer.Ordinal);
            string languagePath = Path.Combine(AssetLoader.GetModPath(this), "Language", language);

            if (Directory.Exists(languagePath))
            {
                try
                {
                    foreach (string jsonFilePath in Directory.EnumerateFiles(languagePath, "*.json", SearchOption.AllDirectories))
                    {
                        try
                        {
                            using (StreamReader file = File.OpenText(jsonFilePath))
                            using (JsonTextReader reader = new JsonTextReader(file))
                            {
                                JObject languageJson = JObject.Load(reader);
                                JArray? items = languageJson["items"] as JArray;
                                if (items == null)
                                {
                                    API.Logger.Error($"Language file does not contain an items array: {jsonFilePath}");
                                    continue;
                                }

                                foreach (JObject item in items.OfType<JObject>())
                                {
                                    string? itemKey = item.Value<string>("key");
                                    string? itemValue = item.Value<string>("value");
                                    if (itemKey != null && itemKey.Length > 0 && itemValue != null && !translations.ContainsKey(itemKey))
                                    {
                                        translations.Add(itemKey, itemValue);
                                    }
                                }
                            }
                        }
                        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is JsonException)
                        {
                            API.Logger.Error($"Failed to load language file '{jsonFilePath}': {ex.Message}");
                        }
                    }
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    API.Logger.Error($"Failed to scan language folder '{languagePath}': {ex.Message}");
                }
            }

            translationsByLanguage.Add(language, translations);
            API.Logger.Debug($"Cached {translations.Count} translations for language '{language}'.");
            return translations;
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

            if (!BBPCTemp.is_eng)
            {
                yield return "加载纹理中...";
                ApplyAllTextures();

                yield return "替换音频中";
                if (ConfigManager.AreSoundsEnabled())
                {
                    string audiosPath = Path.Combine(modPath, "Audios", ConfigManager.currect_lang.Value);
                    if (Directory.Exists(audiosPath))
                    {
                        API.Logger.Info($"Audio folder found: {audiosPath}, caching and replacing...");

                        string[] audioFiles = Directory.GetFiles(audiosPath, "*.wav").Concat(Directory.GetFiles(audiosPath, "*.ogg")).ToArray();
                        foreach (string audioFile in audioFiles)
                        {
                            string clipName = Path.GetFileNameWithoutExtension(audioFile);
                            if (!AllClips.ContainsKey(clipName))
                            {
                                AudioClip newClip = AssetLoader.AudioClipFromFile(audioFile);
                                if (newClip)
                                {
                                    newClip.name = clipName;
                                    AllClips.Add(clipName, newClip);
                                    API.Logger.Info($"Audio clip '{clipName}' cached.");
                                }
                            }
                        }

                        SoundObject[] allSounds = Resources.FindObjectsOfTypeAll<SoundObject>();
                        foreach (SoundObject soundObject in allSounds)
                        {
                            if (AllClips.TryGetValue(soundObject.name, out AudioClip newClip))
                            {
                                soundObject.soundClip = newClip;
                                API.Logger.Info($"Sound '{soundObject.name}' replaced.");
                            }
                        }
                    }
                }

                yield return "更新海报中...";
                UpdatePosters(modPath);
            }

#if DEBUG

            if (ConfigManager.IsDevModeEnabled())
            {
                yield return "提取海报信息中 (开发模式)...";
                PosterScanner.ScanAndExportNewPosters(modPath);
            }
#endif

            API.Logger.Info("资源加载完成！");
        }

        private void RegisterFallbackFont(TMP_FontAsset font)
        {
            if (!ConfigManager.IsFontReplacementEnabled()) return;
            if (font == null) return;

            var fallbackList = TMP_Settings.fallbackFontAssets;

            if (fallbackList == null)
            {
                API.Logger.Warning("TMP_Settings.fallbackFontAssets is null");
                return;
            }

            if (!fallbackList.Contains(font))
            {
                fallbackList.Add(font);
                API.Logger.Info($"添加成功！当前 fallback 列表数量: {fallbackList.Count}");

                // 打印所有 fallback 字体名称
                foreach (var f in fallbackList)
                {
                    API.Logger.Info($"  - {f?.name ?? "null"}");
                }
            }
            else
            {
                API.Logger.Info($"字体已存在于 fallback 列表中");
            }

            bool stillExists = fallbackList.Contains(font);
            API.Logger.Info($"字体是否仍在列表中: {stillExists}");
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

        private void OnDestroy()
        {
            CustomOptionsCore.OnMenuInitialize -= OnMenu;

            if (harmonyInstance != null)
            {
                harmonyInstance.UnpatchSelf();
                harmonyInstance = null;
            }

            translationsByLanguage.Clear();
            AllClips.Clear();
        }
    }
}
