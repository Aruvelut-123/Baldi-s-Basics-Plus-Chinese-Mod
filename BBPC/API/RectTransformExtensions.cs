using System;
using System.Collections.Generic;
using UnityEngine;

namespace BBPC.API
{
    public static class RectTransformExtensions
    {
        public static Transform? FindTransform(this Transform parent, string path)
        {
            if (parent == null || string.IsNullOrEmpty(path)) return null;
            return parent.Find(path);
        }

        /// <summary>
        /// A snapshot of relative paths, including inactive children. The first
        /// duplicate path wins, matching the original depth-first hierarchy scan.
        /// Build once per batch rather than caching across scene/hierarchy changes.
        /// </summary>
        public static Dictionary<string, Transform> BuildPathMap(this Transform parent)
        {
            var result = new Dictionary<string, Transform>(StringComparer.Ordinal);
            if (parent == null) return result;

            var paths = new Dictionary<Transform, string> { [parent] = string.Empty };
            foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
            {
                if (child == parent) continue;
                string path = GetPath(parent, child, paths);
                if (!result.ContainsKey(path)) result.Add(path, child);
            }

            return result;
        }

        private static string GetPath(Transform root, Transform child, Dictionary<Transform, string> paths)
        {
            if (paths.TryGetValue(child, out string path)) return path;
            Transform parent = child.parent;
            string prefix = parent == root ? string.Empty : GetPath(root, parent, paths) + "/";
            path = prefix + child.name;
            paths.Add(child, path);
            return path;
        }

        private static void ProcessTargets(this Transform root,
            IEnumerable<KeyValuePair<string, Vector2>> targets,
            Action<RectTransform, Vector2> applyAction)
        {
            if (root == null || targets == null) return;
            Dictionary<string, Transform>? paths = null;
            foreach (KeyValuePair<string, Vector2> target in targets)
            {
                // Don't scan at all for an empty batch.
                paths ??= root.BuildPathMap();
                if (paths.TryGetValue(target.Key, out Transform child) &&
                    child.GetComponent<RectTransform>() is RectTransform rect)
                {
                    applyAction(rect, target.Value);
                }
            }
        }

        public static void SetAnchoredPositions(this Transform root, IEnumerable<KeyValuePair<string, Vector2>> targets)
        {
            root.ProcessTargets(targets, (rect, value) => rect.anchoredPosition = value);
        }

        public static void SetSizeDeltas(this Transform root, IEnumerable<KeyValuePair<string, Vector2>> targets)
        {
            root.ProcessTargets(targets, (rect, value) => rect.sizeDelta = value);
        }

        public static void SetOffsetMins(this Transform root, IEnumerable<KeyValuePair<string, Vector2>> targets)
        {
            root.ProcessTargets(targets, (rect, value) => rect.offsetMin = value);
        }

        public static void SetOffsetMaxs(this Transform root, IEnumerable<KeyValuePair<string, Vector2>> targets)
        {
            root.ProcessTargets(targets, (rect, value) => rect.offsetMax = value);
        }
    }
}
