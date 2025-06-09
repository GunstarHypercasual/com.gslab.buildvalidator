// BuildPipelineService.cs

using System;
using System.IO;
using System.Text.RegularExpressions;

namespace GSLab.BuildValidator
{
    using UnityEditor;
    using UnityEngine;

    public class BuildPipelineService
    {
        public BuildPipelineService(BuildValidatorSettings _) {}
        public bool BuildAndroid(BuildTarget target, bool isIAP, bool isAPK = true)
        {
            Debug.Log($"Build Android for target: {target}, IAP: {isIAP}, APK: {isAPK}");
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            string pkg = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android).Replace('.', '_');
            string version = PlayerSettings.bundleVersion;
            int versionCode = PlayerSettings.Android.bundleVersionCode;
            string dir = EditorUtility.SaveFolderPanel("Chọn thư mục chứa Build", projectRoot, "Build");
            if (string.IsNullOrEmpty(dir))
            {
                Debug.LogError("No build folder selected.");
                return false;
            }
            string aabName = isIAP ? $"{pkg}_{version}_{versionCode}.aab" : $"{pkg}_no_iap_{version}_{versionCode}.aab";
            string apkName = isIAP ? $"{pkg}_{version}_{versionCode}.aab" : $"{pkg}_no_iap_{version}_{versionCode}.apk";
            string storeDir = Path.Combine(projectRoot, "StoreListing");

            EditorUserBuildSettings.buildAppBundle = true;
            string aabOutputPath = Path.Combine(dir, aabName);
            bool aabSuccess = BuildAndCopy(target, aabOutputPath, Path.Combine(storeDir, aabName));

            bool apkSuccess = true;
            if (isAPK)
            {
                Debug.Log("Building APK...");
                EditorUserBuildSettings.buildAppBundle = false;
                string apkOutputPath = Path.Combine(dir, apkName);
                apkSuccess = BuildAndCopy(target, apkOutputPath, Path.Combine(storeDir, apkName));
            }
            
            return aabSuccess && apkSuccess;
        }

        private bool BuildAndCopy(BuildTarget target, string buildPath, string copyDestination)
        {
            BuildPlayerOptions opts = new()
            {
                scenes = EditorBuildSettingsScene.GetActiveSceneList(EditorBuildSettings.scenes),
                locationPathName = buildPath,
                target = target,
                options = BuildOptions.None
            };
            
            var rep = BuildPipeline.BuildPlayer(opts);
            if (rep.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.LogError($"Build failed for {buildPath}: {rep.summary.result}");
                return false;
            }
            
            try 
            {
                File.Copy(buildPath, copyDestination, true);
                Debug.Log($"Copied build to {copyDestination}");
                return true;

            }
            catch (Exception ex)
            {
                Debug.LogError($"Cannot copy build file: {ex.Message}");
                Helpers.ShowDialog("Failure", $"Cannot copy build file to {copyDestination}");
                return false;

            }
        }
        
    }
}