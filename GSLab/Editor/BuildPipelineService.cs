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
        public bool BuildAAB(BuildTarget target, bool isIAP)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            string gameName   = Regex.Replace(PlayerSettings.productName.Trim(), "\\s+", string.Empty);
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
            string outPath = Path.Combine(dir, aabName);
            EditorUserBuildSettings.buildAppBundle = true;
            BuildPlayerOptions opts = new()
            {
                scenes = EditorBuildSettingsScene.GetActiveSceneList(EditorBuildSettings.scenes),
                locationPathName = outPath,
                target = target,
                options = BuildOptions.None
            };
            
            var rep = BuildPipeline.BuildPlayer(opts);
            if (rep.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.LogError("Build failed: " + rep.summary.result);
                return false;
            }
            string storeDir = Path.Combine(projectRoot, "StoreListing");
            string destAAB = Path.Combine(storeDir, aabName);
            try
            {
                File.Copy(outPath, destAAB, true);
                Debug.Log($"Copied AAB to {destAAB}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Can not copy AAB {ex.Message}");
                Helpers.ShowDialog("Failure", "Can not copy file aab");
                return false;
            }
            return true;
        }
    }
}