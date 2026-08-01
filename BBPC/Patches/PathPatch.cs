using HarmonyLib;
using MTM101BaldAPI.AssetTools;
using PlusLevelStudio;
using System.IO;
using System.Linq;

namespace BBPC.EditorExtension.Patches
{
    [HarmonyPatch(typeof(Path))]
    public class PathPatch
    {
        private static string? _cachedModPath;
        private static string? _cachedLevelStudioPath;
        private static bool _isGettingPath = false;
        private static bool _initialized = false;

        // Initialize both paths at startup
        public static void Initialize()
        {
            if (_initialized) return;

            try
            {
                _cachedModPath = AssetLoader.GetModPath(BBPC.Plugin.Instance);
                _cachedLevelStudioPath = AssetLoader.GetModPath(LevelStudioPlugin.Instance);
                _initialized = true;
                API.Logger.Debug($"ModPath initialized: {_cachedModPath}");
                API.Logger.Debug($"LevelStudioPath initialized: {_cachedLevelStudioPath}");
            }
            catch (System.Exception ex)
            {
                API.Logger.Error($"Failed to initialize paths: {ex.Message}");
            }
        }

        // This will catch Path.Combine(params string[] paths)
        [HarmonyPatch(nameof(Path.Combine), new[] { typeof(string[]) })]
        [HarmonyPrefix]
        public static bool CombinePrefix(string[] paths, ref string __result)
        {
            // Not care if is English
            if (Plugin.IsEnglish) return true;

            // Prevent recursion
            if (_isGettingPath) return true;

            if (paths == null || paths.Length < 3) return true;

            // Check if we should modify this result
            if (!string.IsNullOrEmpty(paths[0]) && !string.IsNullOrEmpty(_cachedLevelStudioPath) && paths[0].Contains(_cachedLevelStudioPath) &&
                !string.IsNullOrEmpty(paths[1]) && paths[1].Contains("Data") &&
                !string.IsNullOrEmpty(paths[2]) && paths[2].Contains("UI"))
            {
                try
                {
                    // Get mod path once and cache it
                    if (string.IsNullOrEmpty(_cachedModPath))
                    {
                        _isGettingPath = true;
                        try
                        {
                            _cachedModPath = AssetLoader.GetModPath(BBPC.Plugin.Instance);
                        }
                        finally
                        {
                            _isGettingPath = false;
                        }
                    }

                    string? cachedModPath = _cachedModPath;
                    if (!string.IsNullOrEmpty(cachedModPath))
                    {
                        var newPaths = paths.ToArray();
                        newPaths[0] = cachedModPath!;
                        newPaths[1] = "EditorData";

                        __result = string.Join(Path.DirectorySeparatorChar.ToString(), newPaths);
                        API.Logger.Debug($"Modified Path.Combine result: '{__result}'");
                        return false;
                    }
                }
                catch (System.Exception ex)
                {
                    API.Logger.Error($"Error modifying Path.Combine result: {ex.Message}");
                    return true;
                }
            }
            return true;
        }
    }
}
