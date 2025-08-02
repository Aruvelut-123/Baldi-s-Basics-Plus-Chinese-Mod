using BepInEx;
using MTM101BaldAPI;
using System;
using UnityEngine;

namespace BBPC.API
{
    public static class VersionCheck
    {
        public static bool CheckGameVersion(string expectedVersion, PluginInfo info)
        {
            if (Application.version != expectedVersion)
            {
                string errorMessage = $"游戏版本 ({Application.version}) 与要求的版本 ({expectedVersion}) 不匹配。模组可能无法正常工作。";
                API.Logger.Error(errorMessage);
                // 对于关键错误，可以使用：
                // MTM101BaldiDevAPI.CauseCrash(info, new Exception(errorMessage));
                return false;
            }
            return true;
        }
    }
}