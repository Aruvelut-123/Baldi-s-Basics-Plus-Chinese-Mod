using System.Diagnostics;
using BepInEx.Logging;

namespace BBPC.API
{
    public static class Logger
    {
        private static ManualLogSource _bepLogger = null!;

        public static void Init(ManualLogSource logger)
        {
            _bepLogger = logger;
        }

        public static void Debug(string message)
        {
            Write(LogLevel.Debug, message);
        }

        public static void Info(string message)
        {
            Write(LogLevel.Info, message);
        }

        public static void Warning(string message)
        {
            Write(LogLevel.Warning, message);
        }

        public static void Error(string message)
        {
            Write(LogLevel.Error, message);
        }

        public static void ForceInfo(string message)
        {
            Write(LogLevel.Info, message, true);
        }

        public static void ForceWarning(string message)
        {
            Write(LogLevel.Warning, message, true);
        }

        private static void Write(LogLevel level, string message, bool force = false)
        {
            if (!force && !IsLoggingEnabled()) return;

            string formattedMessage = FormatMessage(message);
            if (_bepLogger != null)
            {
                _bepLogger.Log(level, formattedMessage);
                return;
            }

            switch (level)
            {
                case LogLevel.Error:
                    UnityEngine.Debug.LogError(formattedMessage);
                    break;
                case LogLevel.Warning:
                    UnityEngine.Debug.LogWarning(formattedMessage);
                    break;
                default:
                    UnityEngine.Debug.Log(formattedMessage);
                    break;
            }
        }

        private static string FormatMessage(string message)
        {
            StackFrame frame = new StackFrame(3, false);
            MethodBaseInfo method = GetMethodInfo(frame);
            return $"[{method.ClassName}.{method.MethodName}] {message}";
        }

        private static MethodBaseInfo GetMethodInfo(StackFrame frame)
        {
            System.Reflection.MethodBase? method = frame.GetMethod();
            return new MethodBaseInfo(
                method?.DeclaringType?.Name ?? "UnknownClass",
                method?.Name ?? "UnknownMethod");
        }

        private readonly struct MethodBaseInfo
        {
            public readonly string ClassName;
            public readonly string MethodName;

            public MethodBaseInfo(string className, string methodName)
            {
                ClassName = className;
                MethodName = methodName;
            }
        }

        private static bool IsLoggingEnabled()
        {
            return ConfigManager.EnableLogging == null || ConfigManager.IsLoggingEnabled();
        }
    }
}
