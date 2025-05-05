// BuildPipelineService.cs
namespace GSLab.BuildValidator
{
    using UnityEditor;
    using UnityEngine;

    public class BuildPipelineService
    {
        public BuildPipelineService(BuildValidatorSettings _) {}
        public bool BuildAAB(BuildTarget target, bool isIAP)
        {
            var opts = new BuildPlayerOptions
            {
                scenes = EditorBuildSettingsScene.GetActiveSceneList(EditorBuildSettings.scenes),
                locationPathName = $"../Builds/{(isIAP?"iap_":"noiap_")}{PlayerSettings.applicationIdentifier}_{PlayerSettings.bundleVersion}_{PlayerSettings.Android.bundleVersionCode}.aab",
                target = target
            };
            var rep = BuildPipeline.BuildPlayer(opts);
            if (rep.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.LogError("Build failed: " + rep.summary.result);
                return false;
            }
            return true;
        }
    }
}