using System;
using UnityEngine;
using TMPro;
using Logger = BBPC.API.Logger;

namespace BBPC
{
    public class TextLocalizer : MonoBehaviour
    {
        public string key = null!;
        private TextMeshProUGUI textComponent = null!;
        private bool initialized = false;
        private string? warnedMissingKey;
        private string? warnedEmptyKey;
        
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
            if (textComponent == null || string.IsNullOrEmpty(key))
            {
                return null;
            }

            LocalizationManager? localization = Singleton<LocalizationManager>.Instance;
            if (localization == null)
            {
                return null;
            }

            if (!localization.HasKey(key))
            {
                if (!string.Equals(warnedMissingKey, key, StringComparison.Ordinal))
                {
                    Logger.Warning($"Missing translation key '{key}' for {textComponent.name}.");
                    warnedMissingKey = key;
                }
                return textComponent.text;
            }

            warnedMissingKey = null;
            string localizedText = localization.GetLocalizedText(key);
            if (string.IsNullOrEmpty(localizedText))
            {
                if (!string.Equals(warnedEmptyKey, key, StringComparison.Ordinal))
                {
                    Logger.Warning($"Translation key '{key}' returned empty text for {textComponent.name}.");
                    warnedEmptyKey = key;
                }
                return textComponent.text;
            }

            warnedEmptyKey = null;
            if (!string.Equals(textComponent.text, localizedText, StringComparison.Ordinal))
            {
                textComponent.text = localizedText;
            }

            return textComponent.text;
        }
    }
} 