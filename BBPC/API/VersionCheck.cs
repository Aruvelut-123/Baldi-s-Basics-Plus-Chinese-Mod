using BepInEx;
using System;
using System.Linq;
using UnityEngine;

namespace BBPC.API
{
    public static class VersionCheck
    {
        /// <summary>
        /// 检查游戏版本是否匹配预期版本列表
        /// </summary>
        /// <param name="expectedVersions">支持的版本列表</param>
        /// <returns>如果版本匹配返回 true，否则返回 false</returns>
        public static bool CheckGameVersion(string[] expectedVersions)
        {
            if (expectedVersions == null || expectedVersions.Length == 0)
            {
                API.Logger.Warning("未指定预期版本，跳过版本检查");
                return true;
            }

            string currentVersion = Application.version;
            bool isSupported = expectedVersions.Contains(currentVersion);

            if (!isSupported)
            {
                string expectedList = string.Join(", ", expectedVersions);
                API.Logger.Error(
                    $"游戏版本 ({currentVersion}) 与要求的版本 ({expectedList}) 不匹配。" +
                    $"汉化模组可能无法正常工作。"
                );
            }
            else
            {
                API.Logger.Info($"游戏版本匹配: {currentVersion}");
            }

            return isSupported;
        }
    }
}