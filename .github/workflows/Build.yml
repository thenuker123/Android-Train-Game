#if UNITY_EDITOR
using UnityEditor;
using System.IO;
using UnityEngine;

public static class BuildScript
{
    public static void PerformAndroidBuild()
    {
        Debug.Log("[Cloud Build] Commencing internal programmatic android compiler routine...");

        // 1. Gather all active maps/scenes configured for the project
        string[] testScenes = { "Assets/Scenes/SampleScene.unity" }; 

        // 2. Establish target output directories
        string outputDirectory = Path.Combine(Application.dataPath, "../build/Android");
        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }
        string targetFilePath = Path.Combine(outputDirectory, "DerailValleyMobile.apk");

        // 3. Configure deep compiler parameters matching Android specifications
        BuildPlayerOptions options = new BuildPlayerOptions();
        options.scenes = testScenes;
        options.locationPathName = targetFilePath;
        options.target = BuildTarget.Android;
        options.options = BuildOptions.None;

        // Force Android platform parameters to render normal APK formats instead of app bundles (AAB)
        EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.Generic;
        EditorUserBuildSettings.exportAsGoogleAndroidProject = false;

        // 4. Trigger structural build compilation engine execution
        var compilationReport = BuildPipeline.BuildPlayer(options);
        var constructionResult = compilationReport.summary.result;

        if (constructionResult == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log($"[Cloud Build] Success! Target file package rendered cleanly at: {targetFilePath}");
        }
        else
        {
            Debug.LogError($"[Cloud Build] Compilation terminal error! Summary output state returned: {constructionResult}");
            throw new System.Exception("Unity Headless Engine compilation aborted due to code errors.");
        }
    }
}
#endif
