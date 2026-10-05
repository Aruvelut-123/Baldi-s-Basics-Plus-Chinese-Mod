using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace BBPC.API
{
    public static class LocalizationManagerExtensions
    {
        private static readonly FieldInfo? LocalizedTextField =
            AccessTools.Field(typeof(LocalizationManager), "localizedText");

        public static bool HasKey(this LocalizationManager? localization, string key)
        {
            if (localization == null || string.IsNullOrEmpty(key) || LocalizedTextField == null)
            {
                return false;
            }

            try
            {
                Dictionary<string, string>? localizedText =
                    LocalizedTextField.GetValue(localization) as Dictionary<string, string>;
                return localizedText != null && localizedText.ContainsKey(key);
            }
            catch (Exception ex)
            {
                Logger.Warning($"Unable to inspect localization key '{key}': {ex.Message}");
                return false;
            }
        }
    }
}
