// BuildValidatorSettings.cs

using System;

namespace GSLab.BuildValidator
{
    using UnityEditor;
    using UnityEngine;

    public class BuildValidatorSettings : ScriptableObject
    {
        public bool CreateStoreListing = true;
        public bool GenerateCsv = false;
        public string IconPath;
        public string FeatureImagePath;
        public string[] ScreenshotPaths = new string[0];
        public string InfoTextPath;
        public string KeystorePath;

        const string AssetPath = "Assets/Editor/BuildValidatorSettings.asset";
        const string AssetDirectory = "Assets/Editor";

        public static BuildValidatorSettings LoadOrCreateSettings()
        {
            // Ensure the parent directory exists
            if (!AssetDatabase.IsValidFolder(AssetDirectory))
            {
                AssetDatabase.CreateFolder("Assets", "Editor");
            }
            var settings = AssetDatabase.LoadAssetAtPath<BuildValidatorSettings>(AssetPath);
            if (settings == null)
            {
                settings = CreateInstance<BuildValidatorSettings>();
                AssetDatabase.CreateAsset(settings, AssetPath);
                AssetDatabase.SaveAssets();
            }
            return settings;
        }

        public void Save() { EditorUtility.SetDirty(this); AssetDatabase.SaveAssets(); }

        public void DrawGUI()
        {
            CreateStoreListing = EditorGUILayout.Toggle("Create Store Listing", CreateStoreListing);
            if (!CreateStoreListing) return;
            
            IconPath = EditorGUILayout.TextField("Icon Path", IconPath);
            FeatureImagePath = EditorGUILayout.TextField("Feature Image Path", FeatureImagePath);
            
            EditorGUILayout.LabelField("Screenshot Paths (min 3)");
            if (ScreenshotPaths.Length < 3)
            {
                Array.Resize(ref ScreenshotPaths, 3);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("-", GUILayout.Width(25)))
            {
                if (ScreenshotPaths.Length > 3)
                {
                    Array.Resize(ref ScreenshotPaths, ScreenshotPaths.Length - 1);
                }
            }
            if (GUILayout.Button("+", GUILayout.Width(25)))
            {
                Array.Resize(ref ScreenshotPaths, ScreenshotPaths.Length + 1);
                ScreenshotPaths[ScreenshotPaths.Length - 1] = string.Empty;
            }
            EditorGUILayout.EndHorizontal();

            for (int i = 0; i < ScreenshotPaths.Length; i++)
            {
                ScreenshotPaths[i] = EditorGUILayout.TextField($"Screenshot {i + 1}", ScreenshotPaths[i]);
            }

            InfoTextPath = EditorGUILayout.TextField("Info Text Path", InfoTextPath);
            KeystorePath = EditorGUILayout.TextField("Keystore Path", KeystorePath);
            GenerateCsv = EditorGUILayout.Toggle("Generate IAP CSV", GenerateCsv);
        }
    }
}
