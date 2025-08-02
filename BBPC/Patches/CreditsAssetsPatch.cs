using HarmonyLib;
using MTM101BaldAPI.AssetTools;
using System.IO;
using System.Linq;
using UnityEngine;
using BBPC.API;

namespace BBPC.Patches
{
    [HarmonyPatch(typeof(Credits), "Start")]
    internal class CreditsAssetsPatch
    {
        private static bool textureReplaced = false;

        [HarmonyPostfix]
        [HarmonyPriority(Priority.High)]
        private static void CreditsStartPostfix(Credits __instance)
        {
            if (textureReplaced || !ConfigManager.AreTexturesEnabled())
            {
                return;
            }

            string textureName = "AwaitingSubmission";

            try
            {
                Texture2D originalTexture = Resources.FindObjectsOfTypeAll<Texture2D>().FirstOrDefault(t => t.name == textureName);
                if (originalTexture == null)
                {
                    return;
                }

                string modPath = AssetLoader.GetModPath(Plugin.Instance);
                string filePath = Path.Combine(modPath, "Textures", textureName + ".png");

                if (!File.Exists(filePath))
                {
                    return;
                }

                Texture2D newTexture = AssetLoader.TextureFromFile(filePath);
                if (newTexture == null)
                {
                    API.Logger.Warning($"无法从 {filePath} 加载纹理");
                    return;
                }

                if (originalTexture.width != newTexture.width || originalTexture.height != newTexture.height)
                {
                    API.Logger.Warning($"纹理 '{textureName}' 的尺寸与原始尺寸不匹配。替换已取消。");
                    Object.Destroy(newTexture);
                    return;
                }

                newTexture = AssetLoader.AttemptConvertTo(newTexture, originalTexture.format);
                AssetLoader.ReplaceTexture(originalTexture, newTexture);
                textureReplaced = true;
                API.Logger.Info($"制作人员名单中的纹理 '{textureName}' 已被替换。");
            }
            catch (System.Exception ex)
            {
                API.Logger.Error($"替换制作人员名单纹理时出错: {ex}");
            }
        }
    }
}