using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace BBPC.API
{
    public class FontHelper
    {
        private static TMP_FontAsset? _cachedFont;
        private static readonly object _lock = new object();
        private static readonly string[] TextMeshProShaderNames =
        {
            "TextMeshPro/Bitmap",
            "TextMeshPro/Mobile/Bitmap",
            "TextMeshPro/Distance Field",
            "TextMeshPro/Mobile/Distance Field",
            "TextMeshPro/Distance Field Overlay",
            "TextMeshPro/Mobile/Distance Field - Masking"
        };

        /// <summary>
        /// 获取 TMP 字体（带缓存）
        /// </summary>
        public static TMP_FontAsset? GetTextMeshProFont()
        {
            // 检查缓存
            if (_cachedFont != null)
            {
                return _cachedFont;
            }

            lock (_lock)
            {
                if (_cachedFont != null) return _cachedFont;

                // 检查配置
                if (string.IsNullOrEmpty(ConfigManager.overrideFontPath.Value))
                {
                    Logger.Warning("OverrideFontPath 为空，停止加载字体！");
                    return null;
                }

                string fontFileName = ConfigManager.overrideFontPath.Value;
                TMP_FontAsset? font = null;

                // ===== 策略1：从 AssetBundle 加载 =====
                var overrideFontPath = Path.Combine(AssetLoader.GetModPath(Plugin.Instance), fontFileName);

                if (File.Exists(overrideFontPath))
                {
                    Logger.Info($"尝试从 AssetBundle 加载字体: {overrideFontPath}");
                    font = LoadFromAssetBundle(overrideFontPath);
                }
                else
                {
                    Logger.Error($"字体文件不存在: {overrideFontPath}");
                }

                // ===== 策略2：从 Resources 加载 =====
                if (font == null)
                {
                    Logger.Info($"尝试从 Resources 加载字体: {fontFileName}");
                    font = Resources.Load<TMP_FontAsset>(fontFileName);

                    // 尝试不带扩展名
                    if (font == null && fontFileName.EndsWith(".asset"))
                    {
                        string nameWithoutExt = Path.GetFileNameWithoutExtension(fontFileName);
                        Logger.Info($"尝试从 Resources 加载字体（不带扩展名）: {nameWithoutExt}");
                        font = Resources.Load<TMP_FontAsset>(nameWithoutExt);
                    }
                }

                // ===== 策略3：从系统字体创建 =====
                if (font == null)
                {
                    Logger.Info($"尝试从系统字体创建: {fontFileName}");
                    font = CreateFromSystemFont(fontFileName);
                }

                // ===== 处理加载结果 =====
                if (font != null)
                {
                    Logger.Info($"字体加载成功: {font.name}");

                    // 防止字体在场景切换时被销毁
                    GameObject.DontDestroyOnLoad(font);
                    Logger.Info($"字体 '{font.name}' 已持久化");

                    _cachedFont = font;
                }
                else
                {
                    Logger.Error($"所有加载方式都失败，无法加载字体: {fontFileName}");
                }

                return font;
            }
        }

        /// <summary>
        /// 从 AssetBundle 加载 TMP 字体
        /// </summary>
        private static TMP_FontAsset? LoadFromAssetBundle(string bundlePath)
        {
            try
            {
                // AssetBundles contain platform-specific shader bytecode. Capture the
                // shaders supplied by the running game before a Windows-built bundle
                // introduces an unsupported shader with the same name on Linux.
                Shader[] compatibleShaders = CaptureCompatibleTextMeshProShaders();
                AssetBundle bundle = AssetBundle.LoadFromFile(bundlePath);
                if (bundle == null)
                {
                    Logger.Warning($"无法加载 AssetBundle: {bundlePath}");
                    return null;
                }

                // 尝试加载所有 TMP_FontAsset
                TMP_FontAsset[] allFonts = bundle.LoadAllAssets<TMP_FontAsset>();
                TMP_FontAsset? font = allFonts.FirstOrDefault();

                if (font == null)
                {
                    // 尝试按常见名称加载
                    string fileName = Path.GetFileNameWithoutExtension(bundlePath);
                    string[] possibleNames = {
                        fileName,
                        "t2",
                        "t2.asset",
                        "assets/t2.asset",
                        "default",
                        "SDF"
                    };

                    foreach (var name in possibleNames)
                    {
                        font = bundle.LoadAsset<TMP_FontAsset>(name);
                        if (font != null) break;
                    }
                }

                if (font == null)
                {
                    // 调试：列出所有资源
                    Logger.Info($"AssetBundle 中的资源:");
                    foreach (var name in bundle.GetAllAssetNames())
                    {
                        Logger.Info($"  - {name}");
                    }
                }

                if (font != null)
                {
                    LogFontAssetDetails(font);
                }

                if (font != null && !EnsureRenderableMaterials(font, compatibleShaders, new HashSet<int>()))
                {
                    Logger.Error($"字体 '{font.name}' 的材质在当前平台不可渲染");
                    font = null;
                }

                bundle.Unload(false);
                return font;
            }
            catch (Exception ex)
            {
                Logger.Error($"从 AssetBundle 加载字体失败: {ex.Message}");
                return null;
            }
        }

        private static bool EnsureRenderableMaterials(TMP_FontAsset font, Shader[] compatibleShaders, HashSet<int> visitedFonts)
        {
            if (!visitedFonts.Add(font.GetInstanceID()))
            {
                return true;
            }

            bool renderable = EnsureRenderableMaterial(font, compatibleShaders);
            if (font.fallbackFontAssetTable == null)
            {
                return renderable;
            }

            foreach (TMP_FontAsset fallbackFont in font.fallbackFontAssetTable)
            {
                if (fallbackFont == null)
                {
                    continue;
                }

                LogFontAssetDetails(fallbackFont);
                renderable &= EnsureRenderableMaterials(fallbackFont, compatibleShaders, visitedFonts);
            }

            return renderable;
        }

        private static void LogFontAssetDetails(TMP_FontAsset font)
        {
            string sourceFont = font.sourceFontFile != null ? font.sourceFontFile.name : "none";
            int characterCount = font.characterTable != null ? font.characterTable.Count : 0;
            int fallbackCount = font.fallbackFontAssetTable != null ? font.fallbackFontAssetTable.Count : 0;
            Logger.Info($"字体详情: source={sourceFont}, mode={font.atlasPopulationMode}, render={font.atlasRenderMode}, characters={characterCount}, fallbacks={fallbackCount}, atlases={font.atlasTextureCount}");
        }

        /// <summary>
        /// Captures platform-compatible TMP shaders before loading an AssetBundle.
        /// Calling Shader.Find here is important: after the bundle is loaded Unity can
        /// resolve the name to the bundle's incompatible platform-specific shader.
        /// </summary>
        private static Shader[] CaptureCompatibleTextMeshProShaders()
        {
            var shaders = new List<Shader>();

            foreach (Shader shader in Resources.FindObjectsOfTypeAll<Shader>())
            {
                if (shader != null && shader.isSupported && shader.name.StartsWith("TextMeshPro/", StringComparison.Ordinal))
                {
                    shaders.Add(shader);
                }
            }

            foreach (string shaderName in TextMeshProShaderNames)
            {
                Shader shader = Shader.Find(shaderName);
                if (shader != null && shader.isSupported)
                {
                    shaders.Add(shader);
                }
            }

            return shaders.Distinct().ToArray();
        }

        /// <summary>
        /// Replaces platform-incompatible bundle shader bytecode while preserving the
        /// font's atlas and material settings. This is required for bundles built on
        /// Windows to actually draw on Linux, even though the TMP asset loads normally.
        /// </summary>
        private static bool EnsureRenderableMaterial(TMP_FontAsset font, Shader[] compatibleShaders)
        {
            Material material = font.material;
            if (material == null)
            {
                Logger.Error($"字体 '{font.name}' 没有材质");
                return false;
            }

            if (material.HasProperty("_CullMode"))
            {
                material.SetFloat("_CullMode", 0f);
                Logger.Info($"修复材质 '{material.name}' 的 _CullMode");
            }

            Shader originalShader = material.shader;
            string shaderName = originalShader != null ? originalShader.name : string.Empty;
            if (originalShader != null && originalShader.isSupported)
            {
                Logger.Info($"字体材质可渲染: {material.name} / {shaderName}");
                return material.mainTexture != null;
            }

            Shader? replacement = compatibleShaders.FirstOrDefault(shader =>
                shader.name.Equals(shaderName, StringComparison.Ordinal));

            if (replacement == null)
            {
                Logger.Error($"找不到平台兼容的 TMP shader 来替换 '{shaderName}'");
                return false;
            }

            Texture atlasTexture = material.mainTexture;
            material.shader = replacement;
            if (atlasTexture != null)
            {
                material.mainTexture = atlasTexture;
            }

            if (material.HasProperty("_CullMode"))
            {
                material.SetFloat("_CullMode", 0f);
                Logger.Info($"替换shader后再次修复 '{material.name}' 的 _CullMode");
            }

            Texture? renderedAtlas = material.mainTexture;
            bool renderable = material.shader != null && material.shader.isSupported && renderedAtlas != null;
            if (renderable)
            {
                Logger.Info($"已将字体 '{font.name}' 的不兼容 shader '{shaderName}' 替换为当前平台版本；atlas={renderedAtlas!.width}x{renderedAtlas.height}");
            }
            else
            {
                Logger.Error($"字体 '{font.name}' 的 TMP 材质修复后仍不可渲染");
            }

            return renderable;
        }

        /// <summary>
        /// 从系统字体创建 TMP FontAsset
        /// </summary>
        private static TMP_FontAsset? CreateFromSystemFont(string fontName)
        {
            try
            {
                // 检查是否是系统字体
                string[] systemFonts = GetOSInstalledFontNames();
                if (!systemFonts.Any(f => f.Equals(fontName, StringComparison.OrdinalIgnoreCase)))
                {
                    // 尝试常见的备用字体
                    string[] fallbackFonts = { "Microsoft YaHei", "SimHei", "NotoSansSC", "Arial" };
                    foreach (var fallback in fallbackFonts)
                    {
                        if (systemFonts.Any(f => f.Equals(fallback, StringComparison.OrdinalIgnoreCase)))
                        {
                            fontName = fallback;
                            Logger.Info($"使用备用系统字体: {fontName}");
                            break;
                        }
                    }

                    // 如果还是没有，直接返回
                    if (!systemFonts.Any(f => f.Equals(fontName, StringComparison.OrdinalIgnoreCase)))
                    {
                        Logger.Warning($"系统字体 '{fontName}' 未安装");
                        return null;
                    }
                }

                // 方法1：通过 Unity Font 对象创建
                Font? systemFont = Resources.FindObjectsOfTypeAll<Font>()
                    .FirstOrDefault(f => f.name.Equals(fontName, StringComparison.OrdinalIgnoreCase));

                if (systemFont != null)
                {
                    Logger.Info($"从系统字体 '{fontName}' 创建 TMP FontAsset...");
                    return TMP_FontAsset.CreateFontAsset(
                        systemFont,
                        90,
                        9,
                        GlyphRenderMode.SDFAA,
                        1024,
                        1024,
                        AtlasPopulationMode.Dynamic
                    );
                }

                // 方法2：尝试通过反射调用 CreateFontAsset(string, string, int)
                try
                {
                    var method = typeof(TMP_FontAsset).GetMethod(
                        "CreateFontAsset",
                        new Type[] { typeof(string), typeof(string), typeof(int) }
                    );

                    if (method != null)
                    {
                        Logger.Info($"通过名称创建 TMP FontAsset: {fontName}");
                        var result = method.Invoke(null, new object[] { fontName, "", 90 });
                        return result as TMP_FontAsset;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warning($"通过名称创建失败: {ex.Message}");
                }

                return null;
            }
            catch (Exception ex)
            {
                Logger.Error($"创建系统字体失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 获取操作系统已安装的字体列表
        /// </summary>
        private static string[] GetOSInstalledFontNames()
        {
            try
            {
                using var fonts = new InstalledFontCollection();
                return fonts.Families
                    .Select(f => f.Name)
                    .Distinct()
                    .ToArray();
            }
            catch (Exception ex)
            {
                Logger.Error($"获取系统字体列表失败: {ex.Message}");
                return new string[0];
            }
        }

        /// <summary>
        /// 清除字体缓存（用于重新加载）
        /// </summary>
        public static void ClearCache()
        {
            lock (_lock)
            {
                _cachedFont = null;
                Logger.Info("字体缓存已清除");
            }
        }

        /// <summary>
        /// 检查字体是否已加载
        /// </summary>
        public static bool IsFontLoaded()
        {
            return _cachedFont != null;
        }

        /// <summary>
        /// 获取已加载的字体（如果有）
        /// </summary>
        public static TMP_FontAsset? GetLoadedFont()
        {
            return _cachedFont;
        }
    }
}
