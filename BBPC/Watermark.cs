using TMPro;
using UnityEngine.UI;
using UnityEngine;

namespace BBPC
{
    public class Watermark
    {
        private GameObject watermarkGO;
        private Plugin plugin;
        public static GameObject canvas;
        const string DEV_MARK = "正在使用开发版\n请勿发布此版本!";
        const string ALPHA_MARK = "正在使用 Alpha 版\n可能会有翻译错误、机翻和些许 bug!";
        const string BETA_MARK = "正在使用 Beta 版\n可能会有少量翻译错误和些许 bug!";

        public Watermark(bool is_dev, bool is_alpha, bool is_beta, Plugin plug)
        {
            plugin = plug;
            create_watermark(is_dev, is_alpha, is_beta);
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
                watermarkGO.SetActive(true);
            }
        }

        public void RemoveWatermark()
        {
            if (watermarkGO != null)
            {
                plugin.des(watermarkGO);
                watermarkGO = null;
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

        public GameObject create_watermark(bool is_dev, bool is_alpha, bool is_beta)
        {
            create_canvas();
            if (canvas != null) {
                var watermarkGO = new GameObject("DevWatermark");
                watermarkGO.transform.SetParent(canvas.transform, false);
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
                else
                {
                    plugin.des(watermarkGO);
                    return null;
                }
                watermarkText.fontSize = 24;
                watermarkText.color = Color.red;
                watermarkText.alignment = TextAlignmentOptions.BottomRight;
                RectTransform rt = watermarkGO.GetComponent<RectTransform>();
                rt.SetParent(canvas.transform, false);
                rt.anchorMin = new Vector2(1, 0);
                rt.anchorMax = new Vector2(1, 0);
                rt.pivot = new Vector2(1, 0);
                rt.anchoredPosition = new Vector2(-20, 20);
                rt.sizeDelta = new Vector2(400, 50);
                return watermarkGO;
            }
            else
            {
                return null;
            }
        }
    }
}
