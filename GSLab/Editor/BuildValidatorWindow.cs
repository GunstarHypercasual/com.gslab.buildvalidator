// BuildValidatorWindow.cs

using System;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
#if UNIPAY_PRESENT
using UniPay;
using UnityEngine.Purchasing;
#endif
using UnityEditor.Build;


namespace GSLab.BuildValidator
{
    public class BuildValidatorWindow : EditorWindow
    {
        BuildValidatorSettings settings;
        private StoreListingManager listingManager;
        BuildChecklistValidator validator;
        List<(string StepName, bool Success)> stepStatus = new List<(string, bool)>();

        [MenuItem("GSLab/Build Validator")]
        public static void ShowWindow()
        {
            var window = GetWindow<BuildValidatorWindow>($"Build Validator v{BuildValidationConstants.VALIDATION_VERSION}");
            window.minSize = new Vector2(400, 800);
            window.maxSize = new Vector2(1080, 1920);
        }

        void OnEnable()
        {
            settings = BuildValidatorSettings.LoadOrCreateSettings();
            validator = new BuildChecklistValidator(settings);
            listingManager = new StoreListingManager(settings);

            SyncUniPayDefine();
        }

        static void SyncUniPayDefine()
        {
            const string UNIPAY_SYMBOL = "UNIPAY_PRESENT";
            PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Android, out string[] defines);
            var list = defines.ToList();

            if (AssetDatabase.IsValidFolder("Assets/UniPay"))
            {
                if (!list.Contains(UNIPAY_SYMBOL))
                {
                    list.Add(UNIPAY_SYMBOL);
                    PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Android, list.ToArray());
                    Debug.Log($"[BuildValidator] Added define '{UNIPAY_SYMBOL}' for Android");
                }
            }
            else
            {
                if (list.Contains(UNIPAY_SYMBOL))
                {
                    list.Remove(UNIPAY_SYMBOL);
                    PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Android, list.ToArray());
                    Debug.Log($"[BuildValidator] Remove define '{UNIPAY_SYMBOL}' for Android");
                }
            }
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("Build Validator", EditorStyles.boldLabel);
            settings.DrawGUI();

            GUILayout.Space(30);
            var prevBG = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.2f, 0.6f, 1f);
            
            if (GUILayout.Button("Run No-IAP Build", GUILayout.MinWidth(200), GUILayout.Height(30)))
                stepStatus = validator.RunAll(BuildTarget.Android, false);
            if (GUILayout.Button("Run IAP Build", GUILayout.MinWidth(200), GUILayout.Height(30)))
                stepStatus = validator.RunAll(BuildTarget.Android, true);
            
            GUI.backgroundColor = prevBG;
            
            if (GUILayout.Button("DEBUG - Generate CSV Only"))
            {
                listingManager.GenerateCsv();
            }

            if (GUILayout.Button("DEBUG - Generate material folder"))
            {
                listingManager.PrepareDirectory();
                listingManager.ValidateFiles(false);
                listingManager.RevealDirectory();
            }

            GUILayout.Space(30);
            DrawStepStatus();
        }
        
        void DrawStepStatus()
        {
            if (stepStatus == null || stepStatus.Count == 0) return;
            EditorGUILayout.LabelField("Step Status:");
            foreach (var (name, success) in stepStatus)
            {
                var icon = success ? "✔️ " : "❌ ";
                var style = success ? EditorStyles.label : EditorStyles.boldLabel;
                EditorGUILayout.LabelField(icon + name, style);
            }
        }
    }
}