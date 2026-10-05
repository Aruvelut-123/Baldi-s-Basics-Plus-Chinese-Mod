using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace BBPC.API
{
    public static class LocalizationExtensions
    {
        /// <summary>
        /// Ensures that a UI text uses BBPC's localizer and refreshes it when needed.
        /// External components with the same simple type name are removed first so
        /// the translation is always handled by BBPC.TextLocalizer.
        /// </summary>
        public static TextLocalizer? ApplyLocalization(this TextMeshProUGUI textComponent,
            string key,
            bool forceRefresh = false)
        {
            if (textComponent == null || string.IsNullOrEmpty(key))
            {
                return null;
            }

            RemoveForeignLocalizers(textComponent);
            TextLocalizer localizer = textComponent.GetComponent<TextLocalizer>()
                ?? textComponent.gameObject.AddComponent<TextLocalizer>();

            if (forceRefresh || !string.Equals(localizer.key, key, StringComparison.Ordinal))
            {
                localizer.key = key;
                localizer.RefreshLocalization();
            }

            return localizer;
        }

        public static TextLocalizer? ApplyLocalization(this TMP_Text textComponent,
            string key,
            bool forceRefresh = false)
        {
            return textComponent is TextMeshProUGUI textMesh
                ? textMesh.ApplyLocalization(key, forceRefresh)
                : null;
        }

        public static TextLocalizer? ApplyLocalization(this GameObject gameObject,
            string key,
            bool forceRefresh = false)
        {
            return gameObject == null
                ? null
                : gameObject.GetComponent<TextMeshProUGUI>()?.ApplyLocalization(key, forceRefresh);
        }

        public static void ApplyLocalizations(this Transform root,
            IReadOnlyDictionary<string, string> targets,
            bool forceRefresh = false)
        {
            if (root == null || targets == null)
            {
                return;
            }

            if (targets.Count == 0) return;
            Dictionary<string, Transform> paths = root.BuildPathMap();
            foreach (KeyValuePair<string, string> target in targets)
            {
                if (paths.TryGetValue(target.Key, out Transform elementTransform))
                {
                    TextMeshProUGUI textComponent = elementTransform.GetComponent<TextMeshProUGUI>();
                    if (textComponent != null) textComponent.ApplyLocalization(target.Value, forceRefresh);
                }
            }
        }

        private static void RemoveForeignLocalizers(Component textComponent)
        {
            foreach (Component component in textComponent.GetComponents<Component>())
            {
                if (component != null &&
                    component.GetType().Name == nameof(TextLocalizer) &&
                    component.GetType() != typeof(TextLocalizer))
                {
                    UnityEngine.Object.Destroy(component);
                }
            }
        }
    }
}
