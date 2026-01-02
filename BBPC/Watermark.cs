using TMPro;
using UnityEngine.UI;
using UnityEngine;
using BBPC.API;

namespace BBPC
{
    public class Watermark
    {
        private GameObject? watermarkGO;
        private Plugin plugin;
        public static GameObject? canvas;
        private string DEV_MARK = Plugin.Instance.GetTranslationKey("BBPC_Watermark_Dev", "Using Development Version\nDo not disturb this!");
        private string ALPHA_MARK = Plugin.Instance.GetTranslationKey("BBPC_Watermark_Alpha", "Using Alpha Version\nMay contains translation mistake,\nrobot translations and several bugs!");
        private string BETA_MARK = Plugin.Instance.GetTranslationKey("BBPC_Watermark_Beta", "Using Beta Version\nMay contains several translation\nmistake and several bugs!");

        public Watermark(bool is_dev, bool is_alpha, bool is_beta, Plugin plug)
        {
            plugin = plug;
            create_watermark(is_dev, is_alpha, is_beta);
            if (ConfigManager.show_watermark.Value && canvas != null && watermarkGO != null) { canvas.SetActive(false); watermarkGO.SetActive(false); }
        }

        public void update_watermark(bool is_dev, bool is_alpha, bool is_beta)
        {
            if (watermarkGO != null)
            {
                watermarkGO.SetActive(false);
                if (is_dev)
                {
                    watermarkGO.GetComponent<TextMeshProUGUI>().text = DEV_MARK;
                }
                else if (is_alpha)
                {
                    watermarkGO.GetComponent<TextMeshProUGUI>().text = ALPHA_MARK;
                }
                else if (is_beta)
                {
                    watermarkGO.GetComponent<TextMeshProUGUI>().text = BETA_MARK;
                }
                else
                {
                    watermarkGO.GetComponent<TextMeshProUGUI>().text = "";
                }
                if (ConfigManager.show_watermark.Value && canvas != null) { canvas.SetActive(false); watermarkGO.SetActive(false); }
                else if (canvas != null) { watermarkGO.SetActive(true); canvas.SetActive(true); }
            }
        }

        public void create_canvas()
        {
            canvas = new GameObject("Canvas_moded");
            Canvas real_canvas = canvas.AddComponent<Canvas>();
            real_canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            real_canvas.sortingOrder = 1000;
            CanvasScaler scaler = canvas.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            Object.DontDestroyOnLoad(canvas);
        }

        public void create_watermark(bool is_dev, bool is_alpha, bool is_beta)
        {
            create_canvas();
            watermarkGO = new GameObject("DevWatermark");
            if (canvas != null) watermarkGO.transform.SetParent(canvas.transform, false);
            TextMeshProUGUI watermarkText = watermarkGO.AddComponent<TextMeshProUGUI>();
            if (is_dev)
            {
                watermarkGO.GetComponent<TextMeshProUGUI>().text = DEV_MARK;
            }
            else if (is_alpha)
            {
                watermarkGO.GetComponent<TextMeshProUGUI>().text = ALPHA_MARK;
            }
            else if (is_beta)
            {
                watermarkGO.GetComponent<TextMeshProUGUI>().text = BETA_MARK;
            }
            watermarkText.fontSize = 24;
            watermarkText.color = Color.red;
            watermarkText.alignment = TextAlignmentOptions.BottomRight;
            RectTransform rt = watermarkGO.GetComponent<RectTransform>();
            if (canvas != null) rt.SetParent(canvas.transform, false);
            rt.anchorMin = new Vector2(1, 0);
            rt.anchorMax = new Vector2(1, 0);
            rt.pivot = new Vector2(1, 0);
            rt.anchoredPosition = new Vector2(-20, 20);
            rt.sizeDelta = new Vector2(400, 50);
        }
    }
}
