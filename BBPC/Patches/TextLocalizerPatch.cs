using System;
using UnityEngine;
using TMPro;
using System.Collections.Generic;
using BBPC.API;
using Logger = BBPC.API.Logger;

namespace BBPC
{
    public class TextLocalizer : MonoBehaviour
    {
        public string key = null!;
        public List<String> replaces = [];
        private TextMeshProUGUI textComponent = null!;
        private bool initialized = false;
        
        private void Awake()
        {
            textComponent = GetComponent<TextMeshProUGUI>();
            ApplyLocalization(); 
            initialized = true;
        }

        private void OnEnable()
        {
            if (initialized)
                ApplyLocalization();
        }

        public object? RefreshLocalization()
        {
            return ApplyLocalization();
        }

        private object? ApplyLocalization()
        {
            if (textComponent != null && !string.IsNullOrEmpty(key) && Singleton<LocalizationManager>.Instance != null)
            {
                Logger.Debug("Applying translation to " + textComponent.name);
                if (Singleton<LocalizationManager>.Instance.HasKey(key))
                {
                    Logger.Debug("Key " + key + " found! Applying translation.");
                    string localizedText = Singleton<LocalizationManager>.Instance.GetLocalizedText(key);
                    Logger.Debug("Get localized text of key " + key + ": "+localizedText);
                    if (!string.IsNullOrEmpty(localizedText) && textComponent.text != localizedText)
                    {
                        if (replaces.Count > 0)
                        {
                            int i = 0;
                            foreach (String replace in replaces)
                            {
                                localizedText = localizedText.Replace("{"+i.ToString()+"}", replaces[i]);
                            }
                        }
                        textComponent.text = localizedText;
                        return localizedText;
                    }
                }
                Logger.Warning("Failed to find translation or text for key " + key + " or the default text is same as localized text, returning fallback value:" + textComponent.text + ".");
                return textComponent.text;
            }
            return null;
        }
    }
} 