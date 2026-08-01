using BepInEx.Logging;

namespace BBPC.NULLStyle.API
{
    internal static class Logger
    {
        private static ManualLogSource? source;

        internal static void Initialize(ManualLogSource logSource)
        {
            source = logSource;
        }

        internal static void Debug(string message) => source?.LogDebug(message);
        internal static void Info(string message) => source?.LogInfo(message);
        internal static void Warning(string message) => source?.LogWarning(message);
        internal static void Error(string message) => source?.LogError(message);
    }
}
