namespace BBPC.API
{
    public static class RestartManager
    {
        private static bool languageRestartRequested;

        public static bool IsLanguageRestartRequested => languageRestartRequested;

        public static void RequestLanguageRestart()
        {
            languageRestartRequested = true;
        }

        /// <summary>
        /// Confirms that the user may exit. A staged DLL is installed by the
        /// BepInEx preloader patcher during the next game launch, before BBPC loads.
        /// </summary>
        public static bool PrepareExit()
        {
            return true;
        }
    }
}
