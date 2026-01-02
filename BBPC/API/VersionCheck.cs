using BepInEx;
using UnityEngine;

namespace BBPC.API
{
    public static class VersionCheck
    {
        public static bool CheckGameVersion(string[] expectedVersions, PluginInfo info)
        {
            bool is_supported = false;
            foreach (string expectedVersion in expectedVersions) {
                if (Application.version == expectedVersion)
                {
                    is_supported = true;
                    break;
                }
            }
            if (!is_supported)
            {
                string errorMessage = $"游戏版本 ({Application.version}) 与要求的版本 ({expectedVersions.ToString()}) 不匹配。模组可能无法正常工作。";
                API.Logger.Error(errorMessage);
            }
            return is_supported;
        }
    }
}