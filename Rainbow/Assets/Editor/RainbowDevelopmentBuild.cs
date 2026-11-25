using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class RainbowDevelopmentBuild
{
    public static void AndroidApk()
    {
        string output = Environment.GetEnvironmentVariable("RAINBOW_DEV_APK");
        if (string.IsNullOrWhiteSpace(output) || !Path.IsPathRooted(output)
            || !output.EndsWith(".apk", StringComparison.OrdinalIgnoreCase))
            throw new BuildFailedException("Set RAINBOW_DEV_APK to an absolute APK path.");

        Directory.CreateDirectory(Path.GetDirectoryName(output));
        BuildAndroid(output, false);
    }

    public static void ExportFirebaseAndroid()
    {
        string output = Environment.GetEnvironmentVariable("RAINBOW_ANDROID_EXPORT");
        if (string.IsNullOrWhiteSpace(output) || !Path.IsPathRooted(output) || Directory.Exists(output))
            throw new BuildFailedException("Set RAINBOW_ANDROID_EXPORT to a new absolute directory.");
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("RAINBOW_GOOGLE_WEB_CLIENT_ID")))
            throw new BuildFailedException("Unity requires RAINBOW_GOOGLE_WEB_CLIENT_ID in its environment.");
        if (!File.Exists(Path.Combine(Application.dataPath, "google-services.json")))
            throw new BuildFailedException("Add the local Android Firebase configuration before exporting.");

        // The Firebase overlay is optional, so keep the SDK-free Editor build compilable.
        var resolver = Type.GetType("GooglePlayServices.PlayServicesResolver, Google.JarResolver");
        MethodInfo resolve = resolver?.GetMethod("ResolveSync", new[] { typeof(bool) });
        if (resolve?.Invoke(null, new object[] { false }) is not true)
            throw new BuildFailedException("Install the Firebase SDK overlay and resolve Android dependencies.");

        Debug.Log("Firebase Android export: Unity inherited the Web client ID.");
        BuildAndroid(output, true);
    }

    private static void BuildAndroid(string output, bool export)
    {
        var options = new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
            locationPathName = output,
            target = BuildTarget.Android,
            options = export
                ? BuildOptions.Development | BuildOptions.AcceptExternalModificationsToPlayer
                : BuildOptions.Development
        };
        bool previousAppBundle = EditorUserBuildSettings.buildAppBundle;
        bool previousExport = EditorUserBuildSettings.exportAsGoogleAndroidProject;
        EditorUserBuildSettings.buildAppBundle = false;
        EditorUserBuildSettings.exportAsGoogleAndroidProject = export;
        try
        {
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException($"Android development build {report.summary.result}.");
        }
        finally
        {
            EditorUserBuildSettings.exportAsGoogleAndroidProject = previousExport;
            EditorUserBuildSettings.buildAppBundle = previousAppBundle;
        }
    }
}
