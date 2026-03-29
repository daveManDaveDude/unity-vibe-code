using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace VibeCode.Build
{
    public static class BuildScripts
    {
        private const string DefaultBuildName = "unity-vibe-code";
        private const string DefaultBuildRoot = "Builds";

        public static void BuildWindows64()
        {
            RunBuild(BuildTarget.StandaloneWindows64, ".exe");
        }

        public static void BuildMacOS()
        {
            RunBuild(BuildTarget.StandaloneOSX, ".app");
        }

        public static void BuildLinux64()
        {
            RunBuild(BuildTarget.StandaloneLinux64, string.Empty);
        }

        private static void RunBuild(BuildTarget target, string requiredExtension)
        {
            try
            {
                string[] scenes = EditorBuildSettings.scenes
                    .Where(scene => scene.enabled)
                    .Select(scene => scene.path)
                    .ToArray();

                if (scenes.Length == 0)
                {
                    throw new BuildFailedException("No enabled scenes were found in Build Settings.");
                }

                string buildRoot = GetCommandLineArg("-customBuildPath") ?? Path.Combine(DefaultBuildRoot, target.ToString());
                string buildName = GetCommandLineArg("-customBuildName") ?? DefaultBuildName;
                bool developmentBuild = HasCommandLineFlag("-developmentBuild");
                string locationPathName = ResolveLocationPath(buildRoot, buildName, requiredExtension);
                string outputDirectory = Path.GetDirectoryName(locationPathName) ?? buildRoot;

                Directory.CreateDirectory(outputDirectory);

                BuildOptions options = BuildOptions.StrictMode | BuildOptions.CompressWithLz4HC;
                if (developmentBuild)
                {
                    options |= BuildOptions.Development | BuildOptions.AllowDebugging;
                }

                var buildOptions = new BuildPlayerOptions
                {
                    scenes = scenes,
                    target = target,
                    locationPathName = locationPathName,
                    options = options
                };

                Debug.Log($"Starting {target} build to '{locationPathName}'.");
                BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
                BuildSummary summary = report.summary;

                if (summary.result != BuildResult.Succeeded)
                {
                    throw new BuildFailedException(
                        $"Build failed for {target}. Result: {summary.result}. Errors: {summary.totalErrors}, Warnings: {summary.totalWarnings}.");
                }

                Debug.Log(
                    $"Build succeeded for {target}. Output: '{summary.outputPath}'. Size: {summary.totalSize} bytes. Duration: {summary.totalTime}.");
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static string ResolveLocationPath(string buildRoot, string buildName, string requiredExtension)
        {
            string normalizedBuildRoot = Path.GetFullPath(buildRoot);
            string finalBuildName = buildName;

            if (!string.IsNullOrEmpty(requiredExtension) &&
                !finalBuildName.EndsWith(requiredExtension, StringComparison.OrdinalIgnoreCase))
            {
                finalBuildName = $"{Path.GetFileNameWithoutExtension(finalBuildName)}{requiredExtension}";
            }

            return Path.Combine(normalizedBuildRoot, finalBuildName);
        }

        private static string GetCommandLineArg(string argumentName)
        {
            string[] arguments = Environment.GetCommandLineArgs();
            for (int index = 0; index < arguments.Length - 1; index++)
            {
                if (arguments[index] == argumentName)
                {
                    return arguments[index + 1];
                }
            }

            return null;
        }

        private static bool HasCommandLineFlag(string argumentName)
        {
            return Environment.GetCommandLineArgs().Any(argument => argument == argumentName);
        }
    }
}
