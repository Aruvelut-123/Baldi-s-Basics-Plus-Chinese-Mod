using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace BBPC.API
{
    public static class PosterScanner
    {
        /// <param name="modPath">模组文件夹路径</param>
        public static void ScanAndExportNewPosters(string modPath)
        {
            if (!ConfigManager.IsDevModeEnabled())
            {
                Logger.Debug("开发者模式已禁用，海报提取已跳过。");
                return;
            }

            string postersPath = Path.Combine(modPath, "PosterFiles");
            if (!Directory.Exists(postersPath))
            {
                Directory.CreateDirectory(postersPath);
                Logger.ForceInfo($"[DEV MODE] 已创建海报文件夹: {postersPath}");
            }

            Logger.ForceInfo("=== 开始扫描海报 (DEV MODE) ===");
            Logger.ForceInfo($"扫描游戏资源...");

            PosterObject[] allPosters = Resources.FindObjectsOfTypeAll<PosterObject>();
            Logger.ForceInfo($"游戏中发现的海报: {allPosters.Length}");
            Logger.ForceInfo($"检查文件夹: {postersPath}");
            Logger.ForceInfo("---");

            int newPostersCount = 0;
            int existingPostersCount = 0;
            int errorCount = 0;

            foreach (PosterObject poster in allPosters)
            {
                try
                {
                    string posterFolderPath = Path.Combine(postersPath, poster.name);
                    string posterDataPath = Path.Combine(posterFolderPath, "PosterData.json");

                    Logger.ForceInfo($"海报检查: {poster.name}");

                    if (File.Exists(posterDataPath))
                    {
                        existingPostersCount++;
                        Logger.ForceInfo($"  └─ 状态：已存在");
                        continue;
                    }

                    newPostersCount++;
                    Logger.ForceInfo($"  └─ 状态：【新海报】");

                    if (!Directory.Exists(posterFolderPath))
                    {
                        Directory.CreateDirectory(posterFolderPath);
                        Logger.ForceInfo($"  └─ [文件夹已创建]: {posterFolderPath}");
                    }
                    else
                    {
                        Logger.ForceInfo($"  └─ 文件夹已存在: {posterFolderPath}");
                    }

                    ExportPosterData(poster, posterDataPath);
                    LogPosterDetails(poster);
                    Logger.ForceInfo("---");
                }
                catch (Exception ex)
                {
                    errorCount++;
                    Logger.ForceWarning($"  └─ [ERROR] 处理海报 '{poster.name}' 时出现错误: {ex.Message}");
                    Logger.ForceInfo("---");
                }
            }

            Logger.ForceInfo("=== 海报扫描完成 ===");
            Logger.ForceInfo($"扫描结果:");
            Logger.ForceInfo($"  游戏中海报总数: {allPosters.Length}");
            Logger.ForceInfo($"  已存在: {existingPostersCount}");
            Logger.ForceInfo($"  新发现: {newPostersCount}");

            if (errorCount > 0)
            {
                Logger.ForceWarning($"  处理过程中出现的错误: {errorCount}");
            }

            if (newPostersCount > 0)
            {
                Logger.ForceInfo("");
                Logger.ForceWarning("!!! 注意 !!!");
                Logger.ForceWarning($"发现新海报: {newPostersCount}");
                Logger.ForceWarning("检查您的 PosterFiles 文件夹并添加翻译!");
                Logger.ForceWarning("添加翻译后，请在配置中禁用开发者模式!");
            }
            else
            {
                Logger.ForceInfo("未找到新海报。所有海报均已添加完毕。");
            }
        }

        private static void ExportPosterData(PosterObject poster, string outputPath)
        {
            PosterTextTable posterTable = new PosterTextTable();

            if (poster.textData != null && poster.textData.Length > 0)
            {
                Logger.ForceInfo($"  └─ 导出海报数据...");
                foreach (var textData in poster.textData)
                {
                    PosterTextData exportData = new PosterTextData
                    {
                        textKey = textData.textKey,
                        position = new IntVector2(textData.position.x, textData.position.z),
                        size = new IntVector2(textData.size.x, textData.size.z),
                        fontSize = textData.fontSize,
                        color = textData.color
                    };

                    posterTable.items.Add(exportData);
                }
            }

            string json = JsonUtility.ToJson(posterTable, true);
            File.WriteAllText(outputPath, json);

            Logger.ForceInfo($"  └─ [创建 JSON]: PosterData.json");
            Logger.ForceInfo($"     文本元素: {posterTable.items.Count}");
            Logger.ForceInfo($"     完整路径: {outputPath}");
        }

        private static void LogPosterDetails(PosterObject poster)
        {
            Logger.ForceInfo($"  └─ 海报详情:");
            Logger.ForceInfo($"     文本元素: {poster.textData?.Length ?? 0}");

            if (poster.textData != null && poster.textData.Length > 0)
            {
                for (int i = 0; i < poster.textData.Length; i++)
                {
                    var textData = poster.textData[i];
                    Logger.Debug($"     元素 {i}: Key='{textData.textKey}', FontSize={textData.fontSize}, Pos=({textData.position.x}, {textData.position.z})");
                }
            }
        }
    }
}
