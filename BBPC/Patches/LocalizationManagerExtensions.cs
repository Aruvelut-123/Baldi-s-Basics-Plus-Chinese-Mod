namespace BBPC
{
    /// <summary>
    /// Backwards-compatible forwarding extension for callers in the BBPC namespace.
    /// </summary>
    public static class LocalizationManager_Extensions
    {
        public static bool HasKey(this LocalizationManager? manager, string key)
        {
            return API.LocalizationManagerExtensions.HasKey(manager, key);
        }
    }
}
