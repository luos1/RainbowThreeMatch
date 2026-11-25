using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

public static class RainbowDevelopmentBuild
{
    public static void AndroidApk()
    {
        string output = Environment.GetEnvironmentVariable("RAINBOW_DEV_APK");
        if (string.IsNullOrWhiteSpace(output) || !Path.IsPathRooted(output)
            || !output.EndsWith(".apk", StringComparison.OrdinalIgnoreCase))
            throw new BuildFailedException("Set RAINBOW_DEV_APK to an absolute APK path.");

        Directory.CreateDirectory(Path.GetDirectoryName(output));
        var options = new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
            locationPathName = output,
            target = BuildTarget.Android,
            options = BuildOptions.Development
        };
        bool previousAppBundle = EditorUserBuildSettings.buildAppBundle;
        EditorUserBuildSettings.buildAppBundle = false;
        try
        {
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException($"Android development build {report.summary.result}.");
        }
        finally
        {
            EditorUserBuildSettings.buildAppBundle = previousAppBundle;
        }
    }
}
