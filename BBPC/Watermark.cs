using TMPro;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System;

namespace BBPC
{
    public class Watermark
    {
        private GameObject watermarkGO;
        private Plugin plugin;
        public static Canvas SharedCanvas { get; private set; }

        private static void EnsureCanvasExists()
        {
            if (SharedCanvas == null)
            {
                GameObject canvasGO = GameObject.Find("Canvas");
                if (canvasGO == null)
                {
                    canvasGO = new GameObject("Canvas");
                    SharedCanvas = canvasGO.AddComponent<Canvas>();
                    SharedCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvasGO.AddComponent<CanvasScaler>();
                    canvasGO.AddComponent<GraphicRaycaster>();
                }
                else
                {
                    SharedCanvas = canvasGO.GetComponent<Canvas>();
                }
            }
        }

        public Watermark(bool is_dev, bool is_alpha, bool is_beta, Plugin plug)
        {
            plugin = plug;
            create_watermark(is_dev, is_alpha, is_beta);
        }

        public void update_watermark()
        {
            if (watermarkGO != null)
            {
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

        public GameObject create_watermark(bool is_dev, bool is_alpha, bool is_beta)
        {
            EnsureCanvasExists();
            var watermarkGO = new GameObject("DevWatermark");
            watermarkGO.transform.SetParent(SharedCanvas.transform, false);
            TextMeshProUGUI watermarkText = watermarkGO.AddComponent<TextMeshProUGUI>();
            if (is_dev)
            {
                watermarkText.text = "RUNNING UNDER DEV BUILD\nDO NOT PUBLISH THIS!";
            }
            else if (is_alpha)
            {
                watermarkText.text = "RUNNING UNDER ALPHA BUILD\nMay have many bugs!";
            }
            else if (is_beta)
            {
                watermarkText.text = "RUNNING UNDER BETA BUILD\nStill not stable until release!";
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
            rt.SetParent(SharedCanvas.transform, false);
            rt.anchorMin = new Vector2(1, 0);
            rt.anchorMax = new Vector2(1, 0);
            rt.pivot = new Vector2(1, 0);
            rt.anchoredPosition = new Vector2(-20, 20);
            rt.sizeDelta = new Vector2(400, 50);
            return watermarkGO;
        }
    }
}
