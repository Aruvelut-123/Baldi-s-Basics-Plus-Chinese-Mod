using BepInEx;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace BBPC.UpdatePatcher
{
    /// <summary>
    /// BepInEx preloader patcher that installs a staged BBPC.dll before plugin loading.
    /// The updater cannot overwrite its own loaded assembly safely, so the runtime UI
    /// writes BBPC.dll.pending and this patcher atomically replaces the old file first.
    /// </summary>
    internal static class Patcher
    {
        private const string AssemblyFileName = "BBPC.dll";
        private const string PendingSuffix = ".pending";

        public static IEnumerable<string> TargetDLLs
        {
            get { return new string[0]; }
        }

        public static void Patch(AssemblyDefinition assembly)
        {
            // No managed game assembly changes are required.
        }

        public static void Initialize()
        {
            try
            {
                foreach (string pendingPath in FindPendingUpdates())
                {
                    try
                    {
                        if (TryInstall(pendingPath))
                        {
                            Log("Installed staged BBPC.dll before plugin loading.");
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log("Failed to install staged BBPC.dll: " + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Failed to enumerate staged BBPC.dll files: " + ex.Message);
            }
        }

        private static IEnumerable<string> FindPendingUpdates()
        {
            if (string.IsNullOrEmpty(Paths.PluginPath) || !Directory.Exists(Paths.PluginPath))
            {
                yield break;
            }

            string directPath = Path.Combine(Paths.PluginPath, AssemblyFileName + PendingSuffix);
            if (File.Exists(directPath))
            {
                yield return directPath;
            }

            string[] pendingFiles;
            try
            {
                pendingFiles = Directory.GetFiles(Paths.PluginPath, AssemblyFileName + PendingSuffix,
                    SearchOption.AllDirectories);
            }
            catch
            {
                yield break;
            }

            foreach (string pendingPath in pendingFiles)
            {
                if (!string.Equals(pendingPath, directPath, StringComparison.OrdinalIgnoreCase))
                {
                    yield return pendingPath;
                }
            }
        }

        private static bool TryInstall(string pendingPath)
        {
            if (!File.Exists(pendingPath)) return false;

            string targetPath = pendingPath.Substring(0, pendingPath.Length - PendingSuffix.Length);
            if (!string.Equals(Path.GetFileName(targetPath), AssemblyFileName, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            FileInfo stagedFile = new FileInfo(pendingPath);
            if (stagedFile.Length == 0) return false;

            AssemblyName stagedAssembly = AssemblyName.GetAssemblyName(pendingPath);
            if (!string.Equals(stagedAssembly.Name, "BBPC", StringComparison.OrdinalIgnoreCase))
            {
                Log("Ignoring staged file with unexpected assembly name: " + stagedAssembly.Name);
                return false;
            }

            string? targetDirectory = Path.GetDirectoryName(targetPath);
            if (string.IsNullOrEmpty(targetDirectory)) return false;
            Directory.CreateDirectory(targetDirectory);

            if (File.Exists(targetPath))
            {
                File.Replace(pendingPath, targetPath, null);
            }
            else
            {
                File.Move(pendingPath, targetPath);
            }

            return true;
        }

        private static void Log(string message)
        {
            Console.WriteLine("[BBPCUpdatePatcher] " + message);
        }
    }
}
