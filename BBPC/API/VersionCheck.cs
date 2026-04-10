using BepInEx;
using UnityEngine;

namespace BBPC.API
{
    public static class VersionCheck
    {
        public static bool CheckGameVersion(string[] expectedVersions, PluginInfo info)
        {
            foreach (string expectedVersion in expectedVersions)
            {
                if (Application.version == expectedVersion)
                {
                    return true;
                }
            }

            string versions = string.Join(", ", expectedVersions);
            string errorMessage = $"游戏版本 ({Application.version}) 与要求的版本 ({versions}) 不匹配。模组可能无法正常工作。";
            API.Logger.Error(errorMessage);
            return false;
        }
    }
}
