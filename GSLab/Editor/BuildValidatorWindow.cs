// BuildValidatorWindow.cs
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace GSLab.BuildValidator
{
    public class BuildValidatorWindow : EditorWindow
    {
        BuildValidatorSettings settings;
        BuildChecklistValidator validator;
        List<(string StepName, bool Success)> stepStatus = new List<(string, bool)>();

        [MenuItem("GSLab/Build Validator")]
        public static void ShowWindow() => GetWindow<BuildValidatorWindow>($"Build Validator v{BuildValidationConstants.VALIDATION_VERSION}");

        void OnEnable()
        {
            settings = BuildValidatorSettings.LoadOrCreateSettings();
            validator = new BuildChecklistValidator(settings);
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("Build Validator", EditorStyles.boldLabel);
            settings.DrawGUI();

            GUILayout.Space(20);
            if (GUILayout.Button("Run No-IAP Build"))
                stepStatus = validator.RunAll(BuildTarget.Android, false);
            if (GUILayout.Button("Run IAP Build"))
                stepStatus = validator.RunAll(BuildTarget.Android, true);

            GUILayout.Space(20);
            DrawStepStatus();
        }

        void DrawStepStatus()
        {
            if (stepStatus == null || stepStatus.Count == 0) return;
            EditorGUILayout.LabelField("Step Status:");
            foreach (var (name, success) in stepStatus)
            {
                var style = success ? EditorStyles.label : EditorStyles.boldLabel;
                EditorGUILayout.LabelField(success ? "✓ " + name : "✗ " + name, style);
            }
        }
    }
}