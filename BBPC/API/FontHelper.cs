using BepInEx;
using MTM101BaldAPI.AssetTools;
using System;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace BBPC.API
{
    public class FontHelper
    {
        private static TMP_FontAsset _cachedFont = null;
        private static readonly object _lock = new object();

        /// <summary>
        /// 获取 TMP 字体（带缓存）
        /// </summary>
        public static TMP_FontAsset GetTextMeshProFont()
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
                TMP_FontAsset font = null;

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
                    Logger.Info($"✅ 字体加载成功: {font.name}");

                    // 防止字体在场景切换时被销毁
                    GameObject.DontDestroyOnLoad(font);
                    Logger.Info($"字体 '{font.name}' 已持久化");

                    _cachedFont = font;
                }
                else
                {
                    Logger.Error($"❌ 所有加载方式都失败，无法加载字体: {fontFileName}");
                }

                return font;
            }
        }

        /// <summary>
        /// 从 AssetBundle 加载 TMP 字体
        /// </summary>
        private static TMP_FontAsset LoadFromAssetBundle(string bundlePath)
        {
            try
            {
                AssetBundle bundle = AssetBundle.LoadFromFile(bundlePath);
                if (bundle == null)
                {
                    Logger.Warning($"无法加载 AssetBundle: {bundlePath}");
                    return null;
                }

                // 尝试加载所有 TMP_FontAsset
                TMP_FontAsset[] allFonts = bundle.LoadAllAssets<TMP_FontAsset>();
                TMP_FontAsset font = allFonts.FirstOrDefault();

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

                bundle.Unload(false);
                return font;
            }
            catch (Exception ex)
            {
                Logger.Error($"从 AssetBundle 加载字体失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 从系统字体创建 TMP FontAsset
        /// </summary>
        private static TMP_FontAsset CreateFromSystemFont(string fontName)
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
                Font systemFont = Resources.FindObjectsOfTypeAll<Font>()
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
        public static TMP_FontAsset GetLoadedFont()
        {
            return _cachedFont;
        }
    }
}